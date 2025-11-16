using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.MmdClassDiagramBase.Data
{
    public class MethodParameterInfo
    {
        public ITypeSymbol? TypeSymbol { get; set; }
        public ClassInfo? TypeClass { get; set; }
        public string Type { get; set; } = "";

        public string Name { get; set; } = "";

        public string FullType { get; set; } = "";

        public string RefKind { get; set; } = "";

        public bool IsOptional { get; set; }

        public bool HasExplicitDefault { get; set; }

        public object? DefaultValue { get; set; } = null;
    }
}
