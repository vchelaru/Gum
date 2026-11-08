// A gallery of Gum screens drawn by Gum.Brutal into a SkiaGameRendering Vulkan image, which a BRUTAL
// renderer (see Presenter.cs) samples on a full-screen triangle every frame. The screens are the same
// ones SilkNetGumSample shows. Click the buttons along the top (or press 1..9) to switch screens.
//
//   BrutalGumSample                      opens a window
//   BrutalGumSample --screenshots <dir>  hidden window, saves one PNG per screen into <dir>, then exits
//   BrutalGumSample --hidden --exit-after 5   presents to a hidden window for 5 seconds, then exits
using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Gum;
using GumSamples;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaGameRendering;
using SkiaGameRendering.Core.VK;
using SkiaSharp;
using System.Diagnostics;
using System.Text;

const int Width = 1400;
const int Height = 900;

string? screenshotDirectory = null;
int screenshotIndex = Array.IndexOf(args, "--screenshots");
if (screenshotIndex >= 0 && screenshotIndex + 1 < args.Length)
{
    screenshotDirectory = args[screenshotIndex + 1];
}
bool presenting = screenshotDirectory == null;
// --hidden keeps the window invisible and --exit-after <seconds> closes it, for runs with nobody watching.
bool hidden = args.Contains("--hidden");
int exitIndex = Array.IndexOf(args, "--exit-after");
double exitAfterSeconds = exitIndex >= 0 && exitIndex + 1 < args.Length ? double.Parse(args[exitIndex + 1]) : 0;

// The Gum project and shaders load relative to the working directory, so run from the build output whatever the caller's folder is.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);


if (!Glfw.Init())
{
    throw new InvalidOperationException("Failed to initialize GLFW.");
}

try
{
    // BRUTAL has no renderer, so the window needs no graphics API of its own.
    Glfw.WindowHint(GlfwWindowHint.ClientApi, Glfw.Constants.GLFW_NO_API);
    Glfw.WindowHint(GlfwWindowHint.Resizable, Glfw.Constants.GLFW_FALSE);
    Glfw.WindowHint(GlfwWindowHint.Visible, presenting && !hidden ? Glfw.Constants.GLFW_TRUE : Glfw.Constants.GLFW_FALSE);
    using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "Gum.Brutal sample", Size = new int2(Width, Height) });

    HashSet<string> instanceExtensions = new HashSet<string>();
    VulkanHelpers.AddPlatformRequiredExtensions(instanceExtensions);
    if (presenting)
    {
        unsafe
        {
            var glfwExtensions = Glfw.GetRequiredInstanceExtensions(out var count);
            for (int i = 0; i < count; i++)
            {
                instanceExtensions.Add(Brutal.Strings.Utf8.GetString(glfwExtensions[i]));
            }
        }
    }

    using Instance instance = Vulkan.CreateInstance(new Instance.CreateInfo
    {
        AppInfo = new VkApplicationInfo { ApiVersion = (int)VulkanHelpers.Api.VERSION_1_3 },
        Flags = VulkanHelpers.GetPlatformRequiredInstanceCreateFlags(),
        EnabledExtensions = instanceExtensions,
        EnabledLayers = [],
    });

    VkSurfaceKHR surface = default;
    if (presenting)
    {
        if (Glfw.CreateWindowSurface(instance.VkHandle, window, out var surfaceHandle) != (int)VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create a Vulkan surface for the window.");
        }
        surface = new VkSurfaceKHR((nint)surfaceHandle);
    }

    // One queue draws and presents, so find a device with a queue family that can do both.
    PhysicalDevice? physicalDevice = null;
    int queueFamily = -1;
    foreach (PhysicalDevice candidate in instance.PhysicalDevices)
    {
        var families = new VkQueueFamilyProperties[candidate.GetQueueFamilyProperties_Count()];
        candidate.GetQueueFamilyProperties_Write(families);
        for (int i = 0; i < families.Length && physicalDevice is null; i++)
        {
            bool canPresent = !presenting || candidate.GetSurfaceSupportKHR(i, surface);
            if ((families[i].QueueFlags & VkQueueFlags.GraphicsBit) != 0 && canPresent)
            {
                (physicalDevice, queueFamily) = (candidate, i);
            }
        }
    }
    if (physicalDevice is null)
    {
        throw new InvalidOperationException("No Vulkan device has a graphics queue family" + (presenting ? " that can present to the window." : "."));
    }

    HashSet<string> deviceExtensions = new HashSet<string>();
    if (presenting)
    {
        VulkanHelpers.AddSwapchainExtensions(deviceExtensions);
    }
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

    // Skia draws into this Vulkan image. The window size is the target size, so cursor positions match Gum.
    using SkiaVulkanContext context = new SkiaVulkanContext(
        instance.VkHandle, physicalDevice.VkHandle, device.VkHandle, queue.VkHandle, (uint)queueFamily);
    using SkiaRenderTarget2D target = new SkiaRenderTarget2D(context, Width, Height);

    GumService gum = GumService.Default;
    gum.Initialize(window, Width, Height, "Content/GumProject/GumProject.gumx");
    // Turns an RT Shader screen .sksl path into a compiled effect, then lets .fx and .slang files work too.
    SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SilkNetGum.Screens.RenderTargetShaderScreen.CompileEffectFromFile;
    gum.UseShadowDusk();
    gum.UseKeyboardDefaults();
    FrameworkElement.GamePadsForUiControl.Clear();
    FrameworkElement.GamePadsForUiControl.AddRange(gum.Gamepads);

    GumGallery gallery = new GumGallery();
    gallery.Build();

    Stopwatch clock = Stopwatch.StartNew();

    double previousSeconds = 0;
    void RenderFrame()
    {
        double now = clock.Elapsed.TotalSeconds;
        gum.Update(now);
        gallery.Tick(now - previousSeconds);
        previousSeconds = now;

        target.Begin();
        target.Canvas.Clear(new SKColor(32, 32, 40));
        gum.Draw(target.Canvas);
        target.End();
    }

    if (screenshotDirectory != null)
    {
        Directory.CreateDirectory(screenshotDirectory);
        for (int i = 0; i < gallery.ScreenCount; i++)
        {
            gallery.LoadScreen(i);
            for (int frame = 0; frame < 6; frame++)
            {
                Glfw.PollEvents();
                RenderFrame();
            }
            device.WaitIdle();

            byte[] rgba = PixelReadback.ReadRegion(
                instance.VkHandle, physicalDevice.VkHandle, device.VkHandle, queue.VkHandle, (uint)queueFamily,
                target.VkImage, 0, 0, Width, Height);
            string path = Path.Combine(screenshotDirectory, $"brutal-{i + 1}.png");
            using SKBitmap bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
            using SKData png = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, png.ToArray());
            Console.WriteLine(path);
        }
        device.WaitIdle();
        return 0;
    }

    window.OnKey += (_, key, _, action, _) =>
    {
        if (action == GlfwKeyAction.Press && key >= GlfwKey.Number1 && key < GlfwKey.Number1 + Math.Min(9, gallery.ScreenCount))
        {
            gallery.LoadScreen(key - GlfwKey.Number1);
        }
    };

    using Presenter presenter = new Presenter(instance, physicalDevice, device, queue, queueFamily, surface, window, target);
    while (!window.ShouldClose && (exitAfterSeconds <= 0 || clock.Elapsed.TotalSeconds < exitAfterSeconds))
    {
        Glfw.PollEvents();
        RenderFrame();
        presenter.Present();
    }

    device.WaitIdle();
    return 0;
}
finally
{
    Glfw.Terminate();
}
