#nullable enable

using Brutal.ShaderCApi;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using System;
using System.Linq;

namespace Gum;

/// <summary>
/// Shows Gum's Vulkan image on the window: a swapchain over the game's surface and a pipeline that samples the
/// image on a full-screen triangle. Modeled on BRUTAL's samples/Demo/DemoVulkan.cs (swapchain, acquire,
/// present). The shaders, sampler and command objects live as long as the presenter; the swapchain, its views,
/// the descriptor set and the pipeline are rebuilt by <see cref="Configure"/> whenever the framebuffer size or
/// the source image changes. The game owns the surface and device and they are never destroyed here.
/// </summary>
internal sealed unsafe class VulkanPresenter : IDisposable
{
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly Queue _queue;
    private readonly VkSurfaceKHR _surface;
    private readonly VkSampler _sampler;
    private readonly VkDescriptorSetLayout _setLayout;
    private readonly ShaderStages _stages;
    private readonly VkPipelineLayout _pipelineLayout;
    private readonly VkCommandPool _commandPool;
    private readonly CommandBuffer _commandBuffer;
    private readonly VkSemaphore _imageAvailable;
    private readonly VkFence _frameFence;
    private readonly VkImageSubresourceRange _colorRange = new VkImageSubresourceRange
    {
        AspectMask = VkImageAspectFlags.ColorBit,
        LevelCount = 1,
        LayerCount = 1,
    };

    // Rebuilt by Configure.
    private bool _configured;
    private VkSwapchainKHR _swapchain;
    private VkImage[] _images = Array.Empty<VkImage>();
    private VkImageView[] _imageViews = Array.Empty<VkImageView>();
    private VkSemaphore[] _renderFinished = Array.Empty<VkSemaphore>();
    private VkExtent2D _extent;
    private VkImageView _sourceView;
    private VkDescriptorPool _pool;
    private readonly VkDescriptorSet[] _sets = new VkDescriptorSet[1];
    private VkPipeline _pipeline;

    public VulkanPresenter(PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily, VkSurfaceKHR surface)
    {
        _physicalDevice = physicalDevice;
        _device = device;
        _queue = queue;
        _surface = surface;

        _sampler = device.CreateSampler(
            new VkSamplerCreateInfo
            {
                MagFilter = VkFilter.Nearest, MinFilter = VkFilter.Nearest,
                AddressModeU = VkSamplerAddressMode.ClampToEdge, AddressModeV = VkSamplerAddressMode.ClampToEdge, AddressModeW = VkSamplerAddressMode.ClampToEdge,
            },
            null);

        var binding = new VkDescriptorSetLayoutBinding
        {
            Binding = 0, DescriptorType = VkDescriptorType.CombinedImageSampler, DescriptorCount = 1, StageFlags = VkShaderStageFlags.FragmentBit,
        };
        _setLayout = device.CreateDescriptorSetLayout(new VkDescriptorSetLayoutCreateInfo { BindingCount = 1, Bindings = &binding }, null);
        _pipelineLayout = device.CreatePipelineLayout([_setLayout], [], null);

        // Shaders: one triangle that covers the screen, sampling Gum's image.
        using var compiler = ShaderC.CreateCompiler();
        using var options = ShaderC.CreateCompilerOptions();
        VkShaderModule Compile(ReadOnlySpan<byte> glsl, ShaderKind kind)
        {
            using var spirv = compiler.Compile(glsl, kind, "shader"u8, "main"u8, options);
            return ShaderModuleExtensions.FromSpirv(device, new ReadOnlySpan<byte>(spirv.GetBytes(), (int)spirv.GetLength()), null);
        }
        _stages = new ShaderStages(device,
        [
            Presets.ShaderStage.Vertex(Compile("""
                #version 450
                layout(location = 0) out vec2 uv;
                void main() {
                    vec2 p = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
                    uv = p;
                    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
                }
                """u8, ShaderKind.VertexShader)),
            Presets.ShaderStage.Fragment(Compile("""
                #version 450
                layout(location = 0) in vec2 uv;
                layout(set = 0, binding = 0) uniform sampler2D tex;
                layout(location = 0) out vec4 outColor;
                void main() { outColor = texture(tex, uv); }
                """u8, ShaderKind.FragmentShader)),
        ], null);

        _commandPool = device.CreateCommandPool(new VkCommandPoolCreateInfo { QueueFamilyIndex = queueFamily, Flags = VkCommandPoolCreateFlags.ResetCommandBufferBit }, null);
        _commandBuffer = device.AllocateCommandBuffer(new VkCommandBufferAllocateInfo { CommandPool = _commandPool, Level = VkCommandBufferLevel.Primary });
        _imageAvailable = device.CreateSemaphore(new VkSemaphoreCreateInfo(), null);
        _frameFence = device.CreateFence(VkFenceCreateFlags.SignaledBit, null);
    }

