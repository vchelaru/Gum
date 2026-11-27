# BRUTAL

## Introduction

This page assumes you have an existing [BRUTAL](https://github.com/ahwootech/BRUTAL) project that opens a GLFW window. See [BRUTAL](../empty-project-before-adding-gum/brutal.md) if you need to create one.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

## Starting From a New Project

If you plan to use a Gum project with OpenGL, the easiest start is a new game that Gum creates for you. In the Gum tool, select **File** > **New Project**, check **Create C# project**, and choose BRUTAL in **Platform**. Or run [`gumcli new MyGame --platform brutal --preview`](../../../../cli/new.md). The game already references Gum, loads the Gum project and copies it to the output folder, so you can skip the rest of this page. See [Creating a Project](../../../../gum-tool/setup/creating-a-project.md).

{% hint style="info" %}
BRUTAL appears in **Platform** only in the Gum tool pre-release from November 9, 2026 or newer. Download it from the [Releases page](https://github.com/vchelaru/Gum/releases). For `gumcli`, install the preview with `dotnet tool install -g GumCli --prerelease`.
{% endhint %}

The rest of this page adds Gum to a game you already have.

## Adding Gum NuGet package

Gum has one package per graphics API. Add only one of them:

| Graphics API | Gum package         |
| ------------ | ------------------- |
| OpenGL 3.3   | `Gum.Brutal.OGL`    |
| Vulkan 1.3   | `Gum.Brutal.Vulkan` |

{% tabs %}
{% tab title="OpenGL" %}
Add the Gum.Brutal.OGL NuGet package ([https://www.nuget.org/packages/Gum.Brutal.OGL](https://www.nuget.org/packages/Gum.Brutal.OGL)). Modify csproj:

```xml
<PackageReference Include="Gum.Brutal.OGL" Version="*-*" />
```

Or add through command line:

```bash
dotnet add package Gum.Brutal.OGL --prerelease
```
{% endtab %}

{% tab title="Vulkan" %}
Add the Gum.Brutal.Vulkan NuGet package ([https://www.nuget.org/packages/Gum.Brutal.Vulkan](https://www.nuget.org/packages/Gum.Brutal.Vulkan)). Modify csproj:

```xml
<PackageReference Include="Gum.Brutal.Vulkan" Version="*-*" />
```

Or add through command line:

```bash
dotnet add package Gum.Brutal.Vulkan --prerelease
```

The Vulkan program in [Full Program](brutal.md#full-program) uses `unsafe` blocks, so also allow them in your csproj:

```xml
<PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```
{% endtab %}
{% endtabs %}

Both packages read mouse, keyboard and gamepad input from your BRUTAL `GlfwWindow` and draw Gum to it, so you never write rendering code for Gum. They work on Windows, Linux and macOS (see [macOS](brutal.md#macos)).

Do not reference both packages in one project. They declare the same `GumService` type.

{% hint style="warning" %}
Do not name your project `BrutalGum` (or `BrutalGum.Vulkan`), that is the assembly name inside the package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder. Gum's build catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## macOS

{% hint style="info" %}
macOS support is available in December 2026, or now if building Gum from source.
{% endhint %}

OpenGL needs one more window hint. macOS creates a core profile context only when GLFW's forward compatibility hint is set, and then provides OpenGL 4.1:

```csharp
// Initialize
if (OperatingSystem.IsMacOS())
{
    Glfw.WindowHint((GlfwWindowHint)0x22006 /* GLFW_OPENGL_FORWARD_COMPAT */, Glfw.Constants.GLFW_TRUE);
}
```

Vulkan runs on MoltenVK. Install the Vulkan loader and MoltenVK:

```bash
brew install molten-vk vulkan-loader
```

Skia draws with Metal on macOS, so the instance must export the Metal device and queue, and the device must enable `VK_EXT_metal_objects`. [Full Program](brutal.md#full-program) does not include this; the [Vulkan sample](../../../../../Samples/BrutalGum/BrutalGumSample/) shows it. To keep the UI's size on a Retina display, see [High-DPI Displays](brutal.md#high-dpi-displays).

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/BrutalGum.OGL/BrutalGum.OGL.csproj for OpenGL
* \<Gum Root>/Runtimes/BrutalGum.Vulkan/BrutalGum.Vulkan.csproj for Vulkan

The project already references `GumCommon` and `SkiaGum` itself, so you do not need to add either separately.

Next, add it as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\BrutalGum.OGL\BrutalGum.OGL.csproj" />
```

To link an extra from [Optional Extras](brutal.md#optional-extras), add its project instead of its package:

* ShadowDusk: \<Gum Root>/Runtimes/SkiaGum.ShadowDusk/SkiaGum.ShadowDusk.csproj
* Expressions: \<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj

## Initializing Gum

Pass the window to `Initialize`. For Vulkan, also pass the Vulkan objects you created, which Gum builds its Skia context and swapchain on.

{% tabs %}
{% tab title="OpenGL" %}
Create the window with an OpenGL 3.3 core context and make it current. Make the window resizable if you want Gum to follow its size, which is the default for GLFW windows.

```csharp
using Brutal.GlfwApi;
using Gum;

// Initialize
Glfw.Init();
Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 3);
Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 3);
Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, 0x32001 /* GLFW_OPENGL_CORE_PROFILE */);
GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "My Game", Size = new Brutal.Numerics.int2(800, 450) });
window.MakeContextCurrent();

GumUI.Initialize(window);
```
{% endtab %}

{% tab title="Vulkan" %}
Create the window with no graphics API, then your Vulkan 1.3 instance, surface, device (with dynamic rendering and the swapchain extension) and a graphics queue family that can present to the surface:

```csharp
using Brutal.GlfwApi;
using Gum;

// Initialize
GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface);
```

Gum never destroys the objects you pass. Destroy them after `GumUI.Uninitialize()`. For a complete setup of the window, instance, surface and device, see the BRUTAL Vulkan sample at `Samples/BrutalGum/BrutalGumSample` in the Gum repository.
{% endtab %}
{% endtabs %}

You can add controls as soon as `Initialize` returns. To load a Gum project too, see [Loading Your Gum Project](brutal.md#loading-your-gum-project).

## Updating and Drawing Gum

Call `Update` once per frame after `Glfw.PollEvents`, then `Draw`:

{% tabs %}
{% tab title="OpenGL" %}
```csharp
// Update and Draw
Glfw.PollEvents();
double gameTime = clock.Elapsed.TotalSeconds;
GumUI.Update(gameTime);
GumUI.Draw();
window.SwapBuffers();
```

`Draw` composites Gum's frame over the window's default framebuffer, so call `SwapBuffers` after it. Skia leaves the OpenGL program, vertex array, buffers, viewport, scissor, blend and depth state as it used them, so set your own again if you draw after Gum.
{% endtab %}

{% tab title="Vulkan" %}
```csharp
// Update and Draw
Glfw.PollEvents();
double gameTime = clock.Elapsed.TotalSeconds;
GumUI.Update(gameTime);
GumUI.Draw();
```

`Draw` presents the frame to the window itself.
{% endtab %}
{% endtabs %}

Gum's canvas is the window's framebuffer. When the window is resized, `Draw` follows the new size and lays out your controls for it. Mouse positions are scaled from window coordinates to framebuffer pixels, so high-DPI displays line up. Gum draws over whatever your game already drew to the framebuffer (OpenGL), so clear or draw your background before calling `Draw`.

### High-DPI Displays

Because the canvas is the framebuffer, Gum's UI looks smaller on a Retina Mac or on Windows at 150%. To keep it the same size, pass the window's `ContentScale` to [Expand To Window](../../../layout/resizing-the-game-window.md#high-dpi-displays), and again when the window moves to a display with another scale:

```csharp
// Class scope
float displayScale;

// Update
if (window.ContentScale.X != displayScale)
{
    displayScale = window.ContentScale.X;
    GumUI.EnableExpandToWindow(defaultZoom: displayScale);
}
GumUI.Update(gameTime);
```

If you [supply your own input](brutal.md#supplying-input-from-your-own-window), read the scale from your window instead, such as SDL's `SDL_GetWindowDisplayScale`.

### Using Your Own Renderer (Vulkan)

If your game already has its own Vulkan renderer, turn presenting off and sample Gum's image yourself. Do this before or after `Initialize`, before the first `Draw`. The surface passed to `Initialize` is then not used and may be `default`:

```csharp
GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, default);
GumUI.PresentToWindow = false;

// After each Draw:
VkImage image = GumUI.RenderTargetImage!.Value;
VkFormat format = GumUI.RenderTargetFormat!.Value;
```

Things to know:

* After `Draw` returns, the image is in `SHADER_READ_ONLY_OPTIMAL` on the queue you passed to `Initialize`. Sample it from a later submission on that queue. From another queue, add your own synchronization.
* Read `RenderTargetImage` again after every `Draw`. The image is replaced when the window size changes, and the old one is freed after the GPU finishes the last frame.
* The image is plain UNORM and holds sRGB bytes. If your pass writes to an sRGB target without decoding the sampled value first, colors are encoded twice and look washed out.

OpenGL has no equivalent switch. `Draw` writes the whole default framebuffer, so anything of yours must be drawn after it.

### Sharing the Queue With Other Threads (Vulkan)

Vulkan requires a queue to be used by one thread at a time. If another thread of your game also submits to the queue you passed to `Initialize` (a texture upload worker, for example), pass the lock that thread uses as `acquireQueueLock`. It is a function that takes the lock and returns an `IDisposable` that releases it:

```csharp
// Class scope
readonly object _queueLock = new object();

sealed class QueueLockRelease(object queueLock) : System.IDisposable
{
    public void Dispose() => System.Threading.Monitor.Exit(queueLock);
}

// Initialize
var release = new QueueLockRelease(_queueLock);
GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface,
    acquireQueueLock: () =>
    {
        System.Threading.Monitor.Enter(_queueLock);
        return release;
    });
```

Your other threads take the same `_queueLock` around their own submits. Gum holds it while it draws a frame, around presenting, while it waits for the GPU after a window resize, and while it frees images (on a resize and in `Uninitialize`). Gum never takes it while it already holds it, so the lock does not need to be re-entrant. Do not hold the lock yourself while calling `Draw`, `Initialize` or `Uninitialize`, since Gum takes it inside them. Returning one cached `IDisposable`, as above, keeps `Draw` free of allocations. Leave `acquireQueueLock` out if only the thread that calls Gum uses the queue.

## Full Program

The following is a complete `Program.cs` that opens a window, initializes Gum, shows a button and runs the game loop. It needs only the Gum package and builds its UI in code, so it runs without a Gum project.

{% tabs %}
{% tab title="OpenGL" %}
```csharp
using Brutal.GlfwApi;
using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using System.Diagnostics;

Glfw.Init();
Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 3);
Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 3);
Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, 0x32001 /* GLFW_OPENGL_CORE_PROFILE */);
using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "My Game", Size = new Brutal.Numerics.int2(800, 450) });
window.MakeContextCurrent();
Glfw.SwapInterval(1);

