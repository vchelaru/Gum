# SkiaSharp (General Canvas)

## Introduction

This page is for projects that render with SkiaSharp but don't fit one of Gum's dedicated Skia hosts (.NET MAUI, WPF, or Silk.NET). It assumes you already have your own `SKCanvas` rendering in your project, gotten however your platform requires. Skia itself is just a 2D graphics API, not a windowing or input system, so beyond "get an `SKCanvas` on screen," Gum can't offer platform-specific setup guidance here, that part is entirely up to your own project.

{% hint style="info" %}
If your project is .NET MAUI, WPF, or Silk.NET, use that page instead: [.NET MAUI](.net-maui.md), [WPF](wpf.md), [Silk.NET](silk.net.md). Each of those wires up a canvas view (or window) for you, and Silk.NET also wires up real mouse/keyboard input. This page is for everything else.
{% endhint %}

{% hint style="warning" %}
This setup is rendering and layout only. There is no supported way to wire mouse, keyboard, or Forms control interactivity (hover, click, focus) here, not even as DIY glue code. If you need interactive Forms controls on SkiaSharp, use the Silk.NET, MAUI, or WPF setup instead, each of which has a real input story.
{% endhint %}

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use the NuGet package. Open your project in your preferred IDE, or add Gum through the command line.

Add the Gum.SkiaSharp NuGet package ([https://www.nuget.org/packages/Gum.SkiaSharp](https://www.nuget.org/packages/Gum.SkiaSharp))

Modify csproj:

```xml
<PackageReference Include="Gum.SkiaSharp" />
```

Or add through command line:

```bash
dotnet add package Gum.SkiaSharp
```

{% hint style="warning" %}
Don't name your project `SkiaGum`, that's the assembly name inside the `Gum.SkiaSharp` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/SkiaGum/SkiaGum.csproj

`SkiaGum.csproj` already references `GumCommon` itself, so you do not need to add `GumCommon` separately.

Next, add SkiaGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\SkiaGum\SkiaGum.csproj" />
```

## Initializing Gum

{% hint style="info" %}
`GumService` for this page's setup is available in `Gum.SkiaSharp` starting September 2026, or now if building Gum from source. Before that, WPF and MAUI hosts still get their own copy of the same type through their dedicated packages (see [WPF](wpf.md) / [.NET MAUI](.net-maui.md)); this page's setup did not have one until now.
{% endhint %}

`Gum.SkiaSharp` includes a render-only `GumService`, the same `Initialize`/`Update`/`Draw`/`HandleResize` API shape used by the dedicated Silk.NET host (see [Silk.NET](silk.net.md)), just without input. Call `GumService.Default.Initialize` once you have an `SKCanvas`, passing a `.gumx` project path only if you're loading one (omit it for a code-only setup):

```csharp
// Initialize
using Gum;
using SkiaSharp;

var bounds = canvas.DeviceClipBounds;
GumService.Default.Initialize(canvas, bounds.Width, bounds.Height, "Content/GumProject/GumProject.gumx");
```

Each frame, update and then draw:

```csharp
// Update
GumService.Default.Update(totalSecondsSinceStart);
```

```csharp
// Draw
GumService.Default.Draw();
```

Whenever your canvas is resized, tell Gum so layout re-runs against the new size:

```csharp
GumService.Default.HandleResize(newWidth, newHeight);
```

{% hint style="info" %}
Gum does not clear the canvas for you, so your own draw code is expected to clear or paint the background before calling `GumService.Default.Draw()`.
{% endhint %}

## Adding Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you can add the `Gum.Expressions` NuGet package for full expression evaluation at runtime. Without this package, simple variable references like `Width = OtherInstance.Width` still work.

Add the NuGet package:

```bash
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` after `GumService.Default.Initialize`. Expression support is typically used with a Gum project that has variable references defined in the tool:

```csharp
// Initialize
GumService.Default.Initialize(canvas, width, height, "Content/GumProject/GumProject.gumx");
GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Circle and Text (Testing the Setup)

Gum can be tested by adding a couple of renderables after Gum is initialized:

```csharp
// Initialize
GumService.Default.Initialize(canvas, width, height);

var circle = new ColoredCircleRuntime();
circle.Color = SKColors.Red;
circle.Width = 200;
circle.Height = 200;
circle.AddToRoot();

var text = new TextRuntime();
text.Text = "SkiaGum on a general canvas!";
text.Dock(Gum.Wireframe.Dock.Top);
text.AddToRoot();
```

After adding these, trigger a redraw through whatever mechanism your host uses to repaint the canvas. You should see a red circle with text docked above it.
