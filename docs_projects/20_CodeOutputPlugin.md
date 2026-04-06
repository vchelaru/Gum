# CodeOutputPlugin (Plugin de Generación de Código)

## Descripción

CodeOutputPlugin genera código C# automáticamente desde elementos Gum (Screens y Components). Permite crear clases con propiedades tipadas, instanciación de elementos, y binding de variables, facilitando la integración de UI en proyectos de juego sin código manual repetitivo.

## Diagrama de Relaciones

```mermaid
graph TB
    CodeOutputPlugin[CodeOutputPlugin]
    
    CodeOutputPlugin --> Gum[Gum Tool]
    CodeOutputPlugin --> WpfDataUi[WpfDataUi]
    CodeOutputPlugin --> Newtonsoft.Json
    CodeOutputPlugin --> System.ComponentModel.Composition
    
    subgraph PluginBase
        PluginBase[PluginBase]
    end
    
    CodeOutputPlugin -.->|Extends| PluginBase
    
    subgraph Managers
        CodeGenerationService[CodeGenerationService]
        CodeGenerator[CodeGenerator]
        RenameService[RenameService]
        ParentSetLogic[ParentSetLogic]
    end
    
    CodeOutputPlugin --> Managers
    
    subgraph Events
        ElementSelected[ElementSelected]
        ElementRename[ElementRename]
        VariableAdd[VariableAdd]
        VariableSet[VariableSet]
        InstanceAdd[InstanceAdd]
        InstanceDelete[InstanceDelete]
    end
    
    CodeOutputPlugin --> Events
    
    subgraph Output
        PartialClasses[Partial Classes]
        Properties[Properties]
        Instantiation[Instantiation Code]
        VariableBindings[Variable Bindings]
    end
    
    Managers --> Output
    
    subgraph Settings
        CodeGenerationElementSettings[Element Settings]
        OutputLibrary[Output Library Selection]
        GenerationBehavior[Generate vs Add]
    end
    
    CodeOutputPlugin --> Settings
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | WPF |
| **.NET** | net8.0-windows |
| **Lenguaje** | C# 12.0 |
| **Code Generation** | System.CodeDom / String Building |
| **MVVM** | CommunityToolkit.Mvvm |
| **Dependencias** | Gum.csproj, WpfDataUi, Gum.ProjectServices |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainCodeOutputPlugin.cs` | `MainCodeOutputPlugin` | `Gum/CodeOutputPlugin/` |

```csharp
[Export(typeof(PluginBase))]
public class MainCodeOutputPlugin : InternalPlugin
{
    public override void StartUp()
    {
        // Suscribirse a eventos del editor
        this.ElementSelected += HandleElementSelected;
        this.ElementRename += HandleRename;
        this.VariableSet += HandleVariableSet;
        this.InstanceAdd += HandleInstanceAdd;
        // ... más suscripciones
        
        // Suscribirse a mensajes MVVM
        _messenger.Register<RequestCodeGenerationMessage>(this, HandleRequest);
    }
}
```

## Funcionalidades

### Generación de Partial Classes
- Clases parciales para extensión manual
- Archivos `.Generated.cs` (auto-generados)
- Archivos `.cs` (código del usuario)

### Propiedades Tipadas
- Variables de Gum → Propiedades C#
- Tipos automáticamente inferidos
- Nullable support

### Instanciación Automática
- Instancias de Gum → Camposprivados
- Inicialización en constructor
- Wire-up en runtime

### Binding de Variables
- Variables expuestas → Propiedades públicas
- Setters que actualizan Gum
- Getters que leen de Gum

### Herencia
- Componentes Gum → Herencia C#
- Base types configurables
- Interfaces support

## Configuración

### Output Libraries

| Library | Namespace | Uso|
|---------|-----------|-----|
| MonoGame | Gum.MonoGame | Juegos MonoGame |
| KNI | Gum.KNI | Juegos KNI |
| FNA | Gum.FNA | Juegos FNA |
| Skia | Gum.Skia | Apps de escritorio |
| Raylib | Gum.Raylib | Juegos Raylib |

