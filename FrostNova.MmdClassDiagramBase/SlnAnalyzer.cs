using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FrostNova.MmdClassDiagramGenerator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using FrostNova.MmdClassDiagramBase.Data;
using MemberInfo = FrostNova.MmdClassDiagramBase.Data.MemberInfo;
using MethodInfo = FrostNova.MmdClassDiagramBase.Data.MethodInfo;
using FrostNova.MmdClassDiagramGenerator.Attributes;
using Microsoft.Build.Locator;

namespace FrostNova.MmdClassDiagramBase
{
    public class SlnAnalyzer
    {

        private SymbolEqualityComparer symbolComparer = SymbolEqualityComparer.Default;
        private ClassInfoComparer classComparer = new ClassInfoComparer();
        private readonly Dictionary<string, ClassInfo> _classMap = new Dictionary<string, ClassInfo>(StringComparer.Ordinal);

        public async Task<List<ClassInfo>> AnalyzeSolution(string slnPath, DiagramConfig config)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                if (instances.Length > 0)
                {
                    // Visual Studio や BuildTools が見つかった場合のみ Register
                    MSBuildLocator.RegisterInstance(instances[0]);
                }
                else
                {
                    const string VS_DIR = @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin";
                    if (System.IO.Directory.Exists(VS_DIR))
                        MSBuildLocator.RegisterMSBuildPath(VS_DIR);
                    else
                        MSBuildLocator.RegisterDefaults();


                }
            }


            using var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();

            Console.WriteLine($"Loading solution: {slnPath}");
            var solution = await workspace.OpenSolutionAsync(slnPath);

            var compilationTasks = solution.Projects.Select(p => p.GetCompilationAsync()).ToList();
            var compilations = await Task.WhenAll(compilationTasks);

            var symbolComparer = SymbolEqualityComparer.Default;
            var deps = new HashSet<ClassInfo>(comparer: classComparer);
            _classMap.Clear();
            //ソリューションに含まれるプロジェクト（参照は除く）
            var projects = solution.Projects;
            foreach (var project in projects)
            {
                Console.WriteLine($"  Loading project: {project.Name}");
                await AnalyzeProject(project, deps, config);
            }

