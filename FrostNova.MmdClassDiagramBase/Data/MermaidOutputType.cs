using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase.Data
{
    [Flags]
    public enum MermaidOutputType
    {
        None = 0,
        Markdown = 1, 
        Mermaid = 2 <<0, 
        png = 2 <<1, 
        DependJson = 2 <<2,
    }
}