### Generation Behaviors

| Behavior | Descripción |
|----------|-------------|
| Generate | Genera archivo nuevo |
| Add | Añade a archivo existente |
| Skip | No genera código |

## Cómo Ampliar

### Configurar Code Generation

```csharp
// En el elemento Gum, configurar:
// 1. Code Generation Settings
var settings = new CodeGenerationElementSettings();
settings.OutputLibrary = OutputLibrary.MonoGame;
settings.GenerationBehavior = GenerationBehavior.Generate;
settings.OutputDirectory = "Generated/UI/";
settings.Namespace = "MyGame.UI";

// 2. Variables a exponer
// En el editor: marcar variable como "Exposed"
// O programáticamente:
elementSave.DefaultState.Variables
    .First(v => v.Name == "Text")
    .IsExcluded = false;
```

### Código Generado Ejemplo

```csharp
// MainMenu.gusx.Generated.cs
namespace MyGame.UI
{
    public partial class MainMenu : GumRuntime.GraphicalUiElement
    {
        // Variables expuestas
        public string TitleText
        {
            get => _titleTextInstance.Text;
            set => _titleTextInstance.Text = value;
        }
        
        // Instancias
        private GumRuntime.GraphicalUiElement _titleTextInstance;
        private GumRuntime.GraphicalUiElement _buttonInstance;
        
        public void Initialize()
        {
            _titleTextInstance = GetChildNamed("TitleText") as GumRuntime.GraphicalUiElement;
            _buttonInstance = GetChildNamed("Button") as GumRuntime.GraphicalUiElement;
        }
    }
}
```

### Extender el Código Generado

```csharp
// MainMenu.gusx.cs (código del usuario)
namespace MyGame.UI
{
    public partial class MainMenu
    {
        // Código custom aquí
        public void SetTitle(string title)
        {
            TitleText = title;
        }
        
        public void OnButtonClicked(Action callback)
        {
            _buttonInstance.Click += (_, _) => callback();
        }
    }
}
```

### Añadir Variable Custom Type Handler

```csharp
public class MyCustomTypeGenerator
{
    public string GenerateProperty(VariableSave variable)
    {
        if (variable.Type == "MyCustomType")
        {
            return $@"
        public {variable.Type} {variable.Name}
        {{
            get => ({variable.Type})GetVariable(""{variable.Name}"");
            set => SetVariable(""{variable.Name}"", value);
        }}";
        }
        return null;
    }
}

// Registrar en CodeGenerator
codeGenerator.CustomTypeHandlers.Add(new MyCustomTypeGenerator());
```

## Retos al Ampliar

### Round-Trip Code Generation
- Código manual se pierde si se regenera mal
- Partial classes mitigan esto
- **Recomendación**: Siempre usar`.Generated.cs` para código auto

### Naming Conflicts
- Nombres de variables Gum pueden chocar con C# keywords
- Espacios en nombres
- **Recomendación**: Sanitización de nombres en CodeGenerator

### Inheritance Depth
- Herencia profunda complicada
- Base types no siempre compatibles
- **Recomendación**: Documentar límites de herencia

### File Sync
- Archivos desincronizados con Gum
- Cambios manuales en generated files
- **Recomendación**: Regenerar después de cambios significativos

## Archivos de Configuración

```xml
<!-- MainMenu.gusx.codegen -->
<CodeGenerationSettings>
  <OutputLibrary>MonoGame</OutputLibrary>
  <GenerationBehavior>Generate</GenerationBehavior>
  <OutputDirectory>Generated/UI/</OutputDirectory>
  <Namespace>MyGame.UI</Namespace>
  <InheritanceLocation>Screen</InheritanceLocation>
  <CustomBaseType>MyGame.UI.ScreenBase</CustomBaseType>
</CodeGenerationSettings>
```

## Uso en Pipeline

```bash
# CLI
gumcli codegen MyProject.gumx --output ./Generated

# O configurar auto-generación en el editor
# Settings > Code Generation > Auto-generate on save
```