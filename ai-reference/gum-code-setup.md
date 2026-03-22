# Gum Code-Only Setup for MonoGame

This document teaches AI agents how to set up and use Gum UI in code-only MonoGame projects (no WYSIWYG editor, no `.gumx` files). For XML-based projects, see `gum-xml-format.md`.

## NuGet Package

Install `Gum.MonoGame`. This single package pulls in everything needed for code-only UI.

## Required Usings

```csharp
using MonoGameGum;                    // GumService
using Gum.Forms;                      // DefaultVisualsVersion
using Gum.Forms.Controls;             // Button, TextBox, StackPanel, etc.
using Gum.Wireframe;                  // Anchor, Dock enums
```

## Game1 Boilerplate

This is the minimal working Game1 class with Gum UI:

```csharp
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Game1 : Game
{
    private static GumService GumUI => GumService.Default;

    private GraphicsDeviceManager _graphics;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);
        GumUI.UseKeyboardDefaults();

        // Create UI here
        var stackPanel = new StackPanel();
        stackPanel.AddToRoot();
        stackPanel.Anchor(Anchor.Center);

        var button = new Button();
        stackPanel.AddChild(button);
        button.Text = "Click Me";
        button.Click += (s, e) => { /* handle click */ };

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
```

## Forms Controls Are the Default

Use Forms controls for all standard UI. They handle visuals, input, and states automatically. Available controls in `Gum.Forms.Controls`:

- **Layout:** `StackPanel`, `Panel`, `ScrollViewer`
- **Input:** `Button`, `TextBox`, `CheckBox`, `RadioButton`, `Slider`, `ComboBox`
- **Display:** `Label`, `Image`, `ListBox`

Do not build these from primitives -- `new Button()` creates its own complete visual tree.

## Standard Visuals (Advanced Only)

Runtimes like `ColoredRectangleRuntime`, `TextRuntime`, `SpriteRuntime`, `NineSliceRuntime`, and `ContainerRuntime` from `MonoGameGum.GueDeriving` provide exact rendering control. Use these only for custom HUD elements, decorative graphics, or building custom controls. If a Forms control exists for the job, use it instead.

## Adding Elements to the Scene

- **Root-level:** `element.AddToRoot()`
- **As child:** `parent.AddChild(child)`

Both methods work for Forms controls and standard visuals.

## Common Mistakes for AI-Generated Code

1. **Don't call `AddToManagers` directly.** Use `AddToRoot()` instead. `AddToManagers` is a low-level API that `AddToRoot` wraps.

2. **Don't use `Children.Add` or set `element.Parent`.** Use `AddChild(child)`. The low-level APIs exist but skip important setup.

3. **Always pass `DefaultVisualsVersion.V3`.** This is required for new projects. Omitting it or using an older version produces outdated visuals.

4. **Don't manually construct a control's visual tree.** `new Button()` creates its own visuals. Building a button from rectangles and text is wrong.

5. **Layout units require `.Visual` access.** Forms controls expose `X`, `Y`, `Width`, `Height` as convenience properties. For layout units, origins, or other Gum-specific properties, access them through the `.Visual` property (e.g., `button.Visual.WidthUnits = ...`).

## Docs Reference

Full documentation: https://docs.flatredball.com/gum/code/about/gum-in-code
