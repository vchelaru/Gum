# InputLibrary (Manejo de Input)

## Descripción

InputLibrary es una librería que proporciona manejo unificado de keyboard y mouse para aplicaciones Windows, integrando input de XNA/MonoGame/KNI con manejo de focus de WinForms. Garantie que el input solo se procesa cuando el control tiene foco.

## Diagrama de Relaciones

```mermaid
graph TB
    InputLibrary[InputLibrary - Input Handling]
    
    subgraph Consumers
        Gum[Gum Tool]
        TextureCoordinateSelectionPlugin[TextureCoordinateSelectionPlugin]
        SkiaPlugin[SkiaPlugin]
        StateAnimationPlugin[StateAnimationPlugin]
        GumFormsPlugin[GumFormsPlugin]
        CodeOutputPlugin[CodeOutputPlugin]
        ImportFromGumxPlugin[ImportFromGumxPlugin]
        FlatRedBall.SpecializedXnaControls[FlatRedBall.SpecializedXnaControls]
    end
    
    Gum --> InputLibrary
    TextureCoordinateSelectionPlugin --> InputLibrary
    SkiaPlugin --> InputLibrary
    StateAnimationPlugin --> InputLibrary
    GumFormsPlugin --> InputLibrary
    CodeOutputPlugin --> InputLibrary
    ImportFromGumxPlugin --> InputLibrary
    FlatRedBall.SpecializedXnaControls --> InputLibrary
    
    subgraph CoreClasses
        Keyboard[Keyboard - Singleton]
        Cursor[Cursor - Singleton]
    end
    
    InputLibrary --> CoreClasses
    
    subgraph Dependencies
        KNI_Input[nkast.Xna.Framework.Input]
    end
    
    InputLibrary --> KNI_Input
    
    subgraph Features
        KeyPushed[KeyPushed - Single Press]
        KeyDown[KeyDown - Held Down]
        FocusAware[Focus-Aware Processing]
        MousePosition[Mouse Position (X, Y)]
        DragDetection[Drag Detection]
        DoubleClick[Double Click Detection]
    end
    
    CoreClasses --> Features
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | Windows Forms + XNA Input |
| **.NET** | net8.0-windows |
| **Lenguaje** | C# 12.0 |
| **Dependencias** | nkast.Xna.Framework.Input (v4.1.9001) |

## Clases Principales

### Keyboard (Singleton)

| Propiedad/Método | Propósito |
|------------------|-----------|
| `KeyPushed(Keys)` | True si tecla fue presionada este frame |
| `KeyDown(Keys)` | True si tecla está siendo presionada |
| `PushedKeys` | Lista de teclas presionadas este frame |
| `HeldKeys` | Lista de teclas mantenidas |
| `Control` | True si Ctrl está presionado |
| `Shift` | True si Shift está presionado |
| `Alt` | True si Alt está presionado |

### Cursor (Singleton)

| Propiedad/Método | Propósito |
|------------------|-----------|
| `X` | Posición X del mouse |
| `Y` | Posición Y del mouse |
| `XChange` | Cambio en X desde último frame |
| `YChange` | Cambio en Y desde último frame |
| `PrimaryPush` | Botón izquierdo presionado (click simple) |
| `PrimaryDown` | Botón izquierdo mantenido |
| `PrimaryClick` | Botón izquierdo soltado (click completo) |
| `SecondaryPush` | Botón derecho presionado |
| `SecondaryDown` | Botón derecho mantenido |
| `SecondaryClick` | Botón derecho soltado |
| `MiddlePush` | Botón medio presionado |
| `MiddleDown` | Botón medio mantenido |
| `MiddleClick` | Botón medio soltado |
| `IsInWindow` | True si mouse está sobre el control |
| `SetWinformsCursor()` | Configura cursor de WinForms |

## Cómo Ampliar

### Uso Básico - Keyboard

```csharp
using InputLibrary;

public class MyGameLoop
{
    public void Update()
    {
        // Detectar tecla presionada (solo una vez)
        if (Keyboard.KeyPushed(Keys.Space))
        {
            Console.WriteLine("Space was pressed!");
        }
        
        // Detectar tecla mantenida
        if (Keyboard.KeyDown(Keys.W))
        {
            MoveForward();
        }
        
        // Combinaciones
        if (Keyboard.Control && Keyboard.KeyPushed(Keys.S))
        {
            Save();
        }
        
        // Iterar teclas presionadas
        foreach (var key in Keyboard.PushedKeys)
        {
            Console.WriteLine($"Key pressed: {key}");
        }
    }
}
```

### Uso Básico - Cursor

```csharp
using InputLibrary;

