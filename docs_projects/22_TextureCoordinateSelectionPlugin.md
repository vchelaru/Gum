# TextureCoordinateSelectionPlugin (Plugin de Coordenadas de Textura)

## Descripción

TextureCoordinateSelectionPlugin proporciona un editor visual para coordenadas de textura de sprites y nine-slice. Permite seleccionar regiones de textura visualmente con zoom, pan, snap a grid, y soporte para coordenadas expuestas desde componentes padre.

## Diagrama de Relaciones

```mermaid
graph TB
    TextureCoordinateSelectionPlugin[TextureCoordinateSelectionPlugin]
    
    TextureCoordinateSelectionPlugin --> Gum[Gum Tool]
    TextureCoordinateSelectionPlugin --> MonoGameGum[MonoGameGum]
    TextureCoordinateSelectionPlugin --> KniGum[KniGum]
    TextureCoordinateSelectionPlugin --> InputLibrary[InputLibrary]
    TextureCoordinateSelectionPlugin --> XnaAndWinforms[XnaAndWinforms]
    TextureCoordinateSelectionPlugin --> FlatRedBall.SpecializedXnaControls
    
    subgraph PluginBase
        PluginBase[PluginBase]
    endTextureCoordinateSelectionPlugin -.->|Extends| PluginBase
    
    subgraph Events
        TreeNodeSelected[TreeNodeSelected]
        VariableSetLate[VariableSetLate]
        WireframeRefreshed[WireframeRefreshed]
        WireframePropertyChanged[WireframePropertyChanged]
        ProjectLoad[ProjectLoad]
    end
    
    TextureCoordinateSelectionPlugin --> Events
    
    subgraph Logic
        TextureCoordinateDisplayController[TextureCoordinateDisplayController]
        ExposedTextureCoordinateLogic[ExposedTextureCoordinateLogic]
        NineSliceGuideManager[NineSliceGuideManager]
        TextureOutlineManager[TextureOutlineManager]
        LineGridManager[LineGridManager]
    end
    
    TextureCoordinateSelectionPlugin --> Logic
    
    subgraph Views
        MainTextureCoordinateView[MainTextureCoordinateView]
    end
    
    Logic --> Views
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | WPF + WinForms (XNA integration) |
| **.NET** | net8.0-windows |
| **Graphics** | KNI (nkast.Xna.Framework) |
| **Dependencias** | Gum.csproj, MonoGameGum, InputLibrary, XnaAndWinforms |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainTextureCoordinatePlugin.cs` | `MainTextureCoordinatePlugin` | `Gum/TextureCoordinateSelectionPlugin/` |

```csharp
[Export(typeof(PluginBase))]
public class MainTextureCoordinatePlugin : PluginBase, IRecipient<UiBaseFontSizeChangedMessage>
{
    public override void StartUp()
    {
        // Suscribirse a eventos
        this.TreeNodeSelected += HandleTreeNodeSelected;
        this.VariableSetLate += HandleVariableSet;
        this.WireframeRefreshed += HandleWireframeRefreshed;
        this.WireframePropertyChanged += HandleWireframePropertyChanged;
        this.ProjectLoad += HandleProjectLoaded;
        
        // Registrar mensajes MVVM
        Locator.GetRequiredService<IMessenger>().RegisterAll(this);
    }
}
```

## Funcionalidades

### Selección Visual de Región
- Click y drag para seleccionar región
- Resize de selección con handles
- Mantener aspect ratio

### Zoom y Pan
- Zoom con scroll wheel
- Pan con drag (middle mouse/Space+drag)
- Fit to window

### Snap to Grid
- Grid configurable
- Snap a bordes de píxeles
- Customización de tamaño de grid

### Coordenadas Expuestas
- Variables de textura coordenadas expuestas desde componentes padre
- Edición de inner region para nine-slice
- Preview en tiempo real

### Nine-Slice Guide Overlays
- Líneas guía para nine-slice regions
- Visual feedback para inner borders
- Arrastrar para ajustar

## Cómo Ampliar

### Usar el Plugin

```csharp
// El plugin se activa cuando se selecciona:
// 1. Un Sprite con textura
// 2. Un NineSlice con textura
// 3. Cualquier elemento con TextureCoordinates expuestas

// En el editor Gum:
// 1. Seleccionar un Sprite
// 2. Ir a la propiedad "Texture Coordinates"
// 3. Click en el botón "..." para abrir el editor

// El editor muestra:
// - La textura completa
// - Región actualmente seleccionada (recuadro)
// - Grid overlay (opcional)
// - Zoom controls
```

### Coordenadas Expuestas desde Componente

```csharp
// En un componente padre:
// 1. Seleccionar la instancia de Sprite
// 2. Exponer "Texture Coordinate" variable
// 3. La instancia padre puede controlar las coordenadas

// ExposedTextureCoordinateLogic.cs maneja esto:
public class ExposedTextureCoordinateLogic
{
    public void ApplyExposedCoordinates(
        ElementSave parentElement,
        InstanceSave spriteInstance,
        StateSave state)
    {
        // Buscar variables expuestas de textura
        var exposedVars = state.Variables
            .Where(v => v.Name.StartsWith("Texture"))
            .ToList();
            
        // Aplicar al editor
        // ...
    }
}
```

### Añadir Overlay Custom

```csharp
public class MyCustomOverlay : ITextureOverlay
{
    public void Draw(GraphicsDevice device, Texture2D texture, Rectangle region)
    {
        // Dibujar overlay custom sobre la textura
        spriteBatch.Begin();
        spriteBatch.Draw(myOverlayTexture, region, Color.White);
        spriteBatch.End();
    }
}

// Registrar en el editor
textureCoordinateController.AddOverlay(new MyCustomOverlay());
```

## Clases Principales

### TextureCoordinateDisplayController

| Propiedad/Método | Propósito |
|------------------|-----------|
| `Texture` | Textura actual |
| `SelectedRegion` | Región seleccionada |
| `Zoom` | Nivel de zoom |
| `GridSize` | Tamaño del grid |
| `IsSnapEnabled` | Si snap está activo |

### NineSliceGuideManager

| Propiedad/Método | Propósito |
|------------------|-----------|
| `InnerLeft` | Límite izquierdo interior |
| `InnerRight` | Límite derecho interior |
| `InnerTop` | Límite superior interior |
| `InnerBottom` | Límite inferior interior |

## Retos al Ampliar

### Integración XNA/WPF
- Interop complejo entre XNA y WPF
- Gestión de GraphicsDevice
- **Recomendación**: Usar XnaAndWinforms como capa de abstracción

### Coordenadas de Textura Grandes
- Texturas muy grandes (4096x4096+) pueden ser lentas
- Zoom extrema causa aliasing
- **Recomendación**: Limitar zoom máximo o usar mipmaps

### Texturas Dinámicas
- Texturas que cambian en runtime
- Cache no se actualiza
- **Recomendación**: Invalidar cache al cambiar textura

### Nine-Slice Complejos
- Nine-slice con inner regions variables
- Sincronización entre editor y preview
- **Recomendación**: Usar bindings bidireccionales