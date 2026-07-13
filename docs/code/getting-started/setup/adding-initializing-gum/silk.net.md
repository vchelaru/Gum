# Silk.NET

## Introduction

This page assumes you have an existing Silk.NET project. This can be an empty project or an existing game.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.SilkNet NuGet package ([https://www.nuget.org/packages/Gum.SilkNet](https://www.nuget.org/packages/Gum.SilkNet))

Modify csproj:

```xml
<PackageReference Include="Gum.SilkNet" />
```

Or add through command line:

```bash
dotnet add package Gum.SilkNet
```

`Gum.SilkNet` renders through SkiaSharp and adds real Forms input (mouse, keyboard, focus) via `Silk.NET.Input`. Your project still owns window creation and the render loop; Gum takes an `SKCanvas` and an `IInputContext` you hand it.

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/SilkNetGum/SilkNetGum.csproj

`SilkNetGum.csproj` already references `GumCommon` itself, so you do not need to add `GumCommon` separately.

Next, add SilkNetGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\SilkNetGum\SilkNetGum.csproj" />
```

## Initializing Gum

Gum requires SkiaSharp to render in Silk.NET, and `Silk.NET.Windowing` to create the window and OpenGL/ANGLE context that SkiaSharp draws into. A full Silk.NET project requires a few hundred lines of code (window creation, ANGLE backend selection, the GL debug callback, and so on), so an entire Program file is not included here — the relevant setup and initialization code is shown below.

Create the window and input context using `Silk.NET.Windowing.Window.Create`, then hand Gum the resulting `SKCanvas` and `IInputContext`:

```csharp
// Initialize
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using Silk.NET.Input;
using SkiaSharp;
using Gum;

// Silk.NET.Windowing.Sdl must create and own the window (via window.Initialize()) for
// Silk.NET.Input to ever receive events. Set the render backend, GraphicsAPI, and window
// options, then create and initialize the window before anything else.
SdlWindowing.Use();

Silk.NET.Windowing.IWindow window = Silk.NET.Windowing.Window.Create(options);
window.Initialize();

// Create the SkiaSharp GL surface/canvas from the window's GL context (grContext, grGlInterface,
// and renderTarget setup omitted here -- see the sample for the full ANGLE/backend setup).
SKCanvas canvas = surface.Canvas;

// Only after window.Initialize() has run does CreateInput() build an IInputContext that
// actually receives events.
IInputContext inputContext = window.CreateInput();

GumService.Default.Initialize(canvas, inputContext, "Content/GumProject/GumProject.gumx");
```

Each frame, pump window events before updating and drawing Gum:

```csharp
// Update
window.DoEvents();
GumService.Default.Update(totalSeconds);
```

{% hint style="warning" %}
The `IInputContext` you pass to `Initialize` must come from a window that `Silk.NET.Windowing` created and initialized itself — `Window.Create(options)` followed by `window.Initialize()`, then `window.CreateInput()`. Building an `IInputContext` by wrapping a window you created another way (for example via `SdlWindowing.CreateFrom(existingHandle)`) skips the normal event-subscription path. The resulting `IInputContext` looks valid, but it silently never receives events — no exception is thrown, and clicks, key presses, and typed text simply do nothing.
{% endhint %}

For a working project, see the Gum Silk.NET sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/SilkNetGum" %}

<figure><img src="../../../../.gitbook/assets/22_12 20 45.png" alt=""><figcaption><p>Gum running in a Silk.NET project</p></figcaption></figure>
