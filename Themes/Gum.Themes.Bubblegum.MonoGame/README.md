# Gum.Themes.Bubblegum

A pastel pink "casual game" theme for Gum UI Forms controls. Pairs rounded pill buttons with soft drop shadows and the Nunito typeface for a friendly, approachable look.

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Bubblegum.MonoGame`
- KNI: `dotnet add package Gum.Themes.Bubblegum.Kni`
- raylib: `dotnet add package Gum.Themes.Bubblegum.Raylib`

## Usage

Call the parameterless `BubblegumTheme.Apply()` once after initializing Gum — the same call on every backend:

```csharp
using Gum.Themes.Bubblegum;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    BubblegumTheme.Apply();
    base.Initialize();
}
```

Every default Forms control now renders in the Bubblegum style.

> On MonoGame/KNI a legacy `BubblegumTheme.Apply(GraphicsDevice)` overload remains for source
> compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

## Licensing

- Theme code: MIT (same as Gum).
- Bundled Nunito font: SIL Open Font License (see `OFL.txt` packed at the root of the NuGet).
