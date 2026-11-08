// Copied from SkiaGameRendering (MIT, samples/Sample.Brutal/PixelReadback.cs). Test-only Vulkan readback for the screenshot mode.
using System.Runtime.InteropServices;
using static Tests.CoreVK.VulkanTestNative;

/// <summary>Test-only Vulkan readback, so the smoke test can assert a pixel without any library CPU path.</summary>
static class PixelReadback
{
    static uint FindType(IntPtr physical, uint bits, uint props)
    {
        vkGetPhysicalDeviceMemoryProperties(physical, out var memProps);
        for (int i = 0; i < memProps.memoryTypeCount; i++)
            if ((bits & (1u << i)) != 0 && (memProps.memoryTypes[i].propertyFlags & props) == props)
                return (uint)i;
        throw new InvalidOperationException("No memory type.");
    }

    // Test-only: copies one texel of a SHADER_READ_ONLY_OPTIMAL image to a host buffer and returns RGBA.
    public static byte[] ReadPixel(IntPtr instance, IntPtr physical, IntPtr device, IntPtr queue, uint family, ulong imageHandle, int x, int y, uint oldLayout = 5) =>
        ReadRegion(instance, physical, device, queue, family, imageHandle, x, y, 1, 1, oldLayout);

    /// <summary>Copies a w x h region to a host buffer and returns tightly packed top-down RGBA. oldLayout is 5 (SHADER_READ_ONLY_OPTIMAL) or 2 (COLOR_ATTACHMENT_OPTIMAL).</summary>
    public static unsafe byte[] ReadRegion(IntPtr instance, IntPtr physical, IntPtr device, IntPtr queue, uint family, ulong imageHandle, int x, int y, int w, int h, uint oldLayout = 5)
    {
        var image = (IntPtr)(long)imageHandle;
        static void Ok(int r, string what) { if (r != VK_SUCCESS) throw new InvalidOperationException($"{what} failed: {r}"); }

        var bufferInfo = new VkBufferCreateInfo
        {
            sType = VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO, size = (ulong)(w * h * 4), usage = VK_BUFFER_USAGE_TRANSFER_DST_BIT, sharingMode = VK_SHARING_MODE_EXCLUSIVE,
        };
        Ok(vkCreateBuffer(device, &bufferInfo, IntPtr.Zero, out var buffer), "vkCreateBuffer");
        vkGetBufferMemoryRequirements(device, buffer, out var req);
        var alloc = new VkMemoryAllocateInfo
        {
            sType = VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO, allocationSize = req.size,
            memoryTypeIndex = FindType(physical, req.memoryTypeBits, VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT),
        };
        Ok(vkAllocateMemory(device, &alloc, IntPtr.Zero, out var memory), "vkAllocateMemory");
        Ok(vkBindBufferMemory(device, buffer, memory, 0), "vkBindBufferMemory");

        var poolInfo = new VkCommandPoolCreateInfo { sType = VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO, queueFamilyIndex = family };
        Ok(vkCreateCommandPool(device, &poolInfo, IntPtr.Zero, out var pool), "vkCreateCommandPool");
        var cbInfo = new VkCommandBufferAllocateInfo
        {
            sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO, commandPool = pool, level = VK_COMMAND_BUFFER_LEVEL_PRIMARY, commandBufferCount = 1,
        };
        Ok(vkAllocateCommandBuffers(device, &cbInfo, out var cb), "vkAllocateCommandBuffers");
        var begin = new VkCommandBufferBeginInfo { sType = VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO, flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT };
        Ok(vkBeginCommandBuffer(cb, &begin), "vkBeginCommandBuffer");

        var barrier = new VkImageMemoryBarrier
        {
            sType = VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            srcAccessMask = oldLayout == 2 ? 0x100u /* COLOR_ATTACHMENT_WRITE */ : 0x20u /* SHADER_READ */, dstAccessMask = VK_ACCESS_TRANSFER_READ_BIT,
            oldLayout = oldLayout, newLayout = VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED, image = image,
            subresourceRange = new VkImageSubresourceRange { aspectMask = VK_IMAGE_ASPECT_COLOR_BIT, levelCount = 1, layerCount = 1 },
        };
        vkCmdPipelineBarrier(cb, oldLayout == 2 ? 0x400u /* COLOR_ATTACHMENT_OUTPUT */ : 0x80u /* FRAGMENT_SHADER */, VK_PIPELINE_STAGE_TRANSFER_BIT, 0, 0, IntPtr.Zero, 0, IntPtr.Zero, 1, &barrier);
        var region = new VkBufferImageCopy
        {
            imageSubresource = new VkImageSubresourceLayers { aspectMask = VK_IMAGE_ASPECT_COLOR_BIT, layerCount = 1 },
            imageOffset = new VkOffset3D { x = x, y = y, z = 0 },
            imageExtent = new VkExtent3D { width = (uint)w, height = (uint)h, depth = 1 },
        };
        vkCmdCopyImageToBuffer(cb, image, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, buffer, 1, &region);
        Ok(vkEndCommandBuffer(cb), "vkEndCommandBuffer");

        var fenceInfo = new VkFenceCreateInfo { sType = VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };
        Ok(vkCreateFence(device, &fenceInfo, IntPtr.Zero, out var fence), "vkCreateFence");
        var cbLocal = cb;
        var submit = new VkSubmitInfo { sType = VK_STRUCTURE_TYPE_SUBMIT_INFO, commandBufferCount = 1, pCommandBuffers = (IntPtr)(&cbLocal) };
        Ok(vkQueueSubmit(queue, 1, &submit, fence), "vkQueueSubmit");
        var fenceLocal = fence;
        Ok(vkWaitForFences(device, 1, ref fenceLocal, true, UINT64_MAX), "vkWaitForFences");

        Ok(vkMapMemory(device, memory, 0, (ulong)(w * h * 4), 0, out var mapped), "vkMapMemory");
        var bytes = new byte[w * h * 4];
        Marshal.Copy(mapped, bytes, 0, bytes.Length);
        vkUnmapMemory(device, memory);

        vkDestroyFence(device, fence, IntPtr.Zero);
        vkFreeCommandBuffers(device, pool, 1, ref cbLocal);
        vkDestroyCommandPool(device, pool, IntPtr.Zero);
        vkFreeMemory(device, memory, IntPtr.Zero);
        vkDestroyBuffer(device, buffer, IntPtr.Zero);
        return bytes;
    }
}
