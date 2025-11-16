using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase.Data
{
    public class DependIndo
    {
        public ClassInfo Source { get; set; }
        public ClassInfo? Dest { get; set; }

        public DependKind Kind { get; set; } = DependKind.Composition;

        public DependIndo(ClassInfo source, ClassInfo dest, DependKind kind)
        {
            Source = source;
            Dest = dest;
            Kind = kind;
        }

        public DependIndo(ClassInfo source)
        {
            Source = source;
            Kind = DependKind.None;
        }
    }
}
