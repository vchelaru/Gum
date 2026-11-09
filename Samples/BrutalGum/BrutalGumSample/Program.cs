// A gallery of Gum screens (the same ones SilkNetGumSample shows) drawn by Gum.Brutal.Vulkan.
// The game creates the window and its Vulkan device and hands them to GumService.Initialize; Gum owns Skia, the
// render target and the swapchain that presents it, so the loop is only PollEvents, Update, Draw.
//
//   BrutalGumSample                                 opens a resizable window
//   BrutalGumSample --hidden --exit-after 5         same, invisible, closes after 5 seconds
//   add --capture <png>     saves the presented swapchain image (the window's pixels) before exiting
//   add --resize 900x500    resizes the window once, halfway through the run
using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Gum;
using GumSamples;
using System.Diagnostics;

SampleArgs sample = new SampleArgs(args);

// The Gum project loads relative to the working directory, so run from the build output whatever the caller's folder is.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

Glfw.Init();
try
{
    // Vulkan draws to the window itself, so GLFW creates no graphics API context.
    Glfw.WindowHint(GlfwWindowHint.ClientApi, Glfw.Constants.GLFW_NO_API);
    SampleSupport.ApplyWindowHints(sample);
    using GlfwWindow window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "Gum.Brutal.Vulkan sample", Size = new int2(1400, 900) });
    SampleSupport.PlaceWindow(window, sample);

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
    GumUI.Initialize(window, instance, physicalDevice, device, queue, queueFamily, surface, "Content/GumProject/GumProject.gumx");
    GumGallery gallery = SampleSupport.CreateGallery(GumUI, window, sample);

    Stopwatch clock = Stopwatch.StartNew();
    double previousSeconds = 0;
    bool resized = false;
    while (!window.ShouldClose)
    {
        Glfw.PollEvents();
        double gameTime = clock.Elapsed.TotalSeconds;
        GumUI.Update(gameTime);
        gallery.Tick(gameTime - previousSeconds);
        previousSeconds = gameTime;
        GumUI.Draw();

        if (sample.ExitAfterSeconds > 0 && gameTime >= sample.ExitAfterSeconds)
        {
            if (sample.CapturePath != null)
            {
                // Test only: proves the frame reached the window by reading the presented swapchain image back.
                device.WaitIdle();
                long[]? presented = (long[]?)typeof(GumService)
                    .GetProperty("LastPresentedForTest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(GumUI);
                if (presented != null)
                {
                    int width = (int)presented[2], height = (int)presented[3];
                    byte[] pixels = PixelReadback.ReadRegion(
                        instance.VkHandle, physicalDevice.VkHandle, device.VkHandle, queue.VkHandle, (uint)queueFamily,
                        (ulong)presented[0], 0, 0, width, height, 1000001002);
                    if (presented[1] == (long)VkFormat.B8G8R8A8UNorm)
                    {
                        for (int i = 0; i < pixels.Length; i += 4)
                        {
                            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
                        }
                    }
                    SampleSupport.SavePng(pixels, width, height, sample.CapturePath);
                }
            }
            window.ShouldClose = true;
        }
        else if (sample.Resize is { } size && !resized && sample.ExitAfterSeconds > 0 && gameTime >= sample.ExitAfterSeconds / 2)
        {
            window.Size = new int2(size.Width, size.Height);
            resized = true;
        }
    }

    device.WaitIdle();
    GumUI.Uninitialize();
    instance.DestroySurfaceKHR(surface, null);
}
finally
{
    Glfw.Terminate();
}
