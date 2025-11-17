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
    internal class DependIndoComparer : IEqualityComparer<DependInfo>
    {
        SymbolEqualityComparer _innerComparer = SymbolEqualityComparer.Default;

        public bool Equals(DependInfo? x, DependInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return _innerComparer.Equals(x.Source.Symbol, y.Source.Symbol) && 
                _innerComparer.Equals(x.Dest!.Symbol!, y.Dest!.Symbol!) && x.Kind == y.Kind;
        }

        public int GetHashCode([DisallowNull] DependInfo obj)
        {
           return _innerComparer.GetHashCode( obj.Source!.Symbol) | _innerComparer.GetHashCode(obj.Dest?.Symbol) | obj.Kind.GetHashCode();
        }
    }
}
