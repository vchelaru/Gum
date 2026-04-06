# XnaAndWinforms (Integración XNA + WinForms)

## Descripción

XnaAndWinforms es una librería que proporciona integración entre XNA/MonoGame/KNI graphics y Windows Forms, permitiendo embeber rendering XNA dentro de controles WinForms. Es esencial para el editor Gum que necesita mostrar previews de UI en ventanas WinForms/WPF.

Permite usar la infraestructura gráfica de XNA (GraphicsDevice, SpriteBatch, etc.) dentro de aplicaciones WinForms.

## Diagrama de Relaciones

```mermaid
graph TB
    XnaAndWinforms[XnaAndWinforms - XNA/WinForms Integration]
    
    subgraph Consumers
        Gum[Gum Tool]
        TextureCoordinateSelectionPlugin[TextureCoordinateSelectionPlugin]
        GumFormsPlugin[GumFormsPlugin]
        CodeOutputPlugin[CodeOutputPlugin]
        ImportFromGumxPlugin[ImportFromGumxPlugin]
        EditorTabPlugin_XNA[EditorTabPlugin_XNA]
        FlatRedBall.SpecializedXnaControls[FlatRedBall.SpecializedXnaControls]
    end
    
    Gum --> XnaAndWinforms
    TextureCoordinateSelectionPlugin --> XnaAndWinforms
    GumFormsPlugin --> XnaAndWinforms
    CodeOutputPlugin --> XnaAndWinforms
    ImportFromGumxPlugin --> XnaAndWinforms
    EditorTabPlugin_XNA --> XnaAndWinforms
    FlatRedBall.SpecializedXnaControls --> XnaAndWinforms
    
    subgraph Dependencies
        KNI[nkast.Xna.Framework]
        KNI_Graphics[nkast.Xna.Framework.Graphics]
        KNI_Input[nkast.Xna.Framework.Input]
        KNI_WinForms[nkast.Kni.Platform.WinForms.DX11]
    end
    
    XnaAndWinforms --> Dependencies
    
    subgraph CoreClasses
        GraphicsDeviceControl[GraphicsDeviceControl]
        GraphicsDeviceService[GraphicsDeviceService]
        ServiceContainer[ServiceContainer]
    endXnaAndWinforms --> CoreClasses
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | Windows Forms + XNA (via KNI) |
| **.NET** | net8.0-windows |
| **Lenguaje** | C# 12.0 |
| **Allow Unsafe** | Sí |
| **Dependencias** | nkast.Xna.Framework.* (v4.1.9001) |

## Clases Principales

### GraphicsDeviceControl

| Propiedad/Método | Propósito |
|------------------|-----------|
| `GraphicsDevice` | GraphicsDevice de XNA |
| `ServiceContainer` | Contenedor de servicios |
| `DesiredFramesPerSecond` | FPS objetivo |
| `XnaDraw` | Evento de draw |
| `XnaUpdate` | Evento de update |
| `ErrorOccurred` | Evento de error |

### Eventos

| Evento | Propósito |
|--------|-----------|
| `XnaDraw` | Llamado cada frame para renderizar |
| `XnaUpdate` | Llamado cada frame para actualizar lógica |
| `ErrorOccurred` | Error de graphics device |

### GraphicsDeviceService

| Propiedad | Propósito |
|-----------|-----------|
| `GraphicsDevice` | El dispositivo compartido |
| `SingletonInstance` | Instancia singleton |

### ServiceContainer

| Método | Propósito |
|--------|-----------|
| `AddService()` | Añade servicio |
| `GetService()` | Obtiene servicio |

## Cómo Ampliar

### Crear Control Custom

```csharp
public class MyXnaControl : GraphicsDeviceControl
{
    private SpriteBatch _spriteBatch;
    private Texture2D _texture;
    
    protected override void Initialize()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        // Load content here
    }
    
    protected override void Draw()
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        _spriteBatch.Begin();
        _spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
        _spriteBatch.End();
    }
    
    protected override void Update()
    {
        // Update logic here
    }
}
```

### Manejar Errores de Device

```csharp
control.ErrorOccurred += (sender, args) =>
{
    // args.ErrorMessage contiene el error
    // args.DeviceLost indica si el device se perdió
    
    if (args.DeviceLost)
    {
        // Intentar recuperar
        args.Retry = true;
    }
    else
    {
        // Error fatal
        args.Retry = false;
    }
};
```

### Compartir GraphicsDevice

```csharp
// Múltiples controles pueden compartir el mismo GraphicsDevice
var service = GraphicsDeviceService.SingletonInstance;
var device = service.GraphicsDevice;

// Usar en múltiples controles
control1.GraphicsDevice = device;
control2.GraphicsDevice = device;
```

### Integración con WPF

```csharp
// Usar ElementHost para embeber en WPF
var host = new ElementHost();
host.Child = winFormsControl; // WinFormsXnaControl

// WPF window
<WindowsFormsHost x:Name="Host"/>
```

## Retos al Ampliar

### Thread Safety
- WinForms y XNA tienen threading diferente
- Operations gráficas deben estar en thread correcto
- **Recomendación**: Usar `Control.Invoke()` para updates

### Device Lost
- GraphicsDevice puede perderse (alt-tab, resize, etc.)
- Resources deben recrearse
- **Recomendación**: Suscribirse a `ErrorOccurred` y manejar DeviceLost

### Memory
- Textures y resources no se liberan automáticamente
- Implementar `IDisposable` correctamente
- **Recomendación**: Usar `using` statements o dispos pattern

### WPF Airspace
- WPF y WinForms comparten Airspace issues
- Renderizado puede tener problemas de z-order
- **Recomendación**: Minimizar overlapped controls

### Resize Handling
- Resize de ventana causa recreation de device
- Estado debe preservarse
- **Recomendación**: Guardar estado antes de resize

## Uso Típico

```csharp
// En formulario WinForms
public partial class PreviewForm : Form
{
    private GraphicsDeviceControl _graphicsControl;
    private SpriteBatch _spriteBatch;
    
    public PreviewForm()
    {
        InitializeComponent();
        
        _graphicsControl = new GraphicsDeviceControl
        {
            Dock = DockStyle.Fill
        };
        
        _graphicsControl.XnaDraw += OnXnaDraw;
        _graphicsControl.XnaUpdate += OnXnaUpdate;
        
        Controls.Add(_graphicsControl);
    }
    
    private void OnXnaDraw(object sender, EventArgs e)
    {
        var device = _graphicsControl.GraphicsDevice;
        device.Clear(Color.Transparent);
        
        _spriteBatch.Begin();
        // Draw Gum elements here
        _spriteBatch.End();
    }
    
    private void OnXnaUpdate(object sender, EventArgs e)
    {
        // Update logic
    }
}
```

## Comparación con Alternativas

| Aspecto | XnaAndWinforms | SkiaSharp.WPF | D3DImage |
|---------|----------------|---------------|----------|
| Framework | WinForms/XNA | WPF/Skia | WPF/DirectX |
| RenderBackend | XNA/MonoGame/KNI | Skia | Direct3D |
| Multiplataforma | Windows only | Cross-platform | Windows only |
| Performance | High | High | Highest |
| Compatibilidad Gum | Nativo | SkiaGum | Limitada |
| Complejidad | Media | Baja | Alta |