    /// <summary>
    /// (Re)builds everything that depends on the window size or on Gum's image: the swapchain at
    /// <paramref name="width"/> by <paramref name="height"/> (the surface's current extent wins when it has one),
    /// and the descriptor that samples <paramref name="source"/>.
    /// </summary>
    public void Configure(int width, int height, VkImage source, VkFormat sourceFormat)
    {
        _device.WaitIdle();
        DestroySizeDependent();

        var capabilities = _physicalDevice.GetSurfaceCapabilitiesKHR(_surface);
        _extent = capabilities.CurrentExtent;
        if (_extent.Width == -1 && _extent.Height == -1)
        {
            _extent = new VkExtent2D(
                Math.Clamp(width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
                Math.Clamp(height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height));
        }

        var imageCount = Math.Max(capabilities.MinImageCount, 2);
        if (capabilities.MaxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, capabilities.MaxImageCount);
        }

        VkCompositeAlphaFlagsKHR[] alphaModes =
        [
            VkCompositeAlphaFlagsKHR.OpaqueBitKHR,
            VkCompositeAlphaFlagsKHR.PreMultipliedBitKHR,
            VkCompositeAlphaFlagsKHR.PostMultipliedBitKHR,
            VkCompositeAlphaFlagsKHR.InheritBitKHR,
        ];
        var compositeAlpha = alphaModes.First(mode => (capabilities.SupportedCompositeAlpha & mode) != 0);

        // Gum's image holds sRGB bytes in a UNORM format. Writing them to a UNORM swapchain as they are
        // keeps the colors right; an sRGB swapchain would encode them a second time.
        var surfaceFormats = new VkSurfaceFormatKHR[_physicalDevice.GetSurfaceFormatsKHR_Count(_surface)];
        _physicalDevice.GetSurfaceFormatsKHR_Write(_surface, surfaceFormats);
        var surfaceFormat = surfaceFormats[0];
        foreach (var candidate in surfaceFormats)
        {
            if (candidate.Format is VkFormat.B8G8R8A8UNorm or VkFormat.R8G8B8A8UNorm)
            {
                surfaceFormat = candidate;
                break;
            }
        }

        _swapchain = _device.CreateSwapchainKHR(
            new VkSwapchainCreateInfoKHR
            {
                Surface = _surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = _extent,
                ImageArrayLayers = 1,
                ImageUsage = VkImageUsageFlags.ColorAttachmentBit,
                ImageSharingMode = VkSharingMode.Exclusive,
                PreTransform = capabilities.CurrentTransform,
                CompositeAlpha = compositeAlpha,
                PresentMode = VkPresentModeKHR.FifoKHR,
                Clipped = true,
            },
            null);

        _images = new VkImage[_device.GetSwapchainImagesKHR_Count(_swapchain)];
        _device.GetSwapchainImagesKHR_Write(_swapchain, _images);
        _imageViews = new VkImageView[_images.Length];
        for (int i = 0; i < _images.Length; i++)
        {
            _imageViews[i] = _device.CreateImageView(
                new VkImageViewCreateInfo
                {
                    Image = _images[i], ViewType = VkImageViewType._2D, Format = surfaceFormat.Format, SubresourceRange = _colorRange,
                },
                null);
        }
        _renderFinished = _images.Select(_ => _device.CreateSemaphore(new VkSemaphoreCreateInfo(), null)).ToArray();

        // The Gum image, as a combined image sampler in descriptor set 0.
        _sourceView = _device.CreateImageView(
            new VkImageViewCreateInfo { Image = source, ViewType = VkImageViewType._2D, Format = sourceFormat, SubresourceRange = _colorRange },
            null);
        var poolSize = new VkDescriptorPoolSize { Type = VkDescriptorType.CombinedImageSampler, DescriptorCount = 1 };
        _pool = _device.CreateDescriptorPool(new VkDescriptorPoolCreateInfo { MaxSets = 1, PoolSizeCount = 1, PoolSizes = &poolSize }, null);
        var setLayoutLocal = _setLayout;
        _device.AllocateDescriptorSets(new VkDescriptorSetAllocateInfo { DescriptorPool = _pool, DescriptorSetCount = 1, SetLayouts = &setLayoutLocal }, _sets);
        var imageInfo = new VkDescriptorImageInfo { Sampler = _sampler, ImageView = _sourceView, ImageLayout = VkImageLayout.ShaderReadOnlyOptimal };
        _device.UpdateDescriptorSets(
            [new VkWriteDescriptorSet { DstSet = _sets[0], DstBinding = 0, DescriptorCount = 1, DescriptorType = VkDescriptorType.CombinedImageSampler, ImageInfo = &imageInfo }],
            []);

        var vertexInput = new VertexInput(0, 0);
        var viewport = new VkViewport { Width = _extent.Width, Height = _extent.Height, MaxDepth = 1 };
        var scissor = new VkRect2D(_extent);
        var viewportState = new VkPipelineViewportStateCreateInfo { ViewportCount = 1, Viewports = &viewport, ScissorCount = 1, Scissors = &scissor };
        var format = surfaceFormat.Format;
        var rendering = new VkPipelineRenderingCreateInfo { ColorAttachmentCount = 1, ColorAttachmentFormats = &format };
        _pipeline = _device.CreateGraphicsPipeline(VkPipelineCache.NullHandle, new VkGraphicsPipelineCreateInfo
        {
            Next = &rendering,
            StageCount = _stages.Count,
            Stages = _stages,
            VertexInputState = vertexInput,
            InputAssemblyState = Presets.InputAssembly.TriangleList,
            ViewportState = &viewportState,
            RasterizationState = Presets.Rasterization.Fill.CullNone,
            MultisampleState = Presets.Multisample.None,
            ColorBlendState = Presets.BlendState.BlendNone,
            Layout = _pipelineLayout,
        }, null);

        _configured = true;
    }

