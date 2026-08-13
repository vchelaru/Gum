# SkiaSharp (General Canvas)

## Introduction

This page is for projects that render with SkiaSharp but don't fit one of Gum's dedicated Skia hosts (.NET MAUI, WPF, or Silk.NET). It assumes you already have your own `SKCanvas` rendering in your project, gotten however your platform requires. Skia itself is just a 2D graphics API, not a windowing or input system, so beyond "get an `SKCanvas` on screen," Gum can't offer platform-specific setup guidance here, that part is entirely up to your own project.

{% hint style="info" %}
If your project is .NET MAUI, WPF, or Silk.NET, use that page instead: [.NET MAUI](.net-maui.md), [WPF](wpf.md), [Silk.NET](silk.net.md). Each of those wires up a canvas view (or window) for you, and Silk.NET also wires up real mouse/keyboard input. This page is for everything else.
{% endhint %}

{% hint style="warning" %}
A raw `SKCanvas` has no built-in input system. Unlike Silk.NET, MAUI, or WPF, nothing here reads your mouse, keyboard, or touch input for you, wiring that up is your project's responsibility. See [Handling Input](#handling-input-your-responsibility) below.
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

## Adding the GumService

`Gum.SkiaSharp` is a rendering and layout library only, it does not include a `GumService`. Every other backend's package (MonoGame, raylib, Silk.NET) bundles its own `GumService`; on Skia that type instead lives as a single shared **source file**, `GumService.cs`, meant to be compiled directly into whichever host needs it (this is also how MAUI and WPF get theirs). There is no separate NuGet package for it yet.

Bring `GumService.cs` into your project one of two ways:

* **Building from source:** file-link it, the same way MAUI and WPF do:

  ```xml
  <Compile Include="..\Gum\Runtimes\SkiaGum.Standalone\GumService.cs" Link="GumService.cs" />
  ```
* **Using the NuGet package only:** copy the file directly into your project instead, since it isn't packaged. You can find it here: [Runtimes/SkiaGum.Standalone/GumService.cs](https://github.com/vchelaru/Gum/blob/main/Runtimes/SkiaGum.Standalone/GumService.cs)

Either way you end up with a `Gum.GumService` type that mirrors the game-host `GumService` used by MonoGame and raylib, so Gum code you write is portable across hosts.

## Initializing Gum

Once you have an `SKCanvas`, initialize Gum with it. `GumService.Default.Initialize` reads the canvas size from `SKCanvas.DeviceClipBounds` by default; pass an explicit width/height instead if that doesn't match what you expect (for example, if the canvas's clip isn't configured yet at the point you call this).

```csharp
// Initialize
using Gum;
using SkiaSharp;

GumService.Default.Initialize(canvas, "Content/GumProject/GumProject.gumx");
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
Gum does not clear the canvas for you (`Renderer.ClearsCanvas` is `false` on this path), so your own draw code is expected to clear or paint the background before calling `Draw()`.
{% endhint %}

## Handling Input (Your Responsibility)

Skia is a rendering technology, not a windowing or input system, so the Skia `GumService` shown above is render-only: it never reads a mouse, keyboard, or touch device, and it never pumps Forms input (hover, push, click, focus) on its own. This means a `Button` or other Forms control will render, but it will not react to anything until you wire input up yourself.

To make the cursor (mouse or touch) interactive:

1. Implement `Gum.Wireframe.ICursor`, translating your platform's pointer events into Gum's coordinate space. The [Mouse and Touch Screen (Cursor)](../../../events-and-interactivity/mouse-and-touch-screen-cursor.md) page's `DisabledCursor` example shows the full shape of the interface to implement.
2. Register it with `Gum.Forms.FormsUtilities.SetCursor(myCursor)`.
3. Each frame, call `Gum.Forms.FormsUtilities.Update(totalSecondsSinceStart, GumService.Default.Root)` yourself, this is the call that actually pumps hover/push/click for Forms controls; `GumService.Default.Update` above does not do it on this host.

{% hint style="warning" %}
There is currently no equivalent public entry point for keyboard input on this host, so keyboard-driven focus and typing (tabbing between controls, typing into a `TextBox`) are not available without modifying Gum source. Mouse/touch interaction via a custom `ICursor` works today; keyboard does not.
{% endhint %}

If you only need to place and animate visuals without reacting to clicks, you can skip this section entirely, layout, rendering, and animation all work with no input wired up at all.

## Adding Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you can add the `Gum.Expressions` NuGet package for full expression evaluation at runtime. Without this package, simple variable references like `Width = OtherInstance.Width` still work.

Add the NuGet package:

```bash
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` after `GumService.Default.Initialize`. Expression support is typically used with a Gum project that has variable references defined in the tool:

```csharp
// Initialize
GumService.Default.Initialize(canvas, "Content/GumProject/GumProject.gumx");
GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Circle and Text (Testing the Setup)

Gum can be tested by adding a couple of renderables after Gum is initialized:

```csharp
// Initialize
GumService.Default.Initialize(canvas);

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
