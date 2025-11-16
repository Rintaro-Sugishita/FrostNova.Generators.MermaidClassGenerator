# FrostNova.Generators.MermaidClassGenerator
mermaid class diagram generator


## How to use
Start FrostNova.MmdClassDiagramGenerator.exe by passing the full path of the sln file you want to analyze.
A mermaid folder will be created in the solution folder and a mermaid class diagram will be output.

## Project settings
Please install FrostNova.Generators.MermaidClassGenerator.Attributes.
Add the `MmdDiagramRoot` attribute to the root element of the class diagram.
The following properties can be set for MmdDiagramRootAttribute.

- GroupNames: You can set output groups. Specify the group names in an array.
- ViewModelType: Set the ViewModel to use from `System.Windows.Window`. `Dependency` relationships are built in the class diagram.

## Diagram settings
Create a `diagramgen.config.json` file in your solution folder.

### Properties
- Mode  
  unsupported.
- OutputMode  
  - One [default]    
    Output to a single file.
  - Multiple  
    The output will be split into several files.
- OutputPath  
  unsupported.
  output path is $(solutionDir)mermaid.
- OutputType  
  - md, markdown  
    Output in markdown format.
  - mmd, mermaid  
    Output in mermaid format.
- ActiveBuildConfigurations  
  unsupported.
- MethodAccessibility  
  Sets the lowest output access modifier for a function.
  - private [default]
  - private protected  
  - internal  
  - protected internal  
  - protected  
  - public  
- PropertyAccessibility  
  Sets the lowest output access modifier for a property.
  - private  
  - private protected  
  - internal  
  - protected internal  
  - protected  
  - public  [default]
- FieldAccessibility  
  Sets the lowest output access modifier for a field.
  - private  
  - private protected  
  - internal  
  - protected internal  
  - protected  
  - public  [default]
- RootAttributes  
  unsupported. 


### Sample
```json
{
    "Mode": "Full",
    "OutputMode": "One",
    "ExcludeNameSpaces": [
        "System",
        "Microsoft"
    ],
    "OutputPath": "docs/diagrams",
    "OutputType": "md",
    "ActiveBuildConfigurations": [
        "Release",
        "Diagram"
    ], 
    "MethodAccessibility": "private",
    "PropertyAccessibility": "public",
    "FieldAccessibility": "public",
    "RootAttributes": [
        "FrostNova.MmdClassDiagramGenerator.GenerateClassDiagramAttribute"
    ]
}
```