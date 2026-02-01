using FrostNova.MmdClassDiagramBase.Data;
using FrostNova.MmdClassDiagramGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase
{
    public class MermaidBuilder
    {
        public void Output(List<DependInfo> list, DiagramConfig config, string outputDir)
        {

            var outputType = config.GetOutputType();
            var outputExtension = DiagramConfig.GetOutputExtension(outputType);

            if (config.OutputMode.Equals("One", StringComparison.OrdinalIgnoreCase))
            {
                //1つのファイルにまとめて出力
                var sb = new StringBuilder();
                Output(sb, list, config, outputType, config.OutputNamespace, config.ExcludeNameSpaces);
                var outputPath = Path.Combine(outputDir, $"ClassDiagram{outputExtension}");
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"Diagram generated at: {outputPath}");
            }
            else
            {
                //1つの root要素それぞれ別のファイルへ出力
                var rootClasses = list.Where(x => x.Source.IsRoot).GroupBy(x => x.Source);
                foreach (var item in rootClasses)
                {
                    var sb = new StringBuilder();
                    //ルート要素だけでなく、依存するものも追加する

                    var dependencies = new List<DependInfo>();
                    dependencies.AddRange(item);
                    foreach (var item2 in item)
                    {
                        dependencies.AddRange(
                        DependInfo.GetAllDependencies(list, item2.Source));
                    }

                    Output(sb, dependencies.ToList(), config, outputType, config.OutputNamespace, config.ExcludeNameSpaces);
                    var outputPath = Path.Combine(outputDir, $"{item.Key.Name}{outputExtension}");

                    File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                    Console.WriteLine($"Diagram generated at: {outputPath}");
                }


                //1つのグループ単位でそれぞれ別のファイルへ出力
                var groups = list.SelectMany(x => x.Source.Groups).Distinct().ToList();

                var generalGroupConfig = config.groupConfigs.FirstOrDefault(x => string.IsNullOrEmpty(x.GroupName));

                foreach (var group in groups)
                {
                    var roots = list.Where(x => x.Source.Groups.Contains(group) || (x.Dest?.Groups.Contains(group) == true)).ToList();
                    //ルート要素だけでなく、依存するものも追加する
                    var groupConfig = config.groupConfigs.FirstOrDefault(x => x.GroupName == group);

                    var dependencies = new List<DependInfo>();
                    dependencies.AddRange(roots);
                    foreach (var item in roots)
                    {
                        dependencies.AddRange(
                        DependInfo.GetAllDependencies(list, item.Source));

                    }

                    var sb = new StringBuilder();
                    Output(sb, dependencies.Distinct().ToList(), config, outputType,
                        generalGroupConfig?.OutputNamespace ?? groupConfig?.OutputNamespace ?? config.OutputNamespace,
                        generalGroupConfig?.ExcludeNamespace ?? groupConfig?.ExcludeNamespace ?? config.ExcludeNameSpaces
                        );
                    var outputPath = Path.Combine(outputDir, $"{group}{outputExtension}");
                    File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                    Console.WriteLine($"Diagram generated at: {outputPath}");
                }
            }

            //乗せるクラス、乗せるけど中身までは書かないクラス、載せないクラス
            //載せないクラス＝名前空間が無視になっている、無視Attributeがついている
        }




        public void Output(StringBuilder sb, List<DependInfo> list, DiagramConfig config, MermaidOutputType type, bool isOutputNamespace, string[] excludeNamespace)
        {
            var allPotentialClasses = list.Select(x => x.Source)
        .Concat(list.Where(x => x.Dest != null).Select(x => x.Dest!))
        .Distinct()
        .ToList();

            // 2. Rootから到達可能な全クラスを事前に計算しておく
            var entryPoints = allPotentialClasses.Where(x => x.IsRoot).ToList();
            var reachableSet = TraceReachableClasses(entryPoints, list);
            AddViewModelsToSet(reachableSet, allPotentialClasses); // 前回の返答のメソッドを利用

            // 3. 出力対象のフィルタリング
            var includedSet = new HashSet<ClassInfo>();

            foreach (var c in allPotentialClasses)
            {
                // 判定ロジック:
                // A. チェック対象の名前空間ではないクラス → 無条件で残す
                // B. チェック対象の名前空間だが、Rootから到達可能 → 残す
                // C. チェック対象の名前空間で、かつ到達不能 → 除外！

                bool isTargetNamespace = config.IsReachabilityCheckTarget(c.Namespace);
                bool isReachable = reachableSet.Contains(c);

                if (!isTargetNamespace || isReachable)
                {
                    includedSet.Add(c);
                }
            }

            // あとはこの includedSet を使って描画
            var classList = includedSet.ToList();


            if (type.HasFlag(MermaidOutputType.Markdown))
            {
                sb.AppendLine("```mermaid");
            }

            sb.AppendLine("classDiagram");

            //// 1. 真のルート要素（configや呼び出し元でRootフラグが立っているもの）を特定
            //var entryPoints = list
            //    .Where(x => x.Source.IsRoot)
            //    .Select(x => x.Source)
            //    .Distinct()
            //    .ToList();

            //// 2. ルートから到達可能なクラスのみを抽出する (再帰的探索)
            //var reachableClasses = new HashSet<ClassInfo>();
            //var stack = new Stack<ClassInfo>(entryPoints);

            //while (stack.Count > 0)
            //{
            //    var current = stack.Pop();
            //    if (reachableClasses.Contains(current)) continue;

            //    // DbSet自体は含めない
            //    if (IsEfCoreDbSet(current)) continue;

            //    reachableClasses.Add(current);

            //    // 現在のクラスから出ている依存先をスタックに積む
            //    var dependencies = list
            //        .Where(d => d.Source == current && d.Dest != null)
            //        .Select(d => d.Dest!);

            //    foreach (var dest in dependencies)
            //    {
            //        if (!reachableClasses.Contains(dest))
            //        {
            //            stack.Push(dest);
            //        }
            //    }
            //}

            //// 3. 出力対象を「到達可能なクラス」に限定
            //var classList = reachableClasses.ToList();
            //var includedSet = reachableClasses;

            // --- 以下、既存の ViewModel 判定や Assembly 判定 ---
            var rootsInOutput = classList.Where(c => c.IsRoot).ToList();
            var vmInOutput = new HashSet<ClassInfo>();
            var symbolComparer = SymbolEqualityComparer.Default;
            foreach (var root in rootsInOutput)
            {
                if (root.VmTypeSymbol == null) continue;
                // classList(到達可能リスト)の中にVMがあるか確認
                var vmClass = list.SelectMany(d => new[] { d.Source, d.Dest })
                                 .FirstOrDefault(c => c != null && c.Symbol != null && symbolComparer.Equals(c.Symbol, root.VmTypeSymbol));
                if (vmClass != null)
                {
                    vmInOutput.Add(vmClass);
                    includedSet.Add(vmClass); // VMも出力対象に加える
                }
            }

            // --- フィルタリングされたリストでネームスペース・クラス出力 ---
            var namespaceGrouped = classList.GroupBy(x => x.Namespace);

            foreach (var group in namespaceGrouped)
            {
                if (excludeNamespace.Contains(group.Key)) continue;

                if (isOutputNamespace)
                {
                    sb.AppendLine($"namespace {group.Key} {{");
                }

                foreach (var classInfo in group)
                {
                    // 省略されていたとしても、ここで判定
                    var isRootOrVm = classInfo.IsRoot || vmInOutput.Contains(classInfo);
                    var asmName = classInfo.Symbol?.ContainingAssembly?.Identity?.Name;
                    // sourceAssemblyNames の定義は既存コード通りとする
                    var isFromSourceAssembly = asmName != null;

                    WriteClass(sb, classInfo, config, includedSet, isRootOrVm, isFromSourceAssembly, new HashSet<string>());
                }

                if (isOutputNamespace) sb.AppendLine("}");
            }

            // 4. 依存関係の出力も「両端が includedSet に含まれるもの」に限定
            foreach (var dependInfo in list)
            {
                if (dependInfo.Source == null || dependInfo.Dest == null) continue;
                if (!includedSet.Contains(dependInfo.Source) || !includedSet.Contains(dependInfo.Dest)) continue;

                WriteDependency(sb, dependInfo, excludeNamespace);
            }

            if (type.HasFlag(MermaidOutputType.Markdown))
            {
                sb.AppendLine("```");
            }
        }


        void WriteClass(StringBuilder sb, ClassInfo classInfo, DiagramConfig config, HashSet<ClassInfo> includedSet, bool isRootOrVm, bool isFromSourceAssembly, HashSet<string> referencedTypeFullNames)
        {
            sb.Append("    class ");
            sb.Append(classInfo.Name);

            if (classInfo.GenericArgumentNames.Count > 0)
            {
                sb.Append("~");

                for (int i = 0; i < classInfo.GenericArgumentNames.Count; i++)
                {
                    if (i > 0) { sb.Append(", "); }
                    sb.Append(classInfo.GenericArgumentNames[i]);
                }


                sb.Append("~");
            }
            sb.AppendLine("{");

            if (classInfo.IsAbstract)
            {
                sb.AppendLine("        <<Abstract>>");
            }

            if (classInfo.IsInterface)
            {
                sb.AppendLine("        <<Interface>>");
            }

            if (classInfo.IsEnum)
            {
                sb.AppendLine("        <<Enumeration>>");
            }
            // プロジェクト跨ぎのルール：
            // - isFromSourceAssembly == true の場合（参照元アセンブリのクラス）は従来のルールでメンバー出力
            // - false（外部クラス）の場合は、参照元が実際に参照している型のみを基準にメンバーを出力する
            bool skipMembers = config.ShouldExcludeMembers(classInfo.Namespace) && !isRootOrVm;
            if (!skipMembers)
            {
                if (isFromSourceAssembly)
                {
                    foreach (var field in classInfo.Properties)
                    {
                        if (isRootOrVm || IsMemberRelevant(field.TypeInfo, includedSet))
                        {
                            WriteMember(sb, field, config.PropertyAccessibility);
                        }
                    }
                    foreach (var field in classInfo.Fields)
                    {
                        if (isRootOrVm || IsMemberRelevant(field.TypeInfo, includedSet))
                        {
                            WriteMember(sb, field, config.FieldAccessibility);
                        }
                    }
                    foreach (var method in classInfo.Methods)
                    {
                        if (isRootOrVm || IsMethodRelevant(method, includedSet))
                        {
                            WriteMethod(sb, method, config.MethodAccessibility);
                        }
                    }
                }
                else
                {
                    // 外部クラス: referencedTypeFullNames を基準に絞る
                    foreach (var field in classInfo.Properties)
                    {
                        if (isRootOrVm || IsTypeReferencedBySources(field.TypeInfo, referencedTypeFullNames))
                        {
                            WriteMember(sb, field, config.PropertyAccessibility);
                        }
                    }
                    foreach (var field in classInfo.Fields)
                    {
                        if (isRootOrVm || IsTypeReferencedBySources(field.TypeInfo, referencedTypeFullNames))
                        {
                            WriteMember(sb, field, config.FieldAccessibility);
                        }
                    }
                    foreach (var method in classInfo.Methods)
                    {
                        if (isRootOrVm || IsMethodReferencedBySources(method, referencedTypeFullNames))
                        {
                            WriteMethod(sb, method, config.MethodAccessibility);
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("        %% Members hidden by config");
            }

            sb.AppendLine("    }");
        }

        /// <summary>
        /// Root要素から依存関係を再帰的に辿り、到達可能なクラスの集合を返します。
        /// </summary>
        private HashSet<ClassInfo> TraceReachableClasses(List<ClassInfo> entries, List<DependInfo> allDepends)
        {
            var reachable = new HashSet<ClassInfo>();
            var stack = new Stack<ClassInfo>(entries);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // 既に探索済み、あるいはnullの場合はスキップ
                if (current == null || reachable.Contains(current)) continue;

                // DbSet自体はノードとして出したくない場合が多いのでここで弾く
                if (IsEfCoreDbSet(current)) continue;

                // 到達可能リストに追加
                reachable.Add(current);

                // 現在のクラス (Source) から出ている依存先 (Dest) を抽出
                // 矢印の向きに従って辿る
                var dependencies = allDepends
                    .Where(d => d.Source != null && d.Source.Equals(current) && d.Dest != null)
                    .Select(d => d.Dest!)
                    .ToList();

                foreach (var dest in dependencies)
                {
                    if (!reachable.Contains(dest))
                    {
                        stack.Push(dest);
                    }
                }
            }
            return reachable;
        }

        /// <summary>
        /// Rootに対応するViewModelが存在する場合、それらも「到達可能」としてセットに加えます。
        /// </summary>
        private void AddViewModelsToSet(HashSet<ClassInfo> set, List<ClassInfo> allPotentialClasses)
        {
            var symbolComparer = SymbolEqualityComparer.Default;

            // 現在のセットに含まれるRoot要素の中で、ViewModelのSymbolを持っているものを抽出
            var rootHasVm = set.Where(c => c.IsRoot && c.VmTypeSymbol != null).ToList();

            foreach (var root in rootHasVm)
            {
                // 全クラスの中から、Symbolが一致するViewModelクラスを探す
                var vmClass = allPotentialClasses.FirstOrDefault(c =>
                    c.Symbol != null && symbolComparer.Equals(c.Symbol, root.VmTypeSymbol));

                if (vmClass != null && !set.Contains(vmClass))
                {
                    // ViewModel自体をセットに追加
                    set.Add(vmClass);

                    // 注意: ViewModelからさらに先に伸びる依存関係も辿る必要がある場合は、
                    // TraceReachableClasses を再度呼ぶか、Trace側で最初からVMもEntryに入れる必要があります。
                }
            }
        }
        // 補助: Dest とそのジェネリック引数を再帰的に fullName 集合へ追加
        static void AddTypeAndGenericFullNames(ClassInfo? type, HashSet<string> set)
        {
            if (type == null) return;
            if (!string.IsNullOrEmpty(type.FullName)) set.Add(type.FullName);
            if (type.GenericTypes != null)
            {
                foreach (var gt in type.GenericTypes)
                {
                    AddTypeAndGenericFullNames(gt, set);
                }
            }
        }

        // 補助: referencedTypeFullNames に含まれる型か（ジェネリック引数も確認）
        static bool IsTypeReferencedBySources(ClassInfo? typeInfo, HashSet<string> referencedTypeFullNames)
        {
            if (typeInfo == null) return false;
            if (!string.IsNullOrEmpty(typeInfo.FullName) && referencedTypeFullNames.Contains(typeInfo.FullName)) return true;
            if (typeInfo.GenericTypes != null && typeInfo.GenericTypes.Any(gt => !string.IsNullOrEmpty(gt.FullName) && referencedTypeFullNames.Contains(gt.FullName))) return true;
            return false;
        }

        // 補助: メソッドが参照元から参照されているか判定（戻り値または引数の型が referencedTypeFullNames に含まれるか）
        static bool IsMethodReferencedBySources(MethodInfo method, HashSet<string> referencedTypeFullNames)
        {
            if (method == null) return false;
            if (method.ReturnType != null && IsTypeReferencedBySources(method.ReturnType, referencedTypeFullNames)) return true;
            if (method.Parameters != null && method.Parameters.Any(p => IsTypeReferencedBySources(p.TypeClass, referencedTypeFullNames))) return true;
            return false;
        }
        // 指定クラス集合に含まれる型か（ジェネリック引数も確認）
        static bool IsMemberRelevant(ClassInfo? typeInfo, HashSet<ClassInfo> includedSet)
        {
            if (typeInfo == null) return false;
            if (includedSet.Contains(typeInfo)) return true;
            if (typeInfo.GenericTypes != null && typeInfo.GenericTypes.Any(gt => includedSet.Contains(gt))) return true;
            return false;
        }

        // メソッドが出力対象か判定（戻り値または任意の引数が includedSet に含まれるか）
        static bool IsMethodRelevant(MethodInfo method, HashSet<ClassInfo> includedSet)
        {
            if (method == null) return false;
            if (method.ReturnType != null && IsMemberRelevant(method.ReturnType, includedSet)) return true;
            if (method.Parameters != null && method.Parameters.Any(p => IsMemberRelevant(p.TypeClass, includedSet))) return true;
            return false;
        }

        public static void WriteMember(StringBuilder sb, MemberInfo member, Accessibility minAccessibility)
        {
            sb.Append("        ");
            if ((int)member.Accessibility < (int)minAccessibility) { sb.Append("%%"); }
            sb.Append(GetMermaidVisibility(member.Accessibility));

            //型
            WriteTypeName(sb, member.TypeInfo);
            //var typeName = GetTypeName(member.TypeInfo);
            //sb.Append(typeName);

            //名前
            sb.Append(" ");
            sb.AppendLine(member.Name);
        }

        public static void WriteMethod(StringBuilder sb, MethodInfo method, Accessibility minAccessibility)
        {
            sb.Append("        ");
            if ((int)method.Accessibility < (int)minAccessibility) { sb.Append("%%"); }
            sb.Append(GetMermaidVisibility(method.Accessibility));

            //名前
            sb.Append(" ");
            sb.Append(method.Name);

            sb.Append("(");

            //引数
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                if (i > 0) { sb.Append(", "); }
                var arg = method.Parameters[i];
                WriteTypeName(sb, arg.TypeClass);
                sb.Append(" ");
                sb.Append(arg.Name);
            }


            sb.Append(")");

            //戻り値の型
            if (method.ReturnType != null && method.ReturnType.Name != "Void")
            {
                sb.Append(" ");
                WriteTypeName(sb, method.ReturnType);
            }
            sb.AppendLine();
            //var typeName = GetTypeName(member.TypeInfo);
            //sb.Append(typeName);


        }

        public static string GetTypeName(ClassInfo? classInfo)
        {
            if (classInfo == null) return "";
            var name = classInfo.Name;
            if (classInfo.GenericTypes.Count > 0)
            {
                name += "~";

                for (int i = 0; i < classInfo.GenericTypes.Count; i++)
                {
                    var genericType = classInfo.GenericTypes[i];
                    if (i != 0) { name += ","; }
                    name += genericType.Name;
                }
                name += "~";
            }
            return name;
        }

        public static void WriteTypeName(StringBuilder sb, ClassInfo? classInfo)
        {
            if (classInfo == null) return;
            sb.Append(classInfo.Name);

            if (classInfo.GenericTypes.Count > 0)
            {
                sb.Append("~");

                for (int i = 0; i < classInfo.GenericTypes.Count; i++)
                {
                    var genericType = classInfo.GenericTypes[i];
                    if (i != 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append(genericType.Name);
                }
                sb.Append("~");
            }
        }

        public static void WriteDependency(StringBuilder sb, DependInfo info, string[] excludeNamespace)
        {
            if (info.Dest == null) return;
            if (excludeNamespace.Contains(info.Source.Namespace)) return;
            if (excludeNamespace.Contains(info.Dest.Namespace)) return;

            sb.Append(info.Source.Name);
            sb.Append(" ");
            sb.Append(GetMermaidRelationship(info.Kind));
            sb.Append(" ");

            sb.Append(info.Dest.Name);

            var description = GetMermaidRelationshipDescription(info.Kind);
            if (description != null)
            {
                sb.Append(" : ");
                sb.Append(description);
            }
            sb.AppendLine();
        }

        public static string GetMermaidVisibility(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return "-";
                case Accessibility.ProtectedAndInternal:
                    return "#~";
                case Accessibility.Protected:
                    return "#";
                case Accessibility.Internal:
                    return "~";
                case Accessibility.ProtectedOrInternal:
                    return "#~";
                case Accessibility.Public:
                    return "+";
                default:
                    return "-";
            }
        }

        public static string GetMermaidRelationship(DependKind kind)
        {
            var a = kind switch
            {
                DependKind.Inherits => "--|>",
                DependKind.Composition => "--*",
                DependKind.Aggregation => "--o",
                DependKind.Association => "-->",
                DependKind.Dependency => "..>",
                DependKind.Realization => "..|>",
                _ => "--"
            };
            return a;
        }

        public static string? GetMermaidRelationshipDescription(DependKind kind)
        {
            var a = kind switch
            {
                DependKind.Inherits => "inherits",
                DependKind.Realization => "implements",
                _ => null
            };
            return a;
        }

        // EF Core 判定ヘルパー: DbSet 判定
        static bool IsEfCoreDbSet(ClassInfo? c)
        {
            if (c == null || c.Symbol == null) return false;
            if (c.Symbol is INamedTypeSymbol named)
            {
                try
                {
                    var orig = named.OriginalDefinition;
                    var s = orig.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (s.Contains("Microsoft.EntityFrameworkCore.DbSet")) return true;
                }
                catch { }
                if (string.Equals(c.Name, "DbSet", StringComparison.Ordinal) &&
                    (c.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ?? false))
                    return true;
            }
            return false;
        }

    }
}