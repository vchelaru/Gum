using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using GumTests.Shared;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    private VulkanTestDevice()
    {
        bool enableValidation;
        // Only the loader and the instance are guarded: their failure is what "no Vulkan driver" looks like (no
        // vulkan-1.dll or libvulkan, no ICD, an ICD without 1.3). A failure after that is a real failure.
        try
        {
            enableValidation = VulkanHelpers.Validation.IsSupported;
            Instance = CreateInstance(enableValidation);
        }
        catch (Exception e) when (e is VkResultError or DllNotFoundException or EntryPointNotFoundException or TypeInitializationException)
        {
            throw new GpuTestRunner.NoContextException($"no Vulkan loader or driver ({e.GetType().Name}: {e.Message})");
        }
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
            DestroyInstance();
            throw new GpuTestRunner.NoContextException("no Vulkan physical device has a graphics queue family");
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

    private static Instance CreateInstance(bool enableValidation)
    {
        HashSet<string> instanceExtensions = new();
        VulkanHelpers.AddPlatformRequiredExtensions(instanceExtensions); // portability enumeration on macOS
        HashSet<string> layers = new();
        if (enableValidation)
        {
            VulkanHelpers.AddValidationLayers(layers);
            instanceExtensions.Add("VK_EXT_debug_utils");
        }
        return Vulkan.CreateInstance(new Instance.CreateInfo
        {
            AppInfo = new VkApplicationInfo { ApiVersion = (int)VulkanHelpers.Api.VERSION_1_3 },
            Flags = VulkanHelpers.GetPlatformRequiredInstanceCreateFlags(),
            EnabledExtensions = instanceExtensions,
            EnabledLayers = layers,
        });
    }

    /// <summary>
    /// Creates the device and runs <paramref name="body"/> through <see cref="GpuTestRunner"/> (worker thread, watchdog,
    /// skip without a device or fail when GUM_REQUIRE_VULKAN=1), then checks the validation layer reported no error.
    /// </summary>
    public static void Run(Action<VulkanTestDevice> body)
    {
        lock (ValidationErrors) { ValidationErrors.Clear(); }
        GpuTestRunner.Run("Vulkan", "GUM_REQUIRE_VULKAN", () =>
        {
            using VulkanTestDevice device = new VulkanTestDevice();
            body(device);
        });
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

    private void DestroyInstance()
    {
        if (ValidationEnabled)
        {
            Instance.DestroyDebugUtilsMessengerEXT(_messenger, null);
        }
        Instance.Dispose();
    }

    public void Dispose()
    {
        Device.WaitIdle();
        Device.Dispose();
        DestroyInstance();
    }
}
