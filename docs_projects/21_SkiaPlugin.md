# SkiaPlugin (Plugin SVG/Skia)

## Descripción

SkiaPlugin (también conocido como SvgPlugin) proporciona renderizado basado en Skia para SVG, Canvas, Arc, RoundedRectangle, Circle, y animaciones Lottie en Gum. Añade tipos de elementos estándar para gráficos vectoriales y propiedades personalizadas para cada tipo.

## Diagrama de Relaciones

```mermaid
graph TB
    SkiaPlugin[SkiaPlugin - SVG/Skia]
    
    SkiaPlugin --> Gum[Gum Tool]
    SkiaPlugin --> KniGum[KniGum]
    SkiaPlugin --> SkiaSharp
    SkiaPlugin --> SkiaSharp.Extended
    SkiaPlugin --> SkiaSharp.Skottie
    SkiaPlugin --> Svg.Skia
    
    subgraph PluginBase
        PluginBase[PluginBase]
    end
    
    SkiaPlugin -.->|Extends| PluginBase
    
    subgraph Events
        CreateRenderableForType[CreateRenderableForType]
        GetDefaultStateForType[GetDefaultStateForType]
        VariableExcluded[VariableExcluded]
        VariableSet[VariableSet]
        ReactToFileChanged[ReactToFileChanged]
    end
    
    SkiaPlugin --> Events
    
    subgraph Managers
        DefaultStateManager[DefaultStateManager]
        StandardAdder[StandardAdder]
    end
    
    SkiaPlugin --> Managers
    
    subgraph Renderables
        RenderableSvg[RenderableSvg]
        RenderableLottieAnimation[RenderableLottieAnimation]
        RenderableCanvas[RenderableCanvas]
        RenderableArc[RenderableArc]
        RenderableRoundedRectangle[RenderableRoundedRectangle]
        RenderableCircle[RenderableCircle]
    end
    
    Managers --> Renderables
    
    subgraph StandardElements
        Svg[Canvas/Svg]
        Arc[Arc]
        RoundedRectangle[RoundedRectangle]
        Circle[Circle]
        LottieAnimation[LottieAnimation]
    end
    
    Renderables -.->|Render| StandardElements
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | WPF + SkiaSharp |
| **.NET** | net8.0-windows |
| **Graphics** | SkiaSharp 3.119+ |
| **SVG** | Svg.Skia |
| **Lottie** | SkiaSharp.Skottie |
| **Dependencias** | Gum.csproj, KniGum, WpfDataUi |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainSkiaPlugin.cs` | `MainSkiaPlugin` | `Gum/SvgPlugin/` |

```csharp
[Export(typeof(PluginBase))]
public class MainSkiaPlugin : PluginBase, IRecipient<UiBaseFontSizeChangedMessage>
{
    public override void StartUp()
    {
        // Suscribirse a eventos
        this.CreateRenderableForType += HandleCreateRenderbleFor;
        this.GetDefaultStateForType += HandleGetDefaultStateForType;
        this.VariableExcluded += DefaultStateManager.GetIfVariableIsExcluded;
        this.VariableSet += DefaultStateManager.HandleVariableSet;
        this.ReactToFileChanged += HandleFileChanged;
        this.IsExtensionValid += HandleIsExtensionValid;
        
        // Añadir menuitem
        AddMenuItem(new List<string> { "Plugins", "Add Skia Standard Elements" });
    }
}
```

## Elementos Estándar Añadidos

| Tipo | Propósito | Renderable |
|------|-----------|------------|
| **Canvas** | Container para dibujos vectoriales | RenderableCanvas |
| **Svg** | Imágenes SVG escalables | RenderableSvg |
| **Arc** | Arcos y círculos parciales | RenderableArc |
| **RoundedRectangle** | Rectángulos con esquinas redondeadas | RenderableRoundedRectangle |
| **Circle** | Círculos perfectos | RenderableCircle |
| **LottieAnimation** | Animaciones vectoriales (.json) | RenderableLottieAnimation |

## Propiedades por Elemento

