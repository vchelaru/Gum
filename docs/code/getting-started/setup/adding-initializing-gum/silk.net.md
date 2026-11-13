# Silk.NET

## Introduction

This page assumes you have an existing Silk.NET project. This can be an empty project or an existing game.

## Starting From a New Project

If you plan to use a Gum project, the easiest start is a new game that Gum creates for you. In the Gum tool, select **File** > **New Project**, check **Create C# project**, and choose Silk.NET in **Platform**. Or run [`gumcli new MyGame --platform silknet`](../../../../cli/new.md). The game already references Gum, loads the Gum project and copies it to the output folder, so you can skip the rest of this page. See [Creating a Project](../../../../gum-tool/setup/creating-a-project.md).

The rest of this page adds Gum to a game you already have.

## Adding Gum NuGet package

The easiest way to add Gum to your project is to use NuGet. Open your project in your preferred IDE, or add packages through the command line.

The block below includes the base package and the Silk.NET packages the program in [Initializing Gum](#initializing-gum) needs to open a window, receive input and clear the screen. Extra features come as separate packages, listed in [Optional Extras](#optional-extras).

Add the Gum.SilkNet NuGet package ([https://www.nuget.org/packages/Gum.SilkNet](https://www.nuget.org/packages/Gum.SilkNet))

Modify csproj:

```xml
<PackageReference Include="Gum.SilkNet" Version="*" />
<PackageReference Include="Silk.NET.Windowing.Sdl" Version="2.21.0" /> <!-- Creates the window and its OpenGL context -->
<PackageReference Include="Silk.NET.Input.Sdl" Version="2.21.0" /> <!-- Required for the input Gum creates from the window -->
<PackageReference Include="Silk.NET.OpenGL" Version="2.21.0" /> <!-- For your own gl.Clear each frame -->
```

Or add through command line:

```bash
dotnet add package Gum.SilkNet
dotnet add package Silk.NET.Windowing.Sdl --version 2.21.0   # Creates the window and its OpenGL context
dotnet add package Silk.NET.Input.Sdl --version 2.21.0       # Required for the input Gum creates from the window
dotnet add package Silk.NET.OpenGL --version 2.21.0          # For your own gl.Clear each frame
```

`Gum.SilkNet` renders through SkiaSharp and adds real Forms input (mouse, keyboard, focus) via `Silk.NET.Input`. Your project owns window creation and the render loop; Gum takes the `IWindow` and creates the SkiaSharp surface, the input context, and resize handling itself. Keep the Silk.NET packages on the same version as each other.

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

Add the following project to your solution:

* \<Gum Root>/Runtimes/SilkNetGum/SilkNetGum.csproj

`SilkNetGum.csproj` already references `GumCommon` itself, so you do not need to add `GumCommon` separately.

Next, add a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\SilkNetGum\SilkNetGum.csproj" />
```

To link an extra from [Optional Extras](#optional-extras), add its project instead of its package:

* ShadowDusk: \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj

To create a project that already references Gum source, run [`gumcli new MyGame -p silknet --source-linked`](../../../../cli/new.md#linking-to-gum-source).

## Initializing Gum

Gum takes the window you create, then creates the input context and the SkiaSharp surface over the window's framebuffer, and follows window resizes. The following is a complete `Program.cs` that also shows a button. It runs on Windows, macOS, and Linux. It needs only the packages above and builds its UI in code, so it runs without a Gum project.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

```csharp
using System.Diagnostics;
using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;

// Silk.NET.Input only receives events from a window that Silk.NET.Windowing.Sdl created and initialized itself.
SdlWindowing.Use();

WindowOptions options = WindowOptions.Default;
// Desktop OpenGL 3.3 core is available on Windows, macOS and Linux without extra native libraries.
options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));
options.Size = new Vector2D<int>(800, 450);
options.Title = "My Game";
options.VSync = true;
options.PreferredDepthBufferBits = 24;
options.PreferredStencilBufferBits = 8;

IWindow window = Window.Create(options);
window.Initialize();

GL gl = GL.GetApi(window);

GumService GumUI = GumService.Default;

GumUI.Initialize(window);
GumUI.UseKeyboardDefaults();
GumUI.UseGamepadDefaults();

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";

Stopwatch clock = Stopwatch.StartNew();

while (!window.IsClosing)
{
    window.DoEvents();

    double gameTime = clock.Elapsed.TotalSeconds;
    GumUI.Update(gameTime);

    gl.ClearColor(0.39f, 0.58f, 0.93f, 1f);
    gl.Clear(ClearBufferMask.ColorBufferBit);

    GumUI.Draw();

    window.GLContext!.SwapBuffers();
}

window.Dispose();
```

The button in the center of the window confirms Gum works. Delete it once you start building your game.

## Optional Extras

Each extra is a separate package. Add the package, then add its line right after `GumUI.Initialize`.

| Extra | Package | Line after `Initialize` |
| --- | --- | --- |
| [ShadowDusk](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp): `.fx` and `.slang` shader files (without it, `SourceShaderFile` loads `.sksl` only) | `Gum.SkiaSharp.ShadowDusk` | `GumUI.UseShadowDusk();` |
| [Expressions](../../../styling/runtime-variable-references.md): arithmetic in variable references | `Gum.Expressions` | `GumUI.UseExpressions();` |

{% hint style="info" %}
`Gum.SkiaSharp.ShadowDusk` is available in November 2026, or now if building Gum from source.
{% endhint %}

## Loading Your Gum Project

To load a Gum project, pass its `.gumj` path to `Initialize`:

```csharp
// Initialize
GumUI.Initialize(window, "Content/GumProject/GumProject.gumj");
```

Then copy the Gum project to the output folder by adding this to your `.csproj`:

```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

To create the Gum project and show its screens, see [Loading a Gum Project](../loading-a-gum-project-.gumx.md).

## Troubleshooting

{% hint style="warning" %}
The window must come from `Window.Create(options)` followed by `window.Initialize()`. Wrapping a window created another way (for example via `SdlWindowing.CreateFrom(existingHandle)`) skips the normal event subscription. The input looks valid, but clicks, key presses, and typed text silently do nothing.
{% endhint %}

{% hint style="warning" %}
If `Initialize` throws `NotSupportedException: Couldn't find a suitable input platform for this view`, your project is missing the `Silk.NET.Input.Sdl` package listed in [Adding Gum NuGet package](#adding-gum-nuget-package). `Silk.NET.Windowing.Sdl` only creates the window, and the SDL input backend lives in the separate `Silk.NET.Input.Sdl` package. Match its version to your other Silk.NET packages.

Call `Initialize` right after `window.Initialize()`. Don't defer it to the window's `Load` event, which only fires from inside `window.Run()`. Gum's setup drives its own loop with `window.DoEvents()` instead.
{% endhint %}

{% hint style="info" %}
If your file also has `using Silk.NET.Input;`, the compiler reports `Button` as ambiguous because Silk.NET defines its own `Button`. Add `using Button = Gum.Forms.Controls.Button;` to pick the Gum control.
{% endhint %}

## Sample Project

For a working project, see the Gum Silk.NET sample:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/SilkNetGum" %}

<figure><img src="../../../../.gitbook/assets/22_12 20 45.png" alt=""><figcaption><p>Gum running in a Silk.NET project</p></figcaption></figure>
