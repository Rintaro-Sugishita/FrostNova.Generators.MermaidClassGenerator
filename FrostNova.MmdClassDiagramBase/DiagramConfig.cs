using FrostNova.MmdClassDiagramBase;
using FrostNova.MmdClassDiagramBase.Data;
using FrostNova.MmdClassDiagramBase.Json;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrostNova.MmdClassDiagramGenerator
{
    public class DiagramConfig
    {
        public string Mode { get; set; } = "Full"; // "Full" or "Selective"

        public bool IsFullMode => Mode == "Full";


        public string OutputMode { get; set; } = "One"; // "One" or "Multiple"

        public string[] ExcludeNameSpaces { get; set; } = ["System", "Microsoft"];

        public string OutputPath { get; set; } = "docs/diagrams";

        public string OutputType { get; set; } = "md";
        public string[] ActiveBuildConfigurations { get; set; } = new[] { "Release", "Diagram" };


        public bool OutputNamespace { get; set; } = true;

        [JsonConverter(typeof(AccessibilityConverter))]
        public Accessibility MethodAccessibility { get; set; } = Accessibility.Private;

        [JsonConverter(typeof(AccessibilityConverter))]
        public Accessibility PropertyAccessibility { get; set; } = Accessibility.Public;

        [JsonConverter(typeof(AccessibilityConverter))]
        public Accessibility FieldAccessibility { get; set; } = Accessibility.Public;

        public GroupConfig[] groupConfigs { get; set; } = [];

        static Accessibility GetAccessibility(string text)
        {
            if (Enum.TryParse<Accessibility>(text, ignoreCase: true, out var res))
            {
                return res;
            }
            if (string.Equals(text, "private protected", StringComparison.OrdinalIgnoreCase))
            {
                return Accessibility.ProtectedAndInternal;
            }
            else if (string.Equals(text, "protected internal", StringComparison.OrdinalIgnoreCase))
            {
                return Accessibility.ProtectedOrInternal;
            }
            return Accessibility.NotApplicable;

        }

        public string[] RootAttributes { get; set; } = new[] { "FrostNova.MmdClassDiagramGenerator.Attributes.MmdDiagramRootAttribute" };


        public static DiagramConfig LoadConfig(string configPath)
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Warning: Config file not found: {configPath}. Using defaults.");
                return new DiagramConfig();
            }

            try
            {
                var json = File.ReadAllText(configPath);

                //var config = JsonSerializer.Deserialize<DiagramConfig>(json);
                var config = DiagramConfigJson.Deserialize(json);

                if (config == null) throw new Exception("Deserialize returned null");
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading config: {ex.Message}");
                return new DiagramConfig();
            }
        }

        public MermaidOutputType GetOutputType()
        {
            var split = OutputType.Split(",");
            var types = MermaidOutputType.None;
            foreach (var type in split)
            {
                var lowerType = type.Trim().ToLower();
                var formalName = lowerType switch
                {
                    "mermaid" => MermaidOutputType.Mermaid,
                    "mmd" => MermaidOutputType.Mermaid,
                    "markdown" => MermaidOutputType.Markdown,
                    "md" => MermaidOutputType.Markdown,
                    "png" => MermaidOutputType.png,
                    _ => MermaidOutputType.Markdown
                };
                types = types | formalName;
            }
            if (types == MermaidOutputType.None)
            {
                return MermaidOutputType.Markdown;
            }
            return types;
        }

        public static string GetOutputExtension(MermaidOutputType type)
        {
            if (type.HasFlag(MermaidOutputType.Markdown))
            {
                return ".md";
            }
            else if (type.HasFlag(MermaidOutputType.Mermaid))
            {
                return ".mmd";
            }
            else if (type.HasFlag(MermaidOutputType.png))
            {
                return ".png";
            }
            return ".md";
        }
    }
}
