# Gum.Themes.Editor

An editor-focused theme for [Gum](https://github.com/vchelaru/Gum) UI. Provides styled visuals for building tool and editor interfaces.

The theme ships per rendering backend. Install the one matching your runtime:

```
dotnet add package Gum.Themes.Editor.MonoGame
```

```
dotnet add package Gum.Themes.Editor.Kni
```

## Usage

Call `EditorTheme.Apply` after initializing Gum:

```csharp
using Gum.Themes.Editor;

protected override void Initialize()
{
    GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);

    EditorTheme.Apply(GraphicsDevice);

    var button = new Button();
    button.Text = "Click Me";
}
```

## Documentation

Full documentation — including the included control list, `PropertyGridVisual`, and `Expander` — lives in the Gum docs:

**https://docs.flatredball.com/gum/code/styling/editor-theme**
