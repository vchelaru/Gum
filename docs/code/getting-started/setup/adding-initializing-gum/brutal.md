# BRUTAL

## Introduction

This page assumes you have an existing [BRUTAL](https://github.com/ahwootech/BRUTAL) project that opens a GLFW window. See [BRUTAL](../empty-project-before-adding-gum/brutal.md) if you need to create one.

{% hint style="info" %}
Available in preview NuGet packages (use a `-preview` version), or now if building Gum from source.
{% endhint %}

## Adding Gum NuGet package

Gum has one package per graphics API. Add only one of them:

| Graphics API | Package |
| --- | --- |
| OpenGL 3.3 | `Gum.Brutal` |
| Vulkan 1.3 | `Gum.Brutal.Vulkan` |

{% tabs %}
{% tab title="OpenGL" %}
Add the Gum.Brutal NuGet package ([https://www.nuget.org/packages/Gum.Brutal](https://www.nuget.org/packages/Gum.Brutal)). Modify csproj:

```xml
<PackageReference Include="Gum.Brutal" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Brutal
```
{% endtab %}

{% tab title="Vulkan" %}
Add the Gum.Brutal.Vulkan NuGet package ([https://www.nuget.org/packages/Gum.Brutal.Vulkan](https://www.nuget.org/packages/Gum.Brutal.Vulkan)). Modify csproj:

```xml
<PackageReference Include="Gum.Brutal.Vulkan" Version="*" />
```

Or add through command line:

```bash
dotnet add package Gum.Brutal.Vulkan
```
{% endtab %}
{% endtabs %}

Both packages render through SkiaSharp and add real Forms input (mouse, keyboard, gamepad, focus) from a BRUTAL `GlfwWindow`. Gum creates the Skia GPU context and render target itself, recreates the target when the window's framebuffer size changes, and puts the frame on the window. You never call Skia or SkiaGameRendering. Both packages work on Windows and Linux.

Do not reference both packages in one project. They declare the same `GumService` type.

{% hint style="warning" %}
Do not name your project `BrutalGum` (or `BrutalGum.Vulkan`), that is the assembly name inside the package. A same-named project produces a same-named output DLL that silently overwrites the runtime's copy in your `bin` folder. Gum's build catches this for you: if your `AssemblyName` collides, the build fails with an error telling you to change it.
{% endhint %}

## Adding Source (Optional)

You can directly link your project to source instead of a NuGet package for improved debuggability, access to fixes and features before NuGet packages are published, or if you are interested in contributing.

To add source, first clone the Gum repository: [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)

If you have already added the Gum NuGet package to your project, remove it.

Add the following project to your solution:

* \<Gum Root>/Runtimes/BrutalGum/BrutalGum.csproj for OpenGL
* \<Gum Root>/Runtimes/BrutalGum.Vulkan/BrutalGum.Vulkan.csproj for Vulkan

The project already references `GumCommon` and `SkiaGum` itself, so you do not need to add either separately.

Next, add it as a project reference in your game project. Your project might look like this depending on the location of the Gum repository relative to your game project:

```xml
<ProjectReference Include="..\Gum\Runtimes\BrutalGum\BrutalGum.csproj" />
```

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

GumService.Default.Initialize(window);
```
{% endtab %}

{% tab title="Vulkan" %}
Create the window with no graphics API, then your Vulkan 1.3 instance, surface, device (with dynamic rendering and the swapchain extension) and a graphics queue family that can present to the surface:

```csharp
using Brutal.GlfwApi;
using Gum;

// Initialize
GumService.Default.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface);
```

Gum never destroys the objects you pass. Destroy them after `GumService.Default.Uninitialize()`. For a complete setup of the window, instance, surface and device, see the BRUTAL Vulkan sample at `Samples/BrutalGum/BrutalGumSample` in the Gum repository.
{% endtab %}
{% endtabs %}

To load a Gum project (a `.gumj` file) at the same time, pass its path as the last argument:

```csharp
// Initialize
GumService.Default.Initialize(window, "Content/GumProject/GumProject.gumj");
```

You can add controls as soon as `Initialize` returns.

## Updating and Drawing Gum

Call `Update` once per frame after `Glfw.PollEvents`, then `Draw`:

{% tabs %}
{% tab title="OpenGL" %}
```csharp
// Update and Draw
Glfw.PollEvents();
GumService.Default.Update(clock.Elapsed.TotalSeconds);
GumService.Default.Draw();
window.SwapBuffers();
```

`Draw` blits Gum's frame to the window's default framebuffer, so call `SwapBuffers` after it. Skia leaves the OpenGL program, vertex array, buffers, viewport, scissor, blend and depth state as it used them, so set your own again if you draw after Gum.
{% endtab %}

{% tab title="Vulkan" %}
```csharp
// Update and Draw
Glfw.PollEvents();
GumService.Default.Update(clock.Elapsed.TotalSeconds);
GumService.Default.Draw();
```

`Draw` presents the frame to the window itself.
{% endtab %}
{% endtabs %}

Gum's canvas is the window's framebuffer. When the window is resized, `Draw` recreates the render target at the new size and lays out your controls for it. Mouse positions are scaled from window coordinates to framebuffer pixels, so high-DPI displays line up. To change the color behind your controls, set `GumService.Default.ClearColor` (transparent by default, which shows black).

### Using Your Own Renderer (Vulkan)

If your game already has its own Vulkan renderer, turn presenting off and sample Gum's image yourself. Do this right after `Initialize`, before the first `Draw`. The surface passed to `Initialize` is then not used and may be `default`:

```csharp
GumService.Default.Initialize(window, instance, physicalDevice, device, queue, queueFamily, default);
GumService.Default.PresentToWindow = false;

// After each Draw:
VkImage image = GumService.Default.RenderTargetImage!.Value;
VkFormat format = GumService.Default.RenderTargetFormat!.Value;
```

Things to know:

* After `Draw` returns, the image is in `SHADER_READ_ONLY_OPTIMAL` on the queue you passed to `Initialize`. Sample it from a later submission on that queue. From another queue, add your own synchronization.
* Read `RenderTargetImage` again after every `Draw`. The image is replaced when the window size changes, and the old one is freed after the GPU finishes the last frame.
* The image is plain UNORM and holds sRGB bytes. If your pass writes to an sRGB target without decoding the sampled value first, colors are encoded twice and look washed out.

OpenGL has no equivalent switch. `Draw` writes the whole default framebuffer, so anything of yours must be drawn after it.

## Adding Shader Support (Optional)

`Gum.SkiaSharp.ShadowDusk` lets a render target container's `SourceShaderFile` point at an `.fx` or `.slang` shader file.

Add the NuGet package:

```bash
dotnet add package Gum.SkiaSharp.ShadowDusk
```

Then call `UseShadowDusk` after `GumService.Default.Initialize`:

```csharp
// Initialize
GumService.Default.Initialize(window, "Content/GumProject/GumProject.gumj");
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
GumService.Default.Initialize(window, "Content/GumProject/GumProject.gumj");
Gum.Expressions.GumExpressionService.Initialize();
```

If linking to source instead of NuGet, add `<Gum Root>/Runtimes/GumExpressions/GumExpressions.csproj` to your solution.

For more information, see the [Runtime Variable References](../../../styling/runtime-variable-references.md) page.

## Adding a Button (Testing the Setup)

Gum can be tested by adding a Button after Gum is initialized. To do so, add code to create a `Button` as shown in the following block of code after Gum is initialized:

```csharp
// Initialize
GumService.Default.Initialize(window);

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{System.DateTime.Now}";
```

`Button` comes from `Gum.Forms.Controls`, `Anchor` from `Gum.Wireframe`.

## Full Program

The following is a complete `Program.cs` that opens a window, initializes Gum, shows a button and runs the game loop. Lines marked Optional need the extra packages described above.

{% tabs %}
{% tab title="OpenGL" %}
```csharp
using Brutal.GlfwApi;
using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using System.Diagnostics;

Glfw.Init();
Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 3);
Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 3);
Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, 0x32001 /* GLFW_OPENGL_CORE_PROFILE */);
using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "My Game", Size = new Brutal.Numerics.int2(800, 450) });
window.MakeContextCurrent();
Glfw.SwapInterval(1);

