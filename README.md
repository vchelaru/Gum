<div align="center">
  <img align="center" src="https://github.com/user-attachments/assets/90d1625b-1d9a-4fca-b9a7-f0fb42badcbb" alt="gum-logo-normal-512"/>
</div>
<div align="center">A general purpose UI layout tool built on object-oriented principles.</div>
<br/>
<div align="center">
  <a href="https://discord.gg/tG5RBgw"><img src="https://img.shields.io/discord/586997072373481494" alt="Join the chat" /></a>
  <a href="https://twitter.com/FlatRedBall"><img src="https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Ftwitter.com%2FFlatRedBall" alt="Twitter"/></a>
  <img src="https://img.shields.io/github/last-commit/vchelaru/Gum/main" alt="Activity" />
</div>
<br/>

![image](https://github.com/vchelaru/Gum/assets/729631/9f1d16b2-47a0-47d4-a6bc-0a3d6f100699)

Specifically it supports:

* Inheritance
* Object instances
* Default/overriding variables
* States (categorized and uncategorized)
* Incredibly flexible layout engine

Gum exports to human-readable XML, and can be extended either using MEF or by modifying the source code directly.

## Beautiful UI, styled in one line

Gum Forms controls look clean by default, and a single line of code restyles **every** control with a built-in theme — same UI, completely different look. Pick one of the shipped themes or author your own.

```csharp
DarkProTheme.Apply(GraphicsDevice); // one line — every control restyled
```

<table>
  <tr>
    <td align="center"><img src="docs/.gitbook/assets/DarkProThemeScreenshot.png" alt="DarkPro theme" width="260"><br/>DarkPro</td>
    <td align="center"><img src="docs/.gitbook/assets/BubblegumThemeScreenshot.png" alt="Bubblegum theme" width="260"><br/>Bubblegum</td>
    <td align="center"><img src="docs/.gitbook/assets/NeonThemeScreenshot.png" alt="Neon theme" width="260"><br/>Neon</td>
  </tr>
  <tr>
    <td align="center"><img src="docs/.gitbook/assets/Retro95ThemeScreenshot.png" alt="Retro 95 theme" width="260"><br/>Retro 95</td>
    <td align="center"><img src="docs/.gitbook/assets/ForestGladeThemeScreenshot.png" alt="Forest Glade theme" width="260"><br/>Forest Glade</td>
    <td align="center"><img src="docs/.gitbook/assets/EditorThemeScreenshot.png" alt="Editor theme" width="260"><br/>Editor</td>
  </tr>
</table>

See the [full theme gallery and usage →](https://docs.flatredball.com/gum/code/styling/themes)

Tutorials and documentation can be found here:

https://docs.flatredball.com/gum/

![Alt](https://repobeats.axiom.co/api/embed/43574f096866fcf9b5addde4589447d1b532ade4.svg "Repobeats analytics image")

### Star History

[![Star History Chart](https://api.star-history.com/svg?repos=vchelaru/gum&type=Date)](https://www.star-history.com/#vchelaru/gum&Date)

### Contributors

<a href="https://github.com/vchelaru/gum/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=vchelaru/gum" />
</a>

## Need Help?

The fastest way to get help is to ask in our Discord: https://discord.gg/uQSam6w36d

You can also post an issue: https://github.com/vchelaru/Gum/issues

You can also check the docs: https://docs.flatredball.com/gum/

## Installation

Gum's runtimes are published to NuGet. Install the package for your platform. All packages share the same version number, so they stay in sync.

| Platform | Runtime | Shapes |
|---|---|---|
| MonoGame | [![NuGet](https://img.shields.io/nuget/v/Gum.MonoGame?label=Gum.MonoGame)](https://www.nuget.org/packages/Gum.MonoGame) | [![NuGet](https://img.shields.io/nuget/v/Gum.Shapes.MonoGame?label=Gum.Shapes.MonoGame)](https://www.nuget.org/packages/Gum.Shapes.MonoGame) |
| KNI | [![NuGet](https://img.shields.io/nuget/v/Gum.KNI?label=Gum.KNI)](https://www.nuget.org/packages/Gum.KNI) | [![NuGet](https://img.shields.io/nuget/v/Gum.Shapes.KNI?label=Gum.Shapes.KNI)](https://www.nuget.org/packages/Gum.Shapes.KNI) |
| raylib | [![NuGet](https://img.shields.io/nuget/v/Gum.raylib?label=Gum.raylib)](https://www.nuget.org/packages/Gum.raylib) | Built-in |
| SkiaSharp | [![NuGet](https://img.shields.io/nuget/v/Gum.SkiaSharp?label=Gum.SkiaSharp)](https://www.nuget.org/packages/Gum.SkiaSharp) | Built-in |
| Maui | [![NuGet](https://img.shields.io/nuget/v/Gum.SkiaSharp.Maui?label=Gum.SkiaSharp.Maui)](https://www.nuget.org/packages/Gum.SkiaSharp.Maui) | Built-in |
| Sokol | [![NuGet](https://img.shields.io/nuget/v/Gum.sokol?label=Gum.sokol)](https://www.nuget.org/packages/Gum.sokol) | Built-in |
| FNA | [![NuGet](https://img.shields.io/nuget/v/Gum.FNA?label=Gum.FNA)](https://www.nuget.org/packages/Gum.FNA) | — *(not supported)* |

> Shapes is the recommended way to draw rectangles, circles, and other primitives in Gum. On MonoGame and KNI it ships as a separate add-on package (install it alongside the runtime); on the other platforms it's built into the runtime.

Gum produces general-purpose XML, so it can be used in virtually any C# environment. Beyond the packaged runtimes listed above, integrations also exist for environments such as FlatRedBall, Meadow, Silk.NET, WPF, and Avalonia. For integration details — or for using GumCore to integrate with your own runtime — see the main documentation: https://docs.flatredball.com/gum/
