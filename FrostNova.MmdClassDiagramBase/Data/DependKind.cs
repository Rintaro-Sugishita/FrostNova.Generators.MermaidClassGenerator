using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.MmdClassDiagramBase.Data
{
    public enum DependKind
    {
        None, 
        Inherits,
        Composition,
        Aggregation,
        Association,
        Dependency,
        Realization,

    }
}
