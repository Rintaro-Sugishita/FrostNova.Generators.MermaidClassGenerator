using FrostNova.MmdClassDiagramBase;
using FrostNova.MmdClassDiagramGenerator;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;

//analyze arguments
var slnPath = "";
if (args.Length > 0)
{
    slnPath = args[0];
}

if (args.Length > 0)
{
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg == "--help" || arg == "-h")
        {
            Console.WriteLine("Usage: FrostNova.MmdClassDiagramGenerator <solution-path>");
            return 0;
        }
        else if(arg == "--version" || arg == "-v")
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"FrostNova.MmdClassDiagramGenerator version {version}");
            return 0;

        }
        else if(arg == "--genconfig")
        {
            var newConfig = new DiagramConfig();
            var json = DiagramConfigJson.Serialize(newConfig);
            File.WriteAllText("diagramgen.config.json", json);
            Console.WriteLine("Generated default configuration file: diagramgen.config.json");
            return 0;
        }
        else
        {
            Console.WriteLine($"Unknown argument: {arg}");
            return 1;
        }
    }
}


if (!File.Exists(slnPath) && File.Exists("path.txt"))
{
    slnPath = File.ReadAllText("path.txt");
}
if (!File.Exists(slnPath))
{
    Console.WriteLine($"Error: Solution file not found: {slnPath}");
    return 1;
}

var config = DiagramConfig.LoadConfig(Path.Combine(Path.GetDirectoryName(slnPath)!, "diagramgen.config.json"));

//analyze classes
SlnAnalyzer analyzer = new();
var list = await analyzer.AnalyzeSolution(slnPath, config);
var depends = analyzer.AnalyzeDependencies(list, config);

Console.WriteLine("Completed analyze.");
Console.WriteLine($"  Found {list.Count(x => x.IsRoot)} classes.");
Console.WriteLine($"  Found {depends.Count} relationships.");

Console.WriteLine();

//output files
var outputDir = Path.Combine(Path.GetDirectoryName(slnPath)!, "mermaid");

if (Directory.Exists(outputDir))
{
    //delete all files.
    var files = Directory.GetFiles(outputDir, "*.*", new EnumerationOptions() { RecurseSubdirectories = true });

    foreach (var file in files)
    {
        File.Delete(file);
    }
}
Directory.CreateDirectory(outputDir);

MermaidBuilder creator = new MermaidBuilder();
creator.Output(depends, config, outputDir);

Console.WriteLine("operation completed !!");

return 0;

