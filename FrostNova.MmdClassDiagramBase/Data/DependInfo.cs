using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase.Data
{
    public class DependInfo
    {
        public ClassInfo Source { get; set; }
        public ClassInfo? Dest { get; set; }

        public DependKind Kind { get; set; } = DependKind.Composition;

        public DependInfo(ClassInfo source, ClassInfo dest, DependKind kind)
        {
            Source = source;
            Dest = dest;
            Kind = kind;
        }

        public DependInfo(ClassInfo source)
        {
            Source = source;
            Kind = DependKind.None;
        }






        public static IEnumerable<DependInfo> GetAllDependencies(
        IEnumerable<DependInfo> list,
        ClassInfo target)
        {
            var result = new HashSet<DependInfo>();
            var visited = new HashSet<ClassInfo>();

            void Traverse(ClassInfo current)
            {
                if (!visited.Add(current))
                    return; // 既に調査済み → 循環依存対策

                // current が依存している一次依存（Dest != null のもの）
                var direct = list.Where(d => d.Source == current && d.Dest != null);

                foreach (var dep in direct)
                {
                    if (result.Add(dep))
                    {
                        // さらにその先の依存へ進む
                        Traverse(dep.Dest!);
                    }
                }
            }

            Traverse(target);
            return result;
        }

    }
}
