# Gum.Themes.Editor

An editor-focused theme for [Gum](https://github.com/vchelaru/Gum) UI. Provides styled visuals for building tool and editor interfaces.

The theme ships per rendering backend. Install the one matching your runtime:

```
dotnet add package Gum.Themes.Editor.MonoGame
```

```
dotnet add package Gum.Themes.Editor.Kni
```

```
dotnet add package Gum.Themes.Editor.Raylib
```

## Usage

Call the parameterless `EditorTheme.Apply()` after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.Editor;

// after GumService.Default.Initialize(...)
EditorTheme.Apply();

var button = new Button();
button.Text = "Click Me";
```

> On MonoGame/KNI a legacy `EditorTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

## Documentation

Full documentation — including the included control list, `PropertyGridVisual`, and `Expander` — lives in the Gum docs:

**https://docs.flatredball.com/gum/code/styling/themes/editor-theme**
