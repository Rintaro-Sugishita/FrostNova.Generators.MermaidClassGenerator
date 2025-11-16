using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace FrostNova.MmdClassDiagramBase.Data
{
    public class MethodInfo
    {
        public IMethodSymbol Symbol { get; set; }

        public ClassInfo? ReturnType { get; set; }
        public ITypeSymbol ReturnTypeSymbol { get; set; }


        public string Name { get; set; }


        public Accessibility Accessibility { get; set; }

        public bool IsStatic { get; set; }

        public bool IsExtensionMethod { get; set; }

        public MethodParameterInfo[] Parameters { get; set; }

        public MethodInfo(IMethodSymbol symbol)
        {
            Symbol = symbol;
            Name = symbol.Name;
            Accessibility = symbol.DeclaredAccessibility;
            IsStatic = symbol.IsStatic;
            IsExtensionMethod = symbol.IsExtensionMethod;
            ReturnTypeSymbol = symbol.ReturnType;
            Parameters = symbol.Parameters.Select(p => new MethodParameterInfo()
            {
                TypeSymbol = p.Type,
                Name = p.Name,
                Type = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                FullType = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", ""),
                RefKind = p.RefKind.ToString(),
                IsOptional = p.IsOptional,
                HasExplicitDefault = p.HasExplicitDefaultValue,
                DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null
            }).ToArray();
        }


    }
}
