# GumFormsPlugin (Plugin de Forms UI)

## Descripción

GumFormsPlugin importa componentes de UI pre-construidos (Forms) en proyectos Gum. Proporciona un diálogo para seleccionar cuales componentes Forms importar, incluyendo Button, TextBox, ScrollViewer, ListBox, y otros controles de UI comunes.

## Diagrama de Relaciones

```mermaid
graph TB
    GumFormsPlugin[GumFormsPlugin]
    
    GumFormsPlugin --> Gum[Gum Tool]
    GumFormsPlugin --> WpfDataUi[WpfDataUi]
    GumFormsPlugin --> InputLibrary[InputLibrary]
    
    subgraph PluginBase
        PluginBase[PluginBase]
    end
    
    GumFormsPlugin -.->|Extends| PluginBase
    
    subgraph Services
        FormsFileService[FormsFileService]
    end
    
    GumFormsPlugin --> Services
    
    subgraph ViewModels
        AddFormsViewModel[AddFormsViewModel]
    end
    
    GumFormsPlugin --> ViewModels
    
    subgraph Views
        AddFormsWindow[AddFormsWindow.xaml]
    end
    
    ViewModels -.->|Data Binds| Views
    
    subgraph Events
        ProjectLoad[ProjectLoad]
        AfterProjectSave[AfterProjectSave]
    end
    
    GumFormsPlugin --> Events
    
    subgraph Templates
        Button[ButtonComponent]
        TextBox[TextBoxComponent]
        ScrollViewer[ScrollViewer]
        ListBox[ListBox]
        ComboBox[ComboBox]
        CheckBox[CheckBox]
        RadioButton[RadioButton]
        Slider[Slider]
    end
    
    services -.->|Copies| Templates
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | WPF |
| **.NET** | net8.0-windows |
| **MVVM** | CommunityToolkit.Mvvm |
| **Dependencias** | Gum.csproj, WpfDataUi, InputLibrary |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainGumFormsPlugin.cs` | `MainGumFormsPlugin` | `Gum/GumFormsPlugin/` |

```csharp
[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PriorityPlugin
{
    public override void StartUp()
    {
        // Suscribirse a eventos
        this.ProjectLoad += HandleProjectLoaded;
        this.AfterProjectSave += HandleProjectSave;
        
        // Añadir item al menú
        AddMenuItemTo("Add Forms Components", HandleAddFormsComponents, "Content");
    }
}
```

## Componentes Disponibles

| Componente | Descripción | Archivo |
|-----------|------------|---------|
| **Button** | Botón con estados Normal, Hover, Pushed | `Components/Button.gucx` |
| **TextBox** | Campo de entrada de texto | `Components/TextBox.gucx` |
| **ScrollViewer** | Container con scroll | `Components/ScrollViewer.gucx` |
| **ListBox** | Lista de items seleccionables | `Components/ListBox.gucx` |
| **ComboBox** | Dropdown con selección | `Components/ComboBox.gucx` |
| **CheckBox** | Checkbox con estado | `Components/CheckBox.gucx` |
| **RadioButton** | Radio button con grupo | `Components/RadioButton.gucx` |
| **Slider** | Slider de valor | `Components/Slider.gucx` |

## Cómo Ampliar

### Añadir Nuevo Componente Template

1. **Crear el componente en `Templates/FormsTemplate/`**:

```
Templates/FormsTemplate/
├── Components/
│   ├── MyButton.gucx
│   └── MyButton.gucx.codegen
├── Screens/
│   └── DemoScreen.gusx
└── Standards/
    └── MyCustomStandard.gutx
```

2. **Actualizar `FormsFileService.cs`**:

```csharp
public static class FormsFileService
{
    public static string GetFormsTemplateFolder()
    {
        return Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "Templates",
            "FormsTemplate"
        );
    }
    
    public static List<string> GetAvailableComponents()
    {
        return new List<string>
        {
            "Button",
            "TextBox",
            "ScrollViewer",
            "ListBox",
            "MyButton" // Nuevo componente
        };
    }
}
```

3. **Actualizar `AddFormsViewModel.cs`**:

```csharp
public class AddFormsViewModel
{
    public ObservableCollection<FormsComponentViewModel> Components { get; }= new();
    
    public AddFormsViewModel()
    {
        Components.Add(new FormsComponentViewModel("Button", "Standard button"));
        Components.Add(new FormsComponentViewModel("TextBox", "Text input"));
        // ... otros componentes
        Components.Add(new FormsComponentViewModel("MyButton", "Custom button"));
    }
}
```

### Usar Desde el Editor

```
1. Menú Content > Add Forms Components
2. Dialog muestra lista de componentes disponibles
3. Seleccionar componentes deseados
4. Click "Add Selected"
5. Componentes se copian a:
   - Components/ (si es componente)
   - Standards/ (si es standard)
6. Archivos de assets se copian a Content/
```

### Importar Programáticamente

```csharp
// API programáticavar formsService = new FormsFileService();
var components = new[] { "Button", "TextBox", "ScrollViewer" };

foreach (var component in components)
{
    formsService.CopyComponentToProject(
        componentName: component,
        destinationProject: "MyProject.gumx",
        overwrite: false
    );
}

// Recargar proyecto para ver cambios
ProjectManager.ReloadProject();
```

## Templates

Los templates se encuentran en `Tools/Gum.ProjectServices/Templates/FormsTemplate/`:

| Archivo/Carpeta | Contenido |
|-----------------|-----------|
| `Components/*.gucx` | Componentes Forms |
| `Standards/*.gutx` | Elementos estándar |
| `Screens/*.gusx` | Pantallas de ejemplo |
| `Standards/StandardGraphics/` | Imágenes estándar |
| `Content/Fonts/` | Fuentes |

## Retos al Ampliar

### Conflictos de Nombres
- Componentes existentes con mismo nombre
- Sobrescritura accidental
- **Recomendación**: Usar `overwrite: false` y verificar conflictos

### Dependencias de Assets
- Componentes dependen de archivos assets
- Copia incompleta de assets
- **Recomendación**: Usar `CopyDirectory` recursivo

### ReferenciasEntre Componentes
- Button referencia Text standard
- ScrollViewer referencia Container
- **Recomendación**: Copiar en orden de dependencias

### Versioning de Templates
- Templates actualizados pueden romper proyectos
- Falta de migración
- **Recomendación**: Incluir versión en templates y detectar versiones antiguas