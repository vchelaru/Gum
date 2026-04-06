# ImportFromGumxPlugin (Plugin de Importación)

## Descripción

ImportFromGumxPlugin permite importar elementos (Screens, Components, Behaviors, Standards) desde archivos de proyecto Gum externos (`.gumx`). Soporta importación desde archivos locales y URLs remotas (http/https), resolviendo automáticamente las dependencias entre elementos.

## Diagrama de Relaciones

```mermaid
graph TB
    ImportFromGumxPlugin[ImportFromGumxPlugin]
    
    ImportFromGumxPlugin --> Gum[Gum Tool]
    ImportFromGumxPlugin --> CommunityToolkit.Mvvm
    ImportFromGumxPlugin --> WpfDataUi[WpfDataUi]
    ImportFromGumxPlugin --> InputLibrary[InputLibrary]
    
    subgraph PluginBase
        PluginBase[PluginBase]
    end
    
    ImportFromGumxPlugin -.->|Extends| PluginBase
    
    subgraph Services
        GumxImportService[GumxImportService]
        GumxDependencyResolver[GumxDependencyResolver]
        GumxSourceService[GumxSourceService]
    end
    
    ImportFromGumxPlugin --> Services
    
    subgraph ViewModels
        ImportFromGumxViewModel[ImportFromGumxViewModel]
        ImportTreeNodeViewModel[ImportTreeNodeViewModel]
        ImportPreviewItemViewModel[ImportPreviewItemViewModel]
    end
    
    ImportFromGumxPlugin --> ViewModels
    
    subgraph Steps
        SelectSource[1. Select Source File]
        LoadPreview[2. Load Preview]
        SelectElements[3. Select Elements]
        ResolveDependencies[4. Resolve Dependencies]
        CopyAssets[5. Copy Assets]
        ImportComplete[6. Import Complete]
    end
    
    Services --> Steps
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
| `MainImportFromGumxPlugin.cs` | `MainImportFromGumxPlugin` | `Gum/ImportFromGumxPlugin/` |

```csharp
[Export(typeof(PluginBase))]
internal class MainImportFromGumxPlugin : InternalPlugin
{
    public override void StartUp()
    {
        // Añadir item al menú
        AddMenuItemTo("Import from .gumx...", HandleImportFromGumx, "Content");
    }
    
    private void HandleImportFromGumx()
    {
        var vm = new ImportFromGumxViewModel();
        var window = new ImportFromGumxWindow { DataContext = vm };
        window.ShowDialog();
    }
}
```

## Flujo de Importación

```
┌─────────────────┐
│ 1. Select Source│
│   - File Browse │
│   - URL Input   │
└────────┬────────┘│
         ▼
┌─────────────────┐
│ 2. Load Preview │
│   - Parse .gumx │
│   - Build Tree  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 3. Select Items │
│   - Checkboxes  │
│   - Preview     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 4. Dependencies │
│   - Topo Sort   │
│   - Order Items │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 5. Copy Assets  │
│   - Textures    │
│   - Fonts       │
│   - Other Files │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 6. Import Elem. │
│   - Copy .gucx  │
│   - Copy .gusx  │
│   - Update refs │
└─────────────────┘
```

## Clases Principales

### GumxImportService

| Método | Propósito |
|--------|-----------|
| `ImportElements()` | Importa elementos seleccionados |
| `ResolveDependencies()` | Ordena elementos por dependencias |
| `CopyAssets()` | Copia archivos assets |
| `UpdateReferences()` | Actualiza referencias de elementos |

### GumxDependencyResolver

| Método | Propósito |
|--------|-----------|
| `GetLoadOrder()` | Retorna orden topológico de carga |
| `GetDependencies()` | Obtiene dependencias de un elemento |
| `HasCircularDependency()` | Detecta dependencias circulares |

### GumxSourceService

| Método | Propósito |
|--------|-----------|
| `LoadFromFile()` | Carga desde archivo local |
| `LoadFromUrl()` | Carga desde URL remota |
| `IsValidSource()` | Valida si es un .gumx válido |

## Cómo Ampliar

### Importar desde Archivo Local

```csharp
var service = new GumxImportService();
var result = await service.ImportElementsAsync(
    sourcePath: "C:/ExternalProject/External.gumx",
    elements: new[] { "Button", "MainMenu", "ButtonBehavior" },
    destinationProject: "MyProject.gumx",
    options: new ImportOptions
    {
        OverwriteExisting = false,
        CopyAssets = true,
        PreserveFolderStructure = true
    }
);