GumUI.Initialize(window);
GumUI.UseKeyboardDefaults();
GumUI.UseGamepadDefaults();

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";

Stopwatch clock = Stopwatch.StartNew();
while (!window.ShouldClose)
{
    Glfw.PollEvents();
    double gameTime = clock.Elapsed.TotalSeconds;
    GumUI.Update(gameTime);
    GumUI.Draw();
    window.SwapBuffers();
}

Glfw.Terminate();

// A property rather than a top-level local, which would hide any GumUI that the project's other files declare (CS8801).
partial class Program
{
    static GumService GumUI => GumService.Default;
}
```
{% endtab %}

{% tab title="Vulkan" %}
```csharp
using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using System.Diagnostics;

Glfw.Init();
// Vulkan draws to the window itself, so GLFW creates no graphics API context.
Glfw.WindowHint(GlfwWindowHint.ClientApi, Glfw.Constants.GLFW_NO_API);
using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "My Game", Size = new int2(800, 450) });

HashSet<string> instanceExtensions = new HashSet<string>();
VulkanHelpers.AddPlatformRequiredExtensions(instanceExtensions);
unsafe
{
    var glfwExtensions = Glfw.GetRequiredInstanceExtensions(out var count);
    for (int i = 0; i < count; i++)
    {
        instanceExtensions.Add(Brutal.Strings.Utf8.GetString(glfwExtensions[i]));
    }
}