            return deps.ToList();
        }


        public async Task AnalyzeProject(Project project, HashSet<ClassInfo> deps, DiagramConfig config)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) return;
            //プロジェクトのファイル単位
            //partialクラスをファイルを分けて記述すると2重に出てくるので、メソッド定義だけ解析する必要がある
            foreach (var document in project.Documents)
            {
                if (!document.SupportsSyntaxTree) continue;
                var tree = await document.GetSyntaxTreeAsync();
                if (tree is null) continue;

                var documentPath = document.FilePath;

                var root = await tree.GetRootAsync();
                var semanticModel = compilation.GetSemanticModel(tree);
                // クラスを列挙
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (ClassDeclarationSyntax cls in classes)
                {
                    var clsSymbol = semanticModel.GetDeclaredSymbol(cls) as INamedTypeSymbol;
                    if (clsSymbol == null) continue;

                    AnalyzeClass(deps, clsSymbol, cls, semanticModel, document);

                }
            }

        }

        private void AnalyzeClass(HashSet<ClassInfo> deps, INamedTypeSymbol clsSymbol, ClassDeclarationSyntax cls, SemanticModel semanticModel, Document document)
        {
            var key = GetSymbolKey(clsSymbol);
            if (!_classMap.TryGetValue(key, out var classInfo))
            {
                classInfo = new ClassInfo(clsSymbol);
                deps.Add(classInfo);
                _classMap[key] = classInfo;
            }
            //ClassInfo classInfo = new ClassInfo(clsSymbol);
            //if (!deps.Add(classInfo))
            //{
            //    //既にあるならそっちを使う
            //    classInfo = deps.First(x => symbolComparer.Equals(x.Symbol, clsSymbol));
            //}
            if (classInfo.IsFirstRead)
            {
                //初ならクラス解析をする
                classInfo.IsFirstRead = false;
                //abstract, enum, interfaceの分岐
                classInfo.IsEnum = clsSymbol.TypeKind == TypeKind.Enum;
                classInfo.IsInterface = clsSymbol.TypeKind == TypeKind.Interface;
                classInfo.IsAbstract = clsSymbol.IsAbstract;

                classInfo.Accessibility = clsSymbol.DeclaredAccessibility;

                //attributeの解析
                var atts = clsSymbol.GetAttributes();
                if (atts != null && atts.Length > 0)
                {
                    foreach (var attr in atts)
                    {
                        var attrClass = attr.AttributeClass;
                        if (attrClass == null) continue;

                        if (attrClass.Name == nameof(MmdDiagramRootAttribute))
                        {
                            classInfo.IsRoot = true;

                            for (int i = 0; i < attr.NamedArguments.Length; i++)
                            {
                                var arg = attr.NamedArguments[i];

                                var paramName = arg.Key;
                                var paramValue = arg.Value;


                                switch (paramName)
                                {
                                    case nameof(MmdDiagramRootAttribute.GroupNames):


                                        if (paramValue.Values != null && paramValue.Values.Length > 0)
                                        {
                                            classInfo.Groups.AddRange(paramValue.Values.Where(x => x.Value != null).Select<TypedConstant, string>(x => ((string?)x.Value) ?? ""));
                                        }

                                        break;
                                    case nameof(MmdDiagramRootAttribute.ViewModelType):

                                        //既に解析済み、あるいはこれから解析予定のViewModelがWindowに設定されているか確認する
                                        var vmType = paramValue.Value as INamedTypeSymbol;
                                        if (vmType != null)
                                        {
                                            classInfo.VmTypeSymbol = vmType;
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                        else if (attrClass.Name == nameof(MmdDiagramIgnoreAttribute))
                        {

                        }

                    }
                }


                // 継承関係
                if (clsSymbol.BaseType is { } baseType && baseType.Name != "Object")
                {
                    var baseClasses = AddDeps(deps, baseType, classInfo);
                    classInfo.BaseType = baseClasses;
                }
                // インターフェース
                foreach (var iFace in clsSymbol.AllInterfaces)
                {
                    var interfaceTypes = AddDeps(deps, iFace, classInfo);
                    classInfo.ImplementedInterfaces.Add(interfaceTypes);
                }

                // メンバー
                foreach (var member in clsSymbol.GetMembers())
                {
                    // 追加: シンボルの宣言位置を見て「全て自動生成ファイルのみ」にあるメンバーか判定
                    var sourcePaths = member.Locations
                                           .Where(l => l.IsInSource)
                                           .Select(l => l.SourceTree?.FilePath)
                                           .Where(fp => !string.IsNullOrEmpty(fp))
                                           .ToArray();

                    bool declaredOnlyInGenerated = sourcePaths.Length > 0
                        && sourcePaths.All(fp => fp!.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || fp!.EndsWith(".g.vb", StringComparison.OrdinalIgnoreCase));

                    // 自動生成ファイルにしか存在しない定義なら public のみ解析対象
                    if (declaredOnlyInGenerated && member.DeclaredAccessibility != Accessibility.Public) continue;

                    switch (member)
                    {
                        case IPropertySymbol prop:
                            var propInfo = new MemberInfo(prop);
                            var types = AddDeps(deps, propInfo.TypeSymbol, classInfo);
                            propInfo.TypeInfo = types;
                            classInfo.Properties.Add(propInfo);

                            break;
                        case IFieldSymbol field:
                            var fieldInfo = new MemberInfo(field);
                            var types2 = AddDeps(deps, fieldInfo.TypeSymbol, classInfo);
                            fieldInfo.TypeInfo = types2;

                            classInfo.Fields.Add(fieldInfo);
                            break;
                        case IMethodSymbol method:
                            //引数なし publicコンストラクタは対象外とする                            
                            if (method.MethodKind == MethodKind.Constructor &&
                                method.DeclaredAccessibility == Accessibility.Public &&
                                method.Parameters.Length == 0)
                            {
                                continue;
                            }

                            //propertyの getterと setterは対象外
                            if (method.MethodKind == MethodKind.PropertySet) continue;
                            if (method.MethodKind == MethodKind.PropertyGet) continue;

                            var methodInfo = new MethodInfo(method);


                            //戻り値の型
                            if (methodInfo.ReturnTypeSymbol.IsAnonymousType) continue;
                            var returns = AddDeps(deps, methodInfo.ReturnTypeSymbol, classInfo);
                            methodInfo.ReturnType = returns;

                            //引数の型
                            foreach (var methodParam in methodInfo.Parameters)
                            {
                                if (methodParam.TypeSymbol == null) continue;
                                if (methodParam.TypeSymbol.IsAnonymousType) continue;
                                var p = AddDeps(deps, methodParam.TypeSymbol, classInfo);
                                methodParam.TypeClass = p;
                            }

                            classInfo.Methods.Add(methodInfo);

                            break;
                    }
                }


            }

            //パス設定
            if (document.FilePath != null)
                classInfo.FilePaths.Add(document.FilePath);

            // メソッド呼び出し関係
            foreach (var methodSyntaxSymbol in cls.Members.OfType<MethodDeclarationSyntax>())
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntaxSymbol) as IMethodSymbol;
                if (methodSymbol == null) continue;
                //var method = deps.FirstOrDefault(x => symbolComparer.Equals(x.Symbol, methodSymbol) == true);

                foreach (var inv in methodSyntaxSymbol.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var target = semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
                    if (target?.ContainingType == null) continue;
                    if (target.ContainingType.IsAnonymousType) continue;

                    var methodDeclarationTypes = AddDeps(deps, target.ContainingType, classInfo);
                    classInfo.Depends.Add(methodDeclarationTypes);
                }
            }

        }

        public List<DependInfo> AnalyzeDependencies(List<ClassInfo> list, DiagramConfig config)
        {
            //依存関係をリスト化する
            //ここで要らないクラスは省く

            var deps = new List<DependInfo>();
            var visited = new HashSet<ClassInfo>();
            foreach (var classInfo in list)
            {
                if (classInfo.IsIgnore) continue;
                var isAdded = new List<bool>();
                //継承
                isAdded.Add(AddDependency(deps, visited, classInfo, classInfo.BaseType, config, DependKind.Inherits));

                //インターフェース
                foreach (var iFace in classInfo.ImplementedInterfaces)
                {
                    isAdded.Add(AddDependency(deps, visited, classInfo, iFace, config, DependKind.Realization));
                }

                //プロパティ、フィールド型
                foreach (var item in classInfo.Properties)
                {
                    isAdded.Add(AddDependency(deps, visited, classInfo, item.TypeInfo, config, DependKind.Association));
                }
                foreach (var item in classInfo.Fields)
                {
                    isAdded.Add(AddDependency(deps, visited, classInfo, item.TypeInfo, config, DependKind.Association));
                }

                //関数
                foreach (var method in classInfo.Methods)
                {
                    //戻り値の型
                    isAdded.Add(AddDependency(deps, visited, classInfo, method.ReturnType, config, DependKind.Association));

                    //関数の引数の型
                    foreach (var methodParam in method.Parameters)
                    {
                        isAdded.Add(AddDependency(deps, visited, classInfo, methodParam.TypeClass, config, DependKind.Association));
                    }

                    //関数の中身が使っているクラス
                    foreach (var a in classInfo.Depends)
                    {
                        isAdded.Add(AddDependency(deps, visited, classInfo, a, config, DependKind.Dependency));
                    }
                }


                //view model
                if (classInfo.VmTypeSymbol != null)
                {
                    //var vmClass = list.FirstOrDefault(x => symbolComparer.Equals(x.Symbol, classInfo.VmTypeSymbol));
                    var vmKey = GetSymbolKey(classInfo.VmTypeSymbol);
                    _classMap.TryGetValue(vmKey, out var vmClass);
                    isAdded.Add(AddDependency(deps, visited, classInfo, vmClass, config, DependKind.Dependency));
                }

                if (isAdded.Any(x => x))
                {
                    visited.Add(classInfo);
                }
            }

            deps = deps.Distinct(new DependIndoComparer()).ToList();

            if (config.IsFullMode)
            {
                //どことも関係がないものをリスト
                var unLoads = list
                    .Where(x => x.IsIgnore == false &&
                                visited.Contains(x, classComparer) == false &&
                                x.FilePaths.Count > 0
                    )
                    .Distinct()
                    .ToList();
                deps.AddRange(unLoads.Select(x => new DependInfo(x)));
            }


            return deps;
        }


        private bool AddDependency(List<DependInfo> deps, HashSet<ClassInfo> visited, ClassInfo? classInfo, ClassInfo? dest, DiagramConfig config, DependKind kind)
        {
            if (classInfo == null) return false;
            if (dest == null) return false;

            if (classComparer.Equals(classInfo, dest)) return false;

            bool isAdded = false;
            if (IsTarget(dest, config))
            {
                var dep = new DependInfo(classInfo, dest, kind);
                visited.Add(dep.Dest!);
                deps.Add(dep);
                isAdded = true;
            }

            //関数の戻り値のジェネリック型
            foreach (var returnGenericType in dest.GenericTypes)
            {
                if (IsTarget(returnGenericType, config))
                {
                    var dep = new DependInfo(classInfo, returnGenericType, DependKind.Association);
                    visited.Add(dep.Dest!);
                    deps.Add(dep);
                    isAdded = true;
                }
            }
            return isAdded;
        }


        static bool IsTarget(ClassInfo info, DiagramConfig config)
        {
            if (info == null) return false;
            if (info.IsIgnore) return false;

            if (info.Namespace.StartsWith("System.")) return false;
            if (info.Namespace.StartsWith("Microsoft.")) return false;

            foreach (var item in config.ExcludeNameSpaces)
            {
                if (info.Namespace.StartsWith(item)) return false;
            }
            return true;
        }



        ClassInfo AddDeps(HashSet<ClassInfo> deps, ITypeSymbol symbol, ClassInfo baseClass)
        {
            var key = GetSymbolKey(symbol);

            if (_classMap.TryGetValue(key, out var typeSymbol))
            {
                return typeSymbol;
            }
            typeSymbol = new ClassInfo(symbol);
            deps.Add(typeSymbol);
            _classMap[key] = typeSymbol;

            //ジェネリック型を解析する
            if (typeSymbol.Symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
            {
                foreach (var item in namedTypeSymbol.TypeArguments)
                {
                    if (item.IsAnonymousType) continue;
                    var res = AddDeps(deps, item, baseClass);
                    typeSymbol.GenericTypes.Add(res);

                }
                foreach (var item in namedTypeSymbol.TypeParameters)
                {
                    typeSymbol.GenericArgumentNames.Add(item.Name);
                }
            }

            return typeSymbol;
        }

        private static string GetSymbolKey(ITypeSymbol symbol)
        {
            if (symbol == null) return string.Empty;

            // OriginalDefinition を使い、名前空間付きの完全名＋アセンブリ名でキー化
            var named = symbol as INamedTypeSymbol;
            string fullName;
            try
            {
                fullName = symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            catch
            {
                fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            var assemblyName = (symbol.ContainingAssembly?.Identity?.Name) ?? string.Empty;

            // ジェネリックのアリティを加えることで差異を吸収
            var arity = named?.Arity ?? 0;

            return $"{fullName}|{assemblyName}|arity={arity}";
        }



    }
}