### Canvas/Svg

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Source` | string | Ruta al archivo SVG |
| `Width` | float | Ancho |
| `Height` | float | Alto |
| `Color` | Color | Tinte de color |

### LottieAnimation

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Source` | string | Ruta al archivo .json/.lottie |
| `IsPlaying` | bool | Reproduciendo |
| `Speed` | float | Velocidad (default 1.0) |
| `CurrentTime` | float | Tiempo actual |
| `Width` | float | Ancho |
| `Height` | float | Alto |

### Arc

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `StartAngle` | float | Ángulo inicial (grados) |
| `SweepAngle` | float | Ángulo del arco (grados) |
| `Radius` | float | Radio |
| `StrokeWidth` | float | Grosor del trazo |
| `Color` | Color | Color del trazo |
| `Fill` | bool | Relleno vs trazo |

### RoundedRectangle

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `CornerRadius` | float | Radio de las esquinas |
| `Width` | float | Ancho |
| `Height` | float | Alto |
| `Color` | Color | Color de relleno |

## Cómo Ampliar

### Añadir Nuevo Tipo de Elemento

```csharp
// 1. Crear clase renderable
public class RenderableCustom : IRenderable
{
    public void Render(ISystemManagers managers)
    {
        var skiaManagers = managers as SkiaSystemManagers;
        var canvas = skiaManagers.Canvas;
        
        // Implementar renderizado personalizado
        using var paint = new SKPaint
        {
            Color = SKColors.Blue,
            Style = SKPaintStyle.Fill
        };
        
        canvas.DrawRect(0, 0, Width, Height, paint);
    }
}

// 2. Registrar en el plugin
private IRenderable HandleCreateRenderbleFor(string type)
{
    return type switch
    {
        "Canvas" => new RenderableCanvas(),
        "Svg" => new RenderableSvg(),
        // ... otros tipos"Custom" => new RenderableCustom(),
        _ => null
    };
}

// 3. Añadir estado por defecto
private StateSave HandleGetDefaultStateForType(string type)
{
    if (type == "Custom")
    {
        var state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "Width", Value = 100f });
        state.Variables.Add(new VariableSave { Name = "Height", Value = 100f });
        state.Variables.Add(new VariableSave { Name = "Color", Value = "'0,0,255,255'" });
        return state;
    }
    return null;
}
```

### Cargar Archivo SVG

```csharp
// En el editor Gum:
// 1. Crear elemento tipo Canvas o Svg
// 2. Establecer propiedad Source a ruta del archivo .svg
// 3. El archivo se renderizará con SkiaSharp

// Programáticamente:
var svg = new RenderableSvg();
svg.SetProperty("Source", "Assets/icon.svg");
svg.SetProperty("Width", 64f);
svg.SetProperty("Height", 64f);
container.AddChild(svg);
```

### Reproducir Animación Lottie

```csharp
var lottie = new RenderableLottieAnimation();
lottie.SetProperty("Source", "Animations/loading.json");
lottie.SetProperty("IsPlaying", true);
lottie.SetProperty("Speed", 1.0f);

// En el loop de actualización:
lottie.Update(gameTime.ElapsedGameTime);
```

## Retos al Ampliar

### Rendimiento SVG Complejo
- SVGs grandes pueden ser lentos
- Animaciones Lottie consumen CPU
- **Recomendación**: Cachear bitmaps para SVGs estáticos

### Integración con KNI
- SkiaPlugin usa KNI para preview
- Diferentes sistemas de coordenadas
- **Recomendación**: Usar transformaciones consistentes

### Recursos Embebidos
- SVGs y Lotties son archivos externos
- Distribución con el juego
- **Recomendación**: Usar Content Pipeline o embeber

### Tamaño de Archivos
- Lotties pueden ser grandes
- Carga asíncrona necesaria
- **Recomendación**: Pre-cargar durante loading screens

## Dependencias SkiaSharp

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| SkiaSharp | 3.119+ | Core rendering |
| SkiaSharp.Extended | 1.60+ | Extensiones |
| SkiaSharp.Skottie | 3.119+ | Lottie animations |
| Svg.Skia | 0.5+ | SVG rendering |
| Topten.RichTextKit | (via SkiaGum) | Rich text |