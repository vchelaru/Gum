// Copied from SkiaGameRendering (MIT, tests/Tests.Core.VK/VulkanTestNative.cs). Test-only raw Vulkan P/Invokes for the screenshot readback.
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tests.CoreVK;

/// <summary>
/// Raw Vulkan P/Invokes, structs and constants used ONLY by the test project to play the "host
/// engine" role - creating its own real <c>VkInstance</c>/<c>VkPhysicalDevice</c>/<c>VkDevice</c>/
/// <c>VkQueue</c> and a host-owned <c>VkImage</c>, exactly like <c>Tests.Core.ANGLE</c>'s
/// <c>D3D11RawResources.cs</c> and <c>Tests.Core.OGL</c>'s <c>GlRawTestFunctions.cs</c> keep their own
/// independent raw bindings rather than reusing the library under test's own P/Invoke declarations
/// (<c>VulkanNative</c> in the main project) - a bug in the shared plumbing should not also hide
/// itself from the test that's supposed to catch it.
///
/// Deliberately minimal: no validation layers, no extensions, no optional features - just enough raw
/// Vulkan to stand up a device, allocate an image, hand it to <c>VkSkiaSurfaceFactory</c>, and read
/// the drawn pixels back via a staging buffer copy.
/// </summary>
internal static unsafe class VulkanTestNative
{
    const string VulkanLoader = "vulkan-1";

