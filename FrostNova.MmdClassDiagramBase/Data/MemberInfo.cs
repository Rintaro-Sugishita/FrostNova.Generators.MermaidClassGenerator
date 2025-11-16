using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.MmdClassDiagramBase.Data
{
    public class MemberInfo
    {
        public ITypeSymbol TypeSymbol { get; set; }
        public ClassInfo? TypeInfo { get; set; }

        public string Name { get; set; }

        public Accessibility Accessibility { get; set; }

        public bool IsStatic { get; set; }

        public bool IsReadOnly { get; set; }

        public MemberInfo(IPropertySymbol property)
        {
            Name = property.Name;
            IsStatic = property.IsStatic;
            IsReadOnly = property.IsReadOnly;
            Accessibility = property.DeclaredAccessibility;

            TypeSymbol = property.Type;
        }

        public MemberInfo(IFieldSymbol field)
        {
            Name = field.Name;
            IsStatic = field.IsStatic;
            IsReadOnly = field.IsReadOnly;
            Accessibility = field.DeclaredAccessibility;

            TypeSymbol = field.Type;
        }

        //public List<string> Depends { get; set; }
    }
}
