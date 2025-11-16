using FrostNova.MmdClassDiagramBase.Data;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase
{
    internal class ClassInfoComparer : IEqualityComparer<ClassInfo>
    {
        SymbolEqualityComparer _innerComparer = SymbolEqualityComparer.Default;
        public bool Equals(ClassInfo? x, ClassInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return _innerComparer.Equals(x.Symbol, y.Symbol);
        }

        public int GetHashCode([DisallowNull] ClassInfo obj)
        {
            return _innerComparer.GetHashCode(obj.Symbol);
        }

    }
}