if (result.Success)
{
    Console.WriteLine($"Imported {result.ImportedElements.Count} elements");
}
else
{
    Console.WriteLine($"Failed: {result.ErrorMessage}");
}
```

### Importar desde URL

```csharp
var sourceService = new GumxSourceService();
var importService = new GumxImportService();

// Cargar desde URL
var content = await sourceService.LoadFromUrlAsync(
    "https://example.com/assets/UI.gumx"
);

// Parsear y obtener elementos disponibles
var gumxProject = content.Parse();
var screens = gumxProject.Screens.Select(s => s.Name).ToList();
var components = gumxProject.Components.Select(c => c.Name).ToList();

// Importar seleccionados
var result = await importService.ImportElementsAsync(
    sourceUrl: "https://example.com/assets/UI.gumx",
    elements: screens.Concat(components),
    destinationProject: "MyProject.gumx"
);
```

### Añadir Tipo de Asset Personalizado

```csharp
public class MyAssetTypeHandler
{
    public IEnumerable<string> CollectAssetPaths(
        GumProjectSave sourceProject,
        IEnumerable<string> elementsToImport)
    {
        var paths = new List<string>();
        
        foreach (var element in GetElements(sourceProject, elementsToImport))
        {
            // Buscar assets personalizados en variables
            foreach (var variable in element.DefaultState.Variables)
            {
                if (variable.Name == "MyCustomAsset")
                {
                    paths.Add(variable.Value as string);
                }
            }
        }
        
        return paths;
    }
}

// Registrar en GumxImportService
importService.AssetHandlers.Add(new MyAssetTypeHandler());
```

### Manejar Conflictos de Nombres

```csharp
public class NameConflictHandler
{
    public string ResolveConflict(string elementName, bool isFromImport)
    {
        // Existente en proyecto destino
        if (!isFromImport && ProjectHasElement(elementName))
        {
            // Preguntar al usuario
            var result = ShowConflictDialog(elementName);
            
            switch (result)
            {
                case ConflictResolution.Rename:
                    return $"{elementName}_Imported";
                case ConflictResolution.Replace:
                    return elementName;
                case ConflictResolution.Skip:
                    return null;
            }
        }
        
        return elementName;
    }
}

// Opciones disponibles
public enum ConflictResolution
{
    Rename,
    Replace,
    Skip
}
```

## Retos al Ampliar

### Dependencias Circulares
- A depende de B, B depende de A
- GumxDependencyResolver detecta esto
- **Recomendación**: Mostrar error claro al usuario

### Assets Referenciados
- Archivos de textura, fuentes, etc.
- Rutas relativas vs absolutas
- **Recomendación**: Usar rutas relativas al proyecto

### URLs Remotas
- Descarga puede fallar
- Cache para reintentos
- **Recomendación**: Timeout configurable y retry logic

### Versioning
- Proyectos de 不同 versiones de Gum
- Formato puede diferir
- **Recomendación**: Migración automática si es posible

### Nombre en Metadata
- Element names pueden tener caracteres especiales
- Normalización necesaria
- **Recomendación**: Sanitize names antes de importar

## Tipos de Elementos Importables

| Tipo | Descripción |
|------|-------------|
| Screen | Pantalla completa de UI |
| Component | Componente reutilizable |
| StandardElement | Elemento estándar (Text, Sprite, etc.) |
| Behavior | Comportamiento reutilizable |