public class MyGameLoop
{
    public void Update()
    {
        // Posición del mouse
        int mouseX = Cursor.X;
        int mouseY = Cursor.Y;
        
        // Detectar click simple
        if (Cursor.PrimaryPush)
        {
            Console.WriteLine($"Click at ({mouseX}, {mouseY})");
        }
        
        // Detectar drag
        if (Cursor.PrimaryDown)
        {
            int deltaX = Cursor.XChange;
            int deltaY = Cursor.YChange;
            ProcessDrag(deltaX, deltaY);
        }
        
        // Detectar click completo
        if (Cursor.PrimaryClick)
        {
            Console.WriteLine("Click completed");
        }
        
        // Click derecho
        if (Cursor.SecondaryClick)
        {
            ShowContextMenu(mouseX, mouseY);
        }
        
        // Hover detection
        if (Cursor.IsInWindow)
        {
            HighlightElementAt(mouseX, mouseY);
        }
    }
}
```

### Integración con Control WinForms

```csharp
public class MyXnaControl : GraphicsDeviceControl
{
    protected override void OnMouseDown(MouseEventArgs e)
    {
        Cursor.HandleMouseDown(e);
        base.OnMouseDown(e);
    }
    
    protected override void OnMouseUp(MouseEventArgs e)
    {
        Cursor.HandleMouseUp(e);
        base.OnMouseUp(e);
    }
    
    protected override void OnMouseMove(MouseEventArgs e)
    {
        Cursor.HandleMouseMove(e);
        base.OnMouseMove(e);
    }
    
    protected override void OnKeyDown(KeyEventArgs e)
    {
        Keyboard.HandleKeyDown(e);
        base.OnKeyDown(e);
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        Keyboard.HandleKeyUp(e);
        base.OnKeyUp(e);
    }
}
```

### Custom Double Click

```csharp
public classDoubleClickDetector
{
    private DateTime _lastClickTime;
    private int _lastClickX;
    private int _lastClickY;
    private readonly TimeSpan _doubleClickTime = TimeSpan.FromMilliseconds(500);
    
    public bool IsDoubleClick()
    {
        if (Cursor.PrimaryPush)
        {
            var now = DateTime.Now;
            var delta = now - _lastClickTime;
            
            if (delta < _doubleClickTime &&
                Math.Abs(Cursor.X - _lastClickX) < 5 &&
                Math.Abs(Cursor.Y - _lastClickY) < 5)
            {
                _lastClickTime = DateTime.MinValue;
                return true;
            }
            
            _lastClickTime = now;
            _lastClickX = Cursor.X;
            _lastClickY = Cursor.Y;
        }
        
        return false;
    }
}
```

## Retos al Ampliar

### Focus Management
- Solo debe procesar input cuando el control tiene foco
- Perder foco debe limpiar estados de teclas
- **Recomendación**: Siempre verificar `Focused` antes de procesar

### Multiple Controls
- Múltiples controles pueden querer input
- Singleton puede causar conflictos
- **Recomendación**: Usar instancias separadas o routing

### Key Rollover
- KeyPushed vs KeyDown puede causar bugs
- Multiple keys presionadas tienen edge cases
- **Recomendación**: Usar buffers para combos complejos

### Gamepad Support
- InputLibrary solo maneja Keyboard/Mouse
- No hay soporte para Gamepad
- **Recomendación**: Extender con GamepadState class

## Uso Típico en Gum

```csharp
// En el editor Gum

// Detectar hotkeys
if (Keyboard.KeyDown(Keys.LeftControl))
{
    if (Keyboard.KeyPushed(Keys.C))
    {
        CopySelection();
    }
    else if (Keyboard.KeyPushed(Keys.V))
    {
        PasteSelection();
    }
    else if (Keyboard.KeyPushed(Keys.Z))
    {
        Undo();
    }
    else if (Keyboard.KeyPushed(Keys.Y))
    {
        Redo();
    }
}

// Seleccionar elemento con mouse
if (Cursor.PrimaryPush)
{
    var element = GetElementAt(Cursor.X, Cursor.Y);
    SelectionManager.Select(element);
}

// Pan con middle mouse
if (Cursor.MiddleDown)
{
    Camera.Pan(-Cursor.XChange, -Cursor.YChange);
}

// Zoom con scroll wheel (requires custom handling)
// InputLibrary doesn't handle scroll, use WinForms event
```

## Comparación de Métodos

| Método | Cuándo Usar |
|--------|--------------|
| `KeyPushed` | Acciones únicas (disparar, abrir menú) |
| `KeyDown` | Movimiento continuo (mover personaje) |
| `PrimaryPush` | Click simple (seleccionar) |
| `PrimaryDown` | Drag, continuous hold |
| `PrimaryClick` | Click completo (fin de drag) |