# Gum.Themes.Bubblegum

A pastel pink "casual game" theme for Gum UI Forms controls. Pairs rounded pill buttons with soft drop shadows and the Nunito typeface for a friendly, approachable look.

## Install

Pick the package matching your runtime:

- MonoGame: `dotnet add package Gum.Themes.Bubblegum.MonoGame`
- KNI: `dotnet add package Gum.Themes.Bubblegum.Kni`

## Usage

Call `BubblegumTheme.Apply(GraphicsDevice)` once after initializing Gum:

```csharp
using Gum.Themes.Bubblegum;

protected override void Initialize()
{
    GumService.Default.Initialize(this);
    BubblegumTheme.Apply(GraphicsDevice);
    base.Initialize();
}
```

Every default Forms control now renders in the Bubblegum style.

## Licensing

- Theme code: MIT (same as Gum).
- Bundled Nunito font: SIL Open Font License (see `OFL.txt` packed at the root of the NuGet).
