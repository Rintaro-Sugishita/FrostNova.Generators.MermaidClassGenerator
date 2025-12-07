using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FrostNova.MmdClassDiagramBase.Data
{
    [DebuggerDisplay("Full:{FullName}")]
    public class ClassInfo
    {

        public List<string> Groups { get; set; } = new();
        public bool IsFirstRead { get; set; } = true;
        public bool IsRoot { get; set; } = false;
        public bool IsIgnore { get; set; } = false;

        public INamedTypeSymbol? VmTypeSymbol { get; set; } = null;


        public ITypeSymbol Symbol { get; set; }

        public List<string> FilePaths { get; set; } = new List<string>();

        public string FullName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;

        public string Kind {  get; set; } = string.Empty;
        public Accessibility Accessibility {  get; set; } = Accessibility.NotApplicable;
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsInterface { get; set; } = false;
        public bool IsEnum { get; set; } = false;


        public string[] TypeParameters { get; set; } = new string[0];

        public ClassInfo? BaseType { get; set; }
        public List<ClassInfo> GenericTypes { get; set; } = new List<ClassInfo>();
        public List<string> GenericArgumentNames { get; set; } = new List<string>();


        public List<MemberInfo> Properties { get; set; } = new List<MemberInfo>();
        public List<MemberInfo> Fields { get; set; } = new List<MemberInfo>();

        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();

        public ClassInfo[] NestedTypes { get; set; } = new ClassInfo[0];

        public List<ClassInfo> ImplementedInterfaces { get; set; } = new ();

        public HashSet<ClassInfo> Depends { get; set; } = new();


        public ClassInfo(ITypeSymbol symbol)
        {
            Symbol = symbol;
            Name = symbol.Name;
            Namespace = symbol.ContainingNamespace?.ToDisplayString()??"";
            FullName = string.IsNullOrEmpty(Namespace) ? 
                                            Name : 
                                            Namespace + "." + Name;

        }

    }
}
