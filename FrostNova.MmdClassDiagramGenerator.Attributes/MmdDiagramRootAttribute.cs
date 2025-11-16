using System;

namespace FrostNova.MmdClassDiagramGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MmdDiagramRootAttribute : MmdDiagramTargetAttribute
    {
        public Type ViewModelType { get; set; }

        public string[] GroupNames { get; set; }
      

    }
}
