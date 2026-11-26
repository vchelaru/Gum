using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BrutalGum.Tests;

/// <summary>
/// A headless Vulkan 1.3 device (no window, no surface) for tests that run Gum on a real GPU or on Mesa's lavapipe.
/// Without a device the test skips, or fails when GUM_REQUIRE_VULKAN=1 (the CI streams that install a driver).
/// When the Khronos validation layer is installed, its errors fail the test.
/// </summary>
internal sealed unsafe class VulkanTestDevice : IDisposable
{
    private static readonly List<string> ValidationErrors = new();

    public Instance Instance { get; }
    public PhysicalDevice PhysicalDevice { get; }
    public Device Device { get; }
    public Queue Queue { get; }
    public int QueueFamily { get; }
    public string DeviceName { get; }
    public bool ValidationEnabled { get; }

    private readonly VkDebugUtilsMessengerEXT _messenger;

    private VulkanTestDevice(bool enableValidation)
    {
        HashSet<string> instanceExtensions = new();
        VulkanHelpers.AddPlatformRequiredExtensions(instanceExtensions); // portability enumeration on macOS
        HashSet<string> layers = new();
        if (enableValidation)
        {
            VulkanHelpers.AddValidationLayers(layers);
            instanceExtensions.Add("VK_EXT_debug_utils");
        }
        Instance = Vulkan.CreateInstance(new Instance.CreateInfo
        {
            AppInfo = new VkApplicationInfo { ApiVersion = (int)VulkanHelpers.Api.VERSION_1_3 },
            Flags = VulkanHelpers.GetPlatformRequiredInstanceCreateFlags(),
            EnabledExtensions = instanceExtensions,
            EnabledLayers = layers,
        });
        if (enableValidation)
        {
            VkDebugUtilsMessengerCreateInfoEXT messengerInfo = VulkanHelpers.BuildDefaultDebugMessengerCreateInfo(new PfnVkDebugUtilsMessengerCallbackEXT(&OnValidationMessage));
            _messenger = Instance.CreateDebugUtilsMessengerEXT(messengerInfo, null);
            ValidationEnabled = true;
        }

        PhysicalDevice? chosen = null;
        int family = -1;
        foreach (PhysicalDevice candidate in Instance.PhysicalDevices)
        {
            var families = new VkQueueFamilyProperties[candidate.GetQueueFamilyProperties_Count()];
            candidate.GetQueueFamilyProperties_Write(families);
            for (int i = 0; i < families.Length && chosen is null; i++)
            {
                if ((families[i].QueueFlags & VkQueueFlags.GraphicsBit) != 0)
                {
                    (chosen, family) = (candidate, i);
                }
            }
            if (chosen is not null)
            {
                break;
            }
        }
        if (chosen is null)
        {
            Instance.Dispose();
            throw new InvalidOperationException("no Vulkan physical device has a graphics queue family");
        }
        PhysicalDevice = chosen;
        QueueFamily = family;
        VkPhysicalDeviceProperties properties = default;
        chosen.GetProperties(ref properties);
        DeviceName = VulkanDeviceInfo.GetDeviceName(properties);

        HashSet<string> deviceExtensions = new();
        VulkanHelpers.AddMandatoryIfSupportedDeviceExtensions(deviceExtensions, chosen); // VK_KHR_portability_subset on macOS
        float priority = 1.0f;
        VkPhysicalDeviceVulkan13Features features13 = new VkPhysicalDeviceVulkan13Features { DynamicRendering = true };
        Device = chosen.CreateDevice(new DeviceEx.CreateInfo
        {
            Extensions = deviceExtensions,
            QueueCreateInfos = [new VkDeviceQueueCreateInfo { QueueFamilyIndex = family, QueueCount = 1, QueuePriorities = &priority }],
            Next = &features13,
        });
        Queue = Device.GetQueue(family, 0);
    }

    /// <summary>
    /// Creates the device, runs <paramref name="body"/> on a worker thread with a watchdog (a stuck driver fails the test
    /// instead of hanging the job), then checks the validation layer reported no error.
    /// </summary>
    public static void Run(Action<VulkanTestDevice> body)
    {
        Exception? failure = null;
        string? noDevice = null;
        Thread worker = new Thread(() =>
        {
            VulkanTestDevice? device = null;
            try
            {
                try
                {
                    device = new VulkanTestDevice(VulkanHelpers.Validation.IsSupported);
                }
                catch (Exception e)
                {
                    noDevice = "no Vulkan 1.3 device: " + e.Message;
                    return;
                }
                lock (ValidationErrors) { ValidationErrors.Clear(); }
                body(device);
            }
            catch (Exception e)
            {
                failure = e;
            }
            finally
            {
                device?.Dispose();
            }
        }) { IsBackground = true };
        worker.Start();
        if (!worker.Join(TimeSpan.FromSeconds(60)))
        {
            throw new TimeoutException("The Vulkan test did not finish within 60 seconds.");
        }
        if (noDevice != null)
        {
            if (Environment.GetEnvironmentVariable("GUM_REQUIRE_VULKAN") == "1")
            {
                throw new InvalidOperationException(noDevice + " (GUM_REQUIRE_VULKAN=1)");
            }
            Skip.If(true, noDevice);
        }
        if (failure != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        lock (ValidationErrors)
        {
            if (ValidationErrors.Count > 0)
            {
                throw new InvalidOperationException("Vulkan validation errors:\n" + string.Join("\n", ValidationErrors));
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static Brutal.Bool32 OnValidationMessage(
        VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT types,
        VkDebugUtilsMessengerCallbackDataEXT* data, void* userData)
    {
        if ((severity & VkDebugUtilsMessageSeverityFlagsEXT.ErrorBitEXT) != 0)
        {
            string message = data->Message == null ? "(no message)" : Marshal.PtrToStringUTF8((nint)data->Message) ?? "";
            lock (ValidationErrors) { ValidationErrors.Add(message); }
        }
        return default;
    }

    public void Dispose()
    {
        Device.WaitIdle();
        Device.Dispose();
        if (ValidationEnabled)
        {
            Instance.DestroyDebugUtilsMessengerEXT(_messenger, null);
        }
        Instance.Dispose();
    }
}