GumService GumUI = GumService.Default;
GumUI.Initialize(window, "Content/GumProject/GumProject.gumj");
GumUI.UseShadowDusk(); // Optional: .fx and .slang shader files (Gum.SkiaSharp.ShadowDusk)
Gum.Expressions.GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references (Gum.Expressions)

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{DateTime.Now}";

Stopwatch clock = Stopwatch.StartNew();
while (!window.ShouldClose)
{
    Glfw.PollEvents();
    GumUI.Update(clock.Elapsed.TotalSeconds);
    GumUI.Draw();
    window.SwapBuffers();
}

Glfw.Terminate();
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

GumService GumUI = GumService.Default;
GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface, "Content/GumProject/GumProject.gumj");
GumUI.UseShadowDusk(); // Optional: .fx and .slang shader files (Gum.SkiaSharp.ShadowDusk)
Gum.Expressions.GumExpressionService.Initialize(); // Optional: arithmetic expressions in variable references (Gum.Expressions)

var button = new Button();
button.AddToRoot();
button.Width = 200;
button.Anchor(Anchor.Center);
button.Click += (_, _) => button.Text = $"Clicked\n{DateTime.Now}";

Stopwatch clock = Stopwatch.StartNew();
while (!window.ShouldClose)
{
    Glfw.PollEvents();
    GumUI.Update(clock.Elapsed.TotalSeconds);
    GumUI.Draw();
}

device.WaitIdle();
GumUI.Uninitialize();
instance.DestroySurfaceKHR(surface, null);
Glfw.Terminate();
```
{% endtab %}
{% endtabs %}
