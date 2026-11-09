# BRUTAL

## Introduction

This page assumes you have an existing [BRUTAL](https://github.com/ahwootech/BRUTAL) project that opens a GLFW window and creates a Vulkan device or an OpenGL context. See [BRUTAL](../empty-project-before-adding-gum/brutal.md) if you need to create one.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Adding Gum NuGet package

Add the Gum.Brutal NuGet package ([https://www.nuget.org/packages/Gum.Brutal](https://www.nuget.org/packages/Gum.Brutal))

Modify csproj:

```xml
<PackageReference Include="Gum.Brutal" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Brutal
```

`Gum.Brutal` renders through SkiaSharp and adds real Forms input (mouse, keyboard, gamepad, focus) from a BRUTAL `GlfwWindow`. BRUTAL has no renderer or texture type, so Gum draws into a GPU image that you sample in your own renderer. `Gum.Brutal` does not depend on a graphics API, so you pick the SkiaGameRendering package that matches the one your game uses:

| Graphics API | Package | Gives you |
| --- | --- | --- |
| Vulkan 1.3 | `SkiaGameRendering.Brutal` | `target.VkImage` |
| OpenGL 3.3+ | `SkiaGameRendering.Brutal.OGL` | `target.GlTexture` |

Add only one of them:

{% tabs %}
{% tab title="Vulkan" %}
```bash
dotnet add package SkiaGameRendering.Brutal
```
{% endtab %}

{% tab title="OpenGL" %}
```bash
dotnet add package SkiaGameRendering.Brutal.OGL
```
{% endtab %}
{% endtabs %}

Both packages work on Windows and Linux, and Gum draws the same way with either: you pass the target's canvas to `GumService.Draw(SKCanvas)`.

{% hint style="warning" %}
Do not name your project `BrutalGum`, that is the assembly name inside the `Gum.Brutal` package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder. Gum's build catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/BrutalGum/BrutalGum.csproj

`BrutalGum.csproj` already references `GumCommon` and `SkiaGum` itself, so you do not need to add either separately.

Next, add BrutalGum as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\BrutalGum\BrutalGum.csproj" />
```

## Initializing Gum

Create a context and a `SkiaRenderTarget2D` for your graphics API, then pass the window and the target's size to Gum. Gum does not need the canvas until the first draw:

{% tabs %}
{% tab title="Vulkan" %}
```csharp
using Brutal.GlfwApi;
using Gum;
using SkiaGameRendering;
using SkiaGameRendering.Core.VK;

// Initialize
var context = new SkiaVulkanContext(
    instance.VkHandle, physicalDevice.VkHandle, device.VkHandle, queue.VkHandle, (uint)queueFamily);
var target = new SkiaRenderTarget2D(context, 800, 450);

GumService.Default.Initialize(window, 800, 450);
```
{% endtab %}

{% tab title="OpenGL" %}
```csharp
using Brutal.GlfwApi;
using Gum;
using SkiaGameRendering;

// Initialize, while your OpenGL context is current
var context = new SkiaGlContext(
    name => Glfw.GetProcAddress(System.Text.Encoding.ASCII.GetBytes(name + "\0")));
var target = new SkiaRenderTarget2D(context, 800, 450);

GumService.Default.Initialize(window, 800, 450);
```
{% endtab %}
{% endtabs %}

`window` is the `GlfwWindow` you created with `Glfw.CreateWindow`. Create the target at the size of the window so mouse positions line up with Gum's coordinates.

To load a Gum project (a `.gumj` file) at the same time, pass its path:

```csharp
// Initialize
GumService.Default.Initialize(window, 800, 450, "Content/GumProject/GumProject.gumj");
```

You can add controls as soon as `Initialize` returns.

## Updating and Drawing Gum

Call `Update` once per frame after `Glfw.PollEvents`, then draw Gum between the target's `Begin` and `End`:

```csharp
// Update and Draw
Glfw.PollEvents();

GumService.Default.Update(clock.Elapsed.TotalSeconds);

target.Begin();
target.Canvas.Clear(SKColors.Transparent);
GumService.Default.Draw(target.Canvas);
target.End();

// target now holds the frame. Sample it in your own renderer.
```

The canvas is readable only between `Begin` and `End`, so pass it to `Draw` every frame.

### Sampling the Image

Sample the frame in your own renderer, for example on a full-screen triangle.

{% tabs %}
{% tab title="Vulkan" %}
Create your own `VkImageView` over `target.VkImage` (format `target.VkFormat`) and sample it. Things to know:

* The image is in `SHADER_READ_ONLY_OPTIMAL` whenever you own it: after construction and after `End`. `Begin` and `End` record the transitions on the queue you gave `SkiaVulkanContext`.
* If several threads submit to that queue, take your own lock around your submits and pass it to `SkiaVulkanContext` as the `acquireQueueLock` option. Single-threaded rendering needs nothing.
* The image is plain UNORM and holds sRGB bytes. If your pass writes to an sRGB swapchain without decoding the sampled value first, colors are encoded twice and look washed out.

For a complete renderer that samples the image, see the `Sample.Brutal.Quad` sample in the SkiaGameRendering repository: [https://github.com/vchelaru/SkiaGameRendering](https://github.com/vchelaru/SkiaGameRendering).
{% endtab %}

{% tab title="OpenGL" %}
Bind `target.GlTexture` and draw with it. Things to know:

* Skia draws on your OpenGL context, so create, `Begin`, `End` and dispose the targets on the thread that owns the context, with the context current. Dispose targets before the `SkiaGlContext`.
* `Begin` and `End` restore only your draw framebuffer binding. Skia leaves the program, vertex array, buffers, viewport, scissor, blend and depth state as it used them, so set yours again after `End`.
* The texture is `GL_RGBA8` with premultiplied alpha, so blend it with `ONE, ONE_MINUS_SRC_ALPHA`.

For a complete example, see the `Sample.Brutal.OGL` sample in the SkiaGameRendering repository: [https://github.com/vchelaru/SkiaGameRendering](https://github.com/vchelaru/SkiaGameRendering).
{% endtab %}
{% endtabs %}

## Adding Shader Support (Optional)

`Gum.SkiaSharp.ShadowDusk` lets a render target container's `SourceShaderFile` point at an `.fx` or `.slang` shader file.

Add the NuGet package:

```bash
dotnet add package Gum.SkiaSharp.ShadowDusk
```

Then call `UseShadowDusk` after `GumService.Default.Initialize`:

```csharp
// Initialize
GumService.Default.Initialize(window, 800, 450, "Content/GumProject/GumProject.gumj");
GumService.Default.UseShadowDusk();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj` to your solution.

For loading, errors, and examples, see [Render Target Shaders on SkiaSharp](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp).

## Adding Expression Support (Optional)

If your Gum project uses arithmetic expressions in variable references (such as `Width = OtherInstance.Width + 20`), you can add the `Gum.Expressions` NuGet package for full expression evaluation at runtime. Without this package, simple variable references like `Width = OtherInstance.Width` still work.

Add the NuGet package:

```bash
dotnet add package Gum.Expressions
```

Then call `GumExpressionService.Initialize()` after `GumService.Default.Initialize`:

```csharp
// Initialize
GumService.Default.Initialize(window, 800, 450, "Content/GumProject/GumProject.gumj");
Gum.Expressions.GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

```csharp
// Initialize
GumService.Default.Initialize(window, 800, 450);

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";
```

`Button` comes from `Gum.Forms.Controls`, `Anchor` from `Gum.Wireframe`.