using Instance instance = Vulkan.CreateInstance(new Instance.CreateInfo
{
    AppInfo = new VkApplicationInfo { ApiVersion = (int)VulkanHelpers.Api.VERSION_1_3 },
    Flags = VulkanHelpers.GetPlatformRequiredInstanceCreateFlags(),
    EnabledExtensions = instanceExtensions,
    EnabledLayers = [],
});

if (Glfw.CreateWindowSurface(instance.VkHandle, window, out var surfaceHandle) != (int)VkResult.Success)
{
    throw new InvalidOperationException("Failed to create a Vulkan surface for the window.");
}
VkSurfaceKHR surface = new VkSurfaceKHR((nint)surfaceHandle);

// One queue draws and presents, so find a device with a queue family that can do both.
PhysicalDevice? physicalDevice = null;
int queueFamily = -1;
foreach (PhysicalDevice candidate in instance.PhysicalDevices)
{
    var families = new VkQueueFamilyProperties[candidate.GetQueueFamilyProperties_Count()];
    candidate.GetQueueFamilyProperties_Write(families);
    for (int i = 0; i < families.Length && physicalDevice is null; i++)
    {
        if ((families[i].QueueFlags & VkQueueFlags.GraphicsBit) != 0 && candidate.GetSurfaceSupportKHR(i, surface))
        {
            (physicalDevice, queueFamily) = (candidate, i);
        }
    }
}
if (physicalDevice is null)
{
    throw new InvalidOperationException("No Vulkan device has a graphics queue family that can present to the window.");
}

