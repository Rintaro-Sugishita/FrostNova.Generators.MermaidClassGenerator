using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.MmdClassDiagramBase
{
    public class GroupConfig
    {
        public string GroupName { get; set; } = "";
        public bool OutputNamespace { get; set; } = true;
        public string[] ExcludeNamespace { get; set; } = [];
    }
}
