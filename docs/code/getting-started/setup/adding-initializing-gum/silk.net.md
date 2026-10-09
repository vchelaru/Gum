# Silk.NET

## Introduction

This page assumes you have an existing Silk.NET project. This can be an empty project or an existing game.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use NuGet. Open your project in your preferred IDE, or add packages through the command line.

The block below includes the base package, the two Silk.NET packages Gum needs to open a window and receive input, and two add-ons marked **optional**: shader file support and arithmetic expression support. If you don't need an add-on, skip its line here and delete the matching line from the initialization code in [Initializing Gum](#initializing-gum) below.

Add the Gum.SilkNet NuGet package ([https://www.nuget.org/packages/Gum.SilkNet](https://www.nuget.org/packages/Gum.SilkNet))

Modify csproj:

```xml
<PackageReference Include="Gum.SilkNet" Version="*" />
<PackageReference Include="Silk.NET.Windowing.Sdl" Version="2.21.0" /> <!-- Creates the window and its OpenGL context -->
<PackageReference Include="Silk.NET.Input.Sdl" Version="2.21.0" /> <!-- Required for window.CreateInput() -->
<PackageReference Include="Gum.SkiaSharp.ShadowDusk" Version="*" /> <!-- Optional: .fx and .slang shader files -->
<PackageReference Include="Gum.Expressions" Version="*" /> <!-- Optional: arithmetic expressions in variable references -->
```

Or add through command line:

```bash
dotnet add package Gum.SilkNet
dotnet add package Silk.NET.Windowing.Sdl --version 2.21.0   # Creates the window and its OpenGL context
dotnet add package Silk.NET.Input.Sdl --version 2.21.0       # Required for window.CreateInput()
dotnet add package Gum.SkiaSharp.ShadowDusk                  # Optional: .fx and .slang shader files
dotnet add package Gum.Expressions                           # Optional: arithmetic expressions in variable references
```

`Gum.SilkNet` renders through SkiaSharp and adds real Forms input (mouse, keyboard, focus) via `Silk.NET.Input`. Your project still owns window creation and the render loop; Gum takes an `SKCanvas` and an `IInputContext` you hand it. Keep the Silk.NET packages on the same version as each other.

{% hint style="info" %}
`Gum.SkiaSharp.ShadowDusk` is available in November 2026, or now if building Gum from source.
{% endhint %}

{% hint style="warning" %}
On Apple Silicon Macs, the SDL native library that `Silk.NET.Windowing.Sdl` 2.21.0 brings in fails macOS's code signature check, and macOS kills the app at launch with no error message. Adding a newer build of the library fixes it:

```bash
dotnet add package Ultz.Native.SDL --version 2.32.10
```

Projects created with `gumcli new -p silknet` already include this package.
{% endhint %}

{% hint style="warning" %}
Don't name your project `SilkNetGum`, that's the assembly name inside the `Gum.SilkNet` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder, causing a `TypeLoadException` at runtime with no build warning. Gum's build now catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

As with the NuGet packages above, the shader and expression projects are marked **optional**; skip a project if you don't need it, and delete its matching line from the initialization code in [Initializing Gum](#initializing-gum).

Add the following projects to your solution:

* \<Gum Root>/Runtimes/SilkNetGum/SilkNetGum.csproj
* \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj, **Optional:** .fx and .slang shader files
* \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj, **Optional:** arithmetic expressions in variable references

`SilkNetGum.csproj` already references `GumCommon` itself, so you do not need to add `GumCommon` separately.

Next, add project references in your game project for the pieces you use. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\SilkNetGum\SilkNetGum.csproj" />
<ProjectReference Include="..\Gum\Runtimes\SkiaGum.ShadowDusk\SkiaGum.ShadowDusk.csproj" />
<ProjectReference Include="..\Gum\Runtimes\GumExpressions\GumExpressions.csproj" />
```

To create a project that already references Gum source, run [`gumcli new MyGame -p silknet --source-linked`](../../../../cli/new.md#linking-to-gum-source).

## Initializing Gum

Gum requires SkiaSharp to render in Silk.NET, and `Silk.NET.Windowing` to create the window and OpenGL context that SkiaSharp draws into. A complete `Program.cs` is long (window creation, the SkiaSharp surface, resizing, the render loop), so only the Gum-specific code is shown below. To get a complete, working `Program.cs`, run [`gumcli new MyGame -p silknet`](../../../../cli/new.md) and read the file it creates. It runs on Windows, macOS, and Linux.

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
// and renderTarget setup omitted here -- see the Program.cs from gumcli new -p silknet).
SKCanvas canvas = surface.Canvas;

// Only after window.Initialize() has run does CreateInput() build an IInputContext that
// actually receives events.
IInputContext inputContext = window.CreateInput();

GumService.Default.Initialize(canvas, inputContext, "Content/GumProject/GumProject.gumj");
GumService.Default.UseShadowDusk(); // Optional: .fx and .slang shader files
GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references
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

{% hint style="warning" %}
If `window.CreateInput()` throws `NotSupportedException: Couldn't find a suitable input platform for this view`, your project is missing the `Silk.NET.Input.Sdl` package listed in [Adding Gum NuGet package](#adding-gum-nuget-package). `Silk.NET.Windowing.Sdl` only creates the window; `CreateInput()` finds its backend by scanning loaded assemblies for a matching `IInputPlatform`, and the SDL implementation lives in the separate `Silk.NET.Input.Sdl` package. Match its version to your other Silk.NET packages.

Once the packages are right, call `CreateInput()` immediately after `window.Initialize()` as shown above. Don't defer it to the window's `Load` event, that event only fires from inside `window.Run()`'s internal loop, and Gum's setup drives its own loop manually via `window.DoEvents()` (see below) rather than calling `window.Run()`.
{% endhint %}

## Adding Shader Support (Optional)

`Gum.SkiaSharp.ShadowDusk` lets a render target container's `SourceShaderFile` point at an `.fx` or `.slang` shader file, so one shader file can be shared with MonoGame. Without this package, `SourceShaderFile` loads `.sksl` files only.

Add the NuGet package:

```bash
dotnet add package Gum.SkiaSharp.ShadowDusk
```

Then call `UseShadowDusk` after `GumService.Default.Initialize`:

```csharp
// Initialize
GumService.Default.Initialize(canvas, inputContext, "Content/GumProject/GumProject.gumj");
GumService.Default.UseShadowDusk();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj` to your solution.

For more information, see [Render Target Shaders on SkiaSharp](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp).

## Adding Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you can add the `Gum.Expressions` NuGet package for full expression evaluation at runtime. Without this package, simple variable references like `Width = OtherInstance.Width` still work.

Add the NuGet package:

```bash
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` after `GumService.Default.Initialize`. Expression support is typically used with a Gum project that has variable references defined in the tool:

```csharp
// Initialize
GumService.Default.Initialize(canvas, inputContext, "Content/GumProject/GumProject.gumj");
GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

```csharp
// Initialize
GumService.Default.Initialize(canvas, inputContext);

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";
```

{% hint style="info" %}
If your file also has `using Silk.NET.Input;`, the compiler reports `Button` as ambiguous because Silk.NET defines its own `Button`. Add `using Button = Gum.Forms.Controls.Button;` to pick the Gum control.
{% endhint %}

For a working project, see the Gum Silk.NET sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/SilkNetGum" %}

<figure><img src="../../../../.gitbook/assets/22_12 20 45.png" alt=""><figcaption><p>Gum running in a Silk.NET project</p></figcaption></figure>