HashSet<string> deviceExtensions = new HashSet<string>();
VulkanHelpers.AddSwapchainExtensions(deviceExtensions);
VulkanHelpers.AddMandatoryIfSupportedDeviceExtensions(deviceExtensions, physicalDevice);
float priority = 1.0f;
VkPhysicalDeviceVulkan13Features features13 = new VkPhysicalDeviceVulkan13Features { DynamicRendering = true };
Device device;
unsafe
{
    device = physicalDevice.CreateDevice(new DeviceEx.CreateInfo
    {
        Extensions = deviceExtensions,
        QueueCreateInfos = [new VkDeviceQueueCreateInfo { QueueFamilyIndex = queueFamily, QueueCount = 1, QueuePriorities = &priority }],
        Next = &features13,
    });
}
using Device deviceScope = device;
Queue queue = device.GetQueue(queueFamily, 0);

GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface);
GumUI.UseKeyboardDefaults();
GumUI.UseGamepadDefaults();

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";

Stopwatch clock = Stopwatch.StartNew();
while (!window.ShouldClose)
{
    Glfw.PollEvents();
    double gameTime = clock.Elapsed.TotalSeconds;
    GumUI.Update(gameTime);
    GumUI.Draw();
}

device.WaitIdle();
GumUI.Uninitialize();
instance.DestroySurfaceKHR(surface, null);
Glfw.Terminate();