    static VulkanTestNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(VulkanTestNative).Assembly, (name, assembly, searchPath) =>
        {
            if (name != VulkanLoader)
                return IntPtr.Zero;

            if (NativeLibrary.TryLoad("vulkan-1.dll", out var winHandle))
                return winHandle;
            if (NativeLibrary.TryLoad("libvulkan.so.1", out var linuxHandle))
                return linuxHandle;
            if (NativeLibrary.TryLoad("libvulkan.dylib", out var macHandle))
                return macHandle;
            if (NativeLibrary.TryLoad("libMoltenVK.dylib", out var moltenHandle))
                return moltenHandle;

            return IntPtr.Zero;
        });
    }

    // ---- sType values (VkStructureType) ----
    internal const uint VK_STRUCTURE_TYPE_APPLICATION_INFO = 0;
    internal const uint VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO = 1;
    internal const uint VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO = 2;
    internal const uint VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO = 3;
    internal const uint VK_STRUCTURE_TYPE_SUBMIT_INFO = 4;
    internal const uint VK_STRUCTURE_TYPE_FENCE_CREATE_INFO = 8;
    internal const uint VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO = 5;
    internal const uint VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO = 39;
    internal const uint VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO = 40;
    internal const uint VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO = 42;
    internal const uint VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO = 12;
    internal const uint VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO = 14;
    internal const uint VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER = 45;

    // ---- misc enums/flags ----
    internal const uint VK_API_VERSION_1_1 = (1u << 22) | (1u << 12) | 0u;
    internal const uint VK_QUEUE_GRAPHICS_BIT = 0x1;
    internal const uint VK_IMAGE_TYPE_2D = 1;
    internal const uint VK_FORMAT_R8G8B8A8_UNORM = 37;
    internal const uint VK_IMAGE_TILING_OPTIMAL = 0;
    internal const uint VK_SAMPLE_COUNT_1_BIT = 1;
    internal const uint VK_SHARING_MODE_EXCLUSIVE = 0;
    internal const uint VK_IMAGE_LAYOUT_UNDEFINED = 0;
    internal const uint VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL = 2;
    internal const uint VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL = 6;
    internal const uint VK_IMAGE_USAGE_TRANSFER_SRC_BIT = 0x1;
    internal const uint VK_IMAGE_USAGE_TRANSFER_DST_BIT = 0x2;
    internal const uint VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT = 0x10;
    internal const uint VK_BUFFER_USAGE_TRANSFER_DST_BIT = 0x2;
    internal const uint VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT = 0x1;
    internal const uint VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT = 0x2;
    internal const uint VK_MEMORY_PROPERTY_HOST_COHERENT_BIT = 0x4;
    internal const uint VK_IMAGE_ASPECT_COLOR_BIT = 0x1;
    internal const uint VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT = 0x100;
    internal const uint VK_ACCESS_TRANSFER_READ_BIT = 0x800;
    internal const uint VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT = 0x400;
    internal const uint VK_PIPELINE_STAGE_TRANSFER_BIT = 0x1000;
    internal const uint VK_COMMAND_BUFFER_LEVEL_PRIMARY = 0;
    internal const uint VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT = 0x1;
    internal const uint VK_QUEUE_FAMILY_IGNORED = 0xFFFFFFFF;
    internal const int VK_SUCCESS = 0;
    internal const ulong VK_WHOLE_SIZE = ulong.MaxValue;
    internal const ulong UINT64_MAX = ulong.MaxValue;

    // ---- structs ----
    [StructLayout(LayoutKind.Sequential)]
    internal struct VkApplicationInfo
    {
        public uint sType;
        public IntPtr pNext;
        public IntPtr pApplicationName;
        public uint applicationVersion;
        public IntPtr pEngineName;
        public uint engineVersion;
        public uint apiVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkInstanceCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public IntPtr pApplicationInfo;
        public uint enabledLayerCount;
        public IntPtr ppEnabledLayerNames;
        public uint enabledExtensionCount;
        public IntPtr ppEnabledExtensionNames;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkExtent3D { public uint width, height, depth; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkQueueFamilyProperties
    {
        public uint queueFlags;
        public uint queueCount;
        public uint timestampValidBits;
        public VkExtent3D minImageTransferGranularity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkDeviceQueueCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public uint queueFamilyIndex;
        public uint queueCount;
        public IntPtr pQueuePriorities;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkDeviceCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public uint queueCreateInfoCount;
        public IntPtr pQueueCreateInfos;
        public uint enabledLayerCount;
        public IntPtr ppEnabledLayerNames;
        public uint enabledExtensionCount;
        public IntPtr ppEnabledExtensionNames;
        public IntPtr pEnabledFeatures;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkImageCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public uint imageType;
        public uint format;
        public VkExtent3D extent;
        public uint mipLevels;
        public uint arrayLayers;
        public uint samples;
        public uint tiling;
        public uint usage;
        public uint sharingMode;
        public uint queueFamilyIndexCount;
        public IntPtr pQueueFamilyIndices;
        public uint initialLayout;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkMemoryRequirements
    {
        public ulong size;
        public ulong alignment;
        public uint memoryTypeBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkMemoryType { public uint propertyFlags; public uint heapIndex; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkMemoryHeap { public ulong size; public uint flags; }

    [InlineArray(32)]
    internal struct VkMemoryTypeArray32 { VkMemoryType _element0; }

    [InlineArray(16)]
    internal struct VkMemoryHeapArray16 { VkMemoryHeap _element0; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkPhysicalDeviceMemoryProperties
    {
        public uint memoryTypeCount;
        public VkMemoryTypeArray32 memoryTypes;
        public uint memoryHeapCount;
        public VkMemoryHeapArray16 memoryHeaps;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkMemoryAllocateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public ulong allocationSize;
        public uint memoryTypeIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkBufferCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public ulong size;
        public uint usage;
        public uint sharingMode;
        public uint queueFamilyIndexCount;
        public IntPtr pQueueFamilyIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkCommandPoolCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public uint queueFamilyIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkCommandBufferAllocateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public IntPtr commandPool;
        public uint level;
        public uint commandBufferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkCommandBufferBeginInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
        public IntPtr pInheritanceInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkImageSubresourceRange
    {
        public uint aspectMask;
        public uint baseMipLevel;
        public uint levelCount;
        public uint baseArrayLayer;
        public uint layerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkImageMemoryBarrier
    {
        public uint sType;
        public IntPtr pNext;
        public uint srcAccessMask;
        public uint dstAccessMask;
        public uint oldLayout;
        public uint newLayout;
        public uint srcQueueFamilyIndex;
        public uint dstQueueFamilyIndex;
        public IntPtr image;
        public VkImageSubresourceRange subresourceRange;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkOffset3D { public int x, y, z; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkImageSubresourceLayers
    {
        public uint aspectMask;
        public uint mipLevel;
        public uint baseArrayLayer;
        public uint layerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkBufferImageCopy
    {
        public ulong bufferOffset;
        public uint bufferRowLength;
        public uint bufferImageHeight;
        public VkImageSubresourceLayers imageSubresource;
        public VkOffset3D imageOffset;
        public VkExtent3D imageExtent;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkSubmitInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint waitSemaphoreCount;
        public IntPtr pWaitSemaphores;
        public IntPtr pWaitDstStageMask;
        public uint commandBufferCount;
        public IntPtr pCommandBuffers;
        public uint signalSemaphoreCount;
        public IntPtr pSignalSemaphores;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VkFenceCreateInfo
    {
        public uint sType;
        public IntPtr pNext;
        public uint flags;
    }

    // ---- functions ----
    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkCreateInstance(VkInstanceCreateInfo* pCreateInfo, IntPtr pAllocator, out IntPtr pInstance);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkDestroyInstance(IntPtr instance, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkEnumeratePhysicalDevices(IntPtr instance, ref uint pPhysicalDeviceCount, [In, Out] IntPtr[]? pPhysicalDevices);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkGetPhysicalDeviceQueueFamilyProperties(IntPtr physicalDevice, ref uint pQueueFamilyPropertyCount, [In, Out] VkQueueFamilyProperties[]? pQueueFamilyProperties);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkGetPhysicalDeviceMemoryProperties(IntPtr physicalDevice, out VkPhysicalDeviceMemoryProperties pMemoryProperties);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkCreateDevice(IntPtr physicalDevice, VkDeviceCreateInfo* pCreateInfo, IntPtr pAllocator, out IntPtr pDevice);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkDestroyDevice(IntPtr device, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkGetDeviceQueue(IntPtr device, uint queueFamilyIndex, uint queueIndex, out IntPtr pQueue);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkCreateImage(IntPtr device, VkImageCreateInfo* pCreateInfo, IntPtr pAllocator, out IntPtr pImage);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkDestroyImage(IntPtr device, IntPtr image, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkGetImageMemoryRequirements(IntPtr device, IntPtr image, out VkMemoryRequirements pMemoryRequirements);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkAllocateMemory(IntPtr device, VkMemoryAllocateInfo* pAllocateInfo, IntPtr pAllocator, out IntPtr pMemory);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkFreeMemory(IntPtr device, IntPtr memory, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkBindImageMemory(IntPtr device, IntPtr image, IntPtr memory, ulong memoryOffset);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkCreateBuffer(IntPtr device, VkBufferCreateInfo* pCreateInfo, IntPtr pAllocator, out IntPtr pBuffer);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkDestroyBuffer(IntPtr device, IntPtr buffer, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkGetBufferMemoryRequirements(IntPtr device, IntPtr buffer, out VkMemoryRequirements pMemoryRequirements);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkBindBufferMemory(IntPtr device, IntPtr buffer, IntPtr memory, ulong memoryOffset);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkMapMemory(IntPtr device, IntPtr memory, ulong offset, ulong size, uint flags, out IntPtr ppData);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkUnmapMemory(IntPtr device, IntPtr memory);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkCreateCommandPool(IntPtr device, VkCommandPoolCreateInfo* pCreateInfo, IntPtr pAllocator, out IntPtr pCommandPool);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkDestroyCommandPool(IntPtr device, IntPtr commandPool, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkAllocateCommandBuffers(IntPtr device, VkCommandBufferAllocateInfo* pAllocateInfo, out IntPtr pCommandBuffers);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkBeginCommandBuffer(IntPtr commandBuffer, VkCommandBufferBeginInfo* pBeginInfo);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkEndCommandBuffer(IntPtr commandBuffer);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkCmdPipelineBarrier(
        IntPtr commandBuffer, uint srcStageMask, uint dstStageMask, uint dependencyFlags,
        uint memoryBarrierCount, IntPtr pMemoryBarriers,
        uint bufferMemoryBarrierCount, IntPtr pBufferMemoryBarriers,
        uint imageMemoryBarrierCount, VkImageMemoryBarrier* pImageMemoryBarriers);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkCmdCopyImageToBuffer(
        IntPtr commandBuffer, IntPtr srcImage, uint srcImageLayout, IntPtr dstBuffer,
        uint regionCount, VkBufferImageCopy* pRegions);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkQueueSubmit(IntPtr queue, uint submitCount, VkSubmitInfo* pSubmits, IntPtr fence);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkCreateFence(IntPtr device, VkFenceCreateInfo* pCreateInfo, IntPtr pAllocator, out IntPtr pFence);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkDestroyFence(IntPtr device, IntPtr fence, IntPtr pAllocator);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkWaitForFences(IntPtr device, uint fenceCount, ref IntPtr pFences, [MarshalAs(UnmanagedType.Bool)] bool waitAll, ulong timeout);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern void vkFreeCommandBuffers(IntPtr device, IntPtr commandPool, uint commandBufferCount, ref IntPtr pCommandBuffers);

    [DllImport(VulkanLoader, CallingConvention = CallingConvention.Winapi)]
    internal static extern int vkDeviceWaitIdle(IntPtr device);
}
