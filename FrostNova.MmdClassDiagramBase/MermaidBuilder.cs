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

            if (type.HasFlag(MermaidOutputType.Markdown))
            {
                sb.AppendLine("```mermaid");
            }

            sb.AppendLine("classDiagram");


            //クラス一覧を取得 出力対象外はここまでで省かれている
            var classList = list.Select(x => x.Source).ToList();
            classList.AddRange(list.Where(x => x.Dest != null).Select(x => x.Dest!));
            classList = classList.Distinct().ToList();

            // 出力対象クラス集合（そのファイルに関連するクラス）
            var includedSet = new HashSet<ClassInfo>(classList);

            // ルートクラス一覧（この出力の中にあるルート）
            var rootsInOutput = classList.Where(c => c.IsRoot).ToList();

            // ルートの ViewModel として含まれているクラス集合
            var vmInOutput = new HashSet<ClassInfo>();
            var symbolComparer = SymbolEqualityComparer.Default;
            foreach (var root in rootsInOutput)
            {
                if (root.VmTypeSymbol == null) continue;
                var vmClass = classList.FirstOrDefault(c => c.Symbol != null && symbolComparer.Equals(c.Symbol, root.VmTypeSymbol));
                if (vmClass != null) vmInOutput.Add(vmClass);
            }

            var namespaceGrouped = classList.GroupBy(x => x.Namespace);

            //名前空間ごとに出力する

            //全クラスの出力
            foreach (var group in namespaceGrouped)
            {
                if (excludeNamespace.Contains(group.Key))
                {
                    continue;
                }
                //名前空間の出力
                if (isOutputNamespace)
                {
                    sb.Append("namespace ");
                    sb.Append(group.Key);
                    sb.AppendLine(" {");
                }

                foreach (var classInfo in group)
                {
                    sb.AppendLine();
                    // ルートまたはその ViewModel の場合は全メンバー出力、それ以外は includedSet に関係するメンバーのみ出力
                    var isRootOrVm = classInfo.IsRoot || vmInOutput.Contains(classInfo);
                    WriteClass(sb, classInfo, config, includedSet, isRootOrVm);
                }

                if (isOutputNamespace)
                {
                    sb.AppendLine("}");
                }

            }

            //依存関係の出力
            foreach (var dependInfo in list)
            {
                WriteDependency(sb, dependInfo, excludeNamespace);
            }

            if (type.HasFlag(MermaidOutputType.Markdown))
            {
                sb.AppendLine("```");
            }

        }



        void WriteClass(StringBuilder sb, ClassInfo classInfo, DiagramConfig config, HashSet<ClassInfo> includedSet, bool isRootOrVm)
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


            //プロパティの出力
            foreach (var field in classInfo.Properties)
            {
                if (isRootOrVm || IsMemberRelevant(field.TypeInfo, includedSet))
                {
                    WriteMember(sb, field, config.PropertyAccessibility);
                }
            }
            //フィールドの出力
            foreach (var field in classInfo.Fields)
            {
                if (isRootOrVm || IsMemberRelevant(field.TypeInfo, includedSet))
                {
                    WriteMember(sb, field, config.FieldAccessibility);
                }
            }

            //関数の出力
            foreach (var method in classInfo.Methods)
            {
                if (isRootOrVm || IsMethodRelevant(method, includedSet))
                {
                    WriteMethod(sb, method, config.MethodAccessibility);
                }
            }

            sb.AppendLine("    }");

            ////インターフェース
            //foreach (var item in classInfo.ImplementedInterfaces)
            //{
            //    sb.Append("<<");
            //    sb.Append(item.Name);
            //    sb.Append(">> ");
            //    sb.AppendLine(classInfo.Name);
            //}

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

    }
}