// A property rather than a top-level local, which would hide any GumUI that the project's other files declare (CS8801).
partial class Program
{
    static GumService GumUI => GumService.Default;
}
```
{% endtab %}
{% endtabs %}

The button in the center of the window confirms Gum works. Delete it once you start building your game.

## Optional Extras

Each extra is a separate package. Add the package, then add its line right after `GumUI.Initialize`.

| Extra                                                                                                                           | Package                                                                            | Line after `Initialize`                    |
| ------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------ |
| [ShadowDusk](../../../standard-visuals/containerruntime.md#render-target-shaders-on-skiasharp): `.fx` and `.slang` shader files | `Gum.SkiaSharp.ShadowDusk`                                                         | `GumUI.UseShadowDusk();`                   |
| [Expressions](../../../styling/runtime-variable-references.md): arithmetic in variable references                               | `Gum.Expressions`                                                                  | `GumUI.UseExpressions();`                  |
| [Themes](../../../styling/themes/): restyles Forms controls created in code                                                     | One package per theme, such as `Gum.Themes.DarkPro.Skia` (add with `--prerelease`) | `Gum.Themes.DarkPro.DarkProTheme.Apply();` |

{% hint style="info" %}
The `.Skia` theme packages are available in December 2026, or now if building Gum from source.
{% endhint %}

## Loading Your Gum Project

To load a Gum project, pass its `.gumj` path as the last argument to `Initialize`:

{% tabs %}
{% tab title="OpenGL" %}
```csharp
// Initialize
GumUI.Initialize(window, "Content/GumProject/GumProject.gumj");
```
{% endtab %}

{% tab title="Vulkan" %}
```csharp
// Initialize
GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface, "Content/GumProject/GumProject.gumj");
```
{% endtab %}
{% endtabs %}

Then copy the Gum project to the output folder by adding this to your `.csproj`:

```xml
<ItemGroup>
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

To create the Gum project and show its screens, see [Loading a Gum Project](../loading-a-gum-project-.gumx.md).

To start building UI, continue with the [code-only Forms tutorial](../../tutorials/code-only-gum-forms-tutorial/).

## Supplying Input From Your Own Window

By default Gum reads the mouse, keyboard, clipboard and gamepad from the `GlfwWindow` you pass to `Initialize`. If your game owns the window and its events itself, such as with SDL3, implement `IBrutalInput` and pass it in place of the window. Gum then never touches GLFW. `IBrutalInput` uses Gum's own `Keys`, `MouseButton` and `Cursors` types.

```csharp
public class MyHostInput : Gum.Input.IBrutalInput
{
    // Update these from your window's events.
    public double CursorX { get; set; }
    public double CursorY { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public int FramebufferWidth { get; set; }
    public int FramebufferHeight { get; set; }
    public string? ClipboardText { get; set; }

    // Raise these from your event loop.
    public event System.Action<double>? Scrolled;
    public event System.Action<uint>? CharTyped;

    // Return what is held right now.
    public bool IsMouseButtonDown(Gum.Input.MouseButton button) => false;
    public bool IsKeyDown(Gum.Forms.Input.Keys key) => false;

    public void SetCursorShape(Gum.Wireframe.Cursors shape) { }
    public bool TryReadGamepad(int index, out Gum.Input.GamePadSnapshot snapshot) { snapshot = default; return false; }
}
```

Pass the input to `Initialize` where the window would go:

{% tabs %}
{% tab title="OpenGL" %}
```csharp
// Initialize
GumUI.Initialize(new MyHostInput());
```
{% endtab %}

{% tab title="Vulkan" %}
```csharp
// Initialize
GumUI.Initialize(new MyHostInput(), instance, physicalDevice, device, queue, queueFamily, surface);
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
`IBrutalInput` is available in December 2026, or now if building Gum from source.
{% endhint %}