    /// <summary>Draws the Gum image to the next swapchain image and presents it.</summary>
    public void Present()
    {
        if (!_configured)
        {
            return;
        }

        _device.WaitForFence(_frameFence, -1);
        _device.ResetFences([_frameFence]);

        _device.AcquireNextImageKHR(_swapchain, -1, _imageAvailable, VkFence.NullHandle, out var imageIndex);
        var image = _images[imageIndex];

        _commandBuffer.Reset();
        _commandBuffer.Begin(VkCommandBufferUsageFlags.OneTimeSubmitBit);
        Transition(image, VkImageLayout.Undefined, VkImageLayout.ColorAttachmentOptimal,
            VkPipelineStageFlags.ColorAttachmentOutputBit, VkPipelineStageFlags.ColorAttachmentOutputBit,
            VkAccessFlags.None, VkAccessFlags.ColorAttachmentWriteBit);

        var color = new VkRenderingAttachmentInfo
        {
            ImageView = _imageViews[imageIndex], ImageLayout = VkImageLayout.ColorAttachmentOptimal,
            LoadOp = VkAttachmentLoadOp.Clear, StoreOp = VkAttachmentStoreOp.Store,
            ClearValue = new VkClearColorValue { Float32 = new Brutal.Numerics.float4(0, 0, 0, 1) },
        };
        _commandBuffer.BeginRendering(new VkRenderingInfo { RenderArea = new VkRect2D(_extent), LayerCount = 1, ColorAttachmentCount = 1, ColorAttachments = &color });
        _commandBuffer.BindPipeline(VkPipelineBindPoint.Graphics, _pipeline);
        _commandBuffer.BindDescriptorSets(VkPipelineBindPoint.Graphics, _pipelineLayout, 0, _sets, []);
        _commandBuffer.Draw(3, 1, 0, 0);
        _commandBuffer.EndRendering();

        Transition(image, VkImageLayout.ColorAttachmentOptimal, VkImageLayout.PresentSrcKHR,
            VkPipelineStageFlags.ColorAttachmentOutputBit, VkPipelineStageFlags.BottomOfPipeBit,
            VkAccessFlags.ColorAttachmentWriteBit, VkAccessFlags.None);
        _commandBuffer.End();

        var signal = _renderFinished[imageIndex];
        _queue.Submit([_imageAvailable], [VkPipelineStageFlags.ColorAttachmentOutputBit], [_commandBuffer], [signal], _frameFence);
        var swapchain = _swapchain;
        _queue.PresentKHR(new VkPresentInfoKHR
        {
            WaitSemaphoreCount = 1, WaitSemaphores = &signal, SwapchainCount = 1, Swapchains = &swapchain, ImageIndices = &imageIndex,
        });
    }

    private void Transition(
        VkImage image, VkImageLayout from, VkImageLayout to,
        VkPipelineStageFlags srcStage, VkPipelineStageFlags dstStage, VkAccessFlags srcAccess, VkAccessFlags dstAccess) =>
        _commandBuffer.PipelineBarrier(srcStage, dstStage, VkDependencyFlags.None, [], [],
        [
            new VkImageMemoryBarrier
            {
                SrcAccessMask = srcAccess, DstAccessMask = dstAccess, OldLayout = from, NewLayout = to,
                SrcQueueFamilyIndex = VK.QUEUE_FAMILY_IGNORED, DstQueueFamilyIndex = VK.QUEUE_FAMILY_IGNORED,
                Image = image, SubresourceRange = _colorRange,
            },
        ]);

    // Callers wait for the device to be idle first.
    private void DestroySizeDependent()
    {
        if (!_configured)
        {
            return;
        }

        _configured = false;
        _device.DestroyPipeline(_pipeline, null);
        _device.DestroyDescriptorPool(_pool, null);
        _device.DestroyImageView(_sourceView, null);
        foreach (var semaphore in _renderFinished)
        {
            _device.DestroySemaphore(semaphore, null);
        }
        foreach (var view in _imageViews)
        {
            _device.DestroyImageView(view, null);
        }
        _device.DestroySwapchainKHR(_swapchain, null);
    }

    public void Dispose()
    {
        _device.WaitIdle();
        DestroySizeDependent();
        _device.DestroyFence(_frameFence, null);
        _device.DestroySemaphore(_imageAvailable, null);
        _device.DestroyCommandPool(_commandPool, null);
        _device.DestroyPipelineLayout(_pipelineLayout, null);
        _stages.Dispose();
        _device.DestroyDescriptorSetLayout(_setLayout, null);
        _device.DestroySampler(_sampler, null);
    }
}
