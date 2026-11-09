#nullable enable

using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using SkiaGameRendering;
using SkiaGameRendering.Core.VK;
using SkiaSharp;

namespace Gum;

/// <summary><see cref="IBrutalTarget"/> over a SkiaGameRendering.Brutal Vulkan render target.</summary>
internal sealed class VulkanTarget : IBrutalTarget
{
    private readonly SkiaRenderTarget2D _target;

    public VulkanTarget(SkiaVulkanContext context, int width, int height) =>
        _target = new SkiaRenderTarget2D(context, width, height);

    public SKCanvas Canvas => _target.Canvas;

    public VkImage Image => new VkImage((nint)_target.VkImage);

    public VkFormat Format => (VkFormat)_target.VkFormat;

    public void Begin() => _target.Begin();

    public void End() => _target.End();

    public void EndWithoutDrawing() => _target.End();

    public void Dispose() => _target.Dispose();
}

/// <summary>Owns the SkiaVulkanContext over the game's Vulkan objects and, unless turned off, the swapchain presenter.</summary>
internal sealed class VulkanBackend : IBrutalGpuBackend
{
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly Queue _queue;
    private readonly int _queueFamily;
    private readonly VkSurfaceKHR _surface;
    private readonly SkiaVulkanContext _context;
    private VulkanPresenter? _presenter;
    private VulkanTarget? _presentedTarget;
    private int _presentedWidth;
    private int _presentedHeight;

    public VulkanBackend(Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily, VkSurfaceKHR surface)
    {
        _physicalDevice = physicalDevice;
        _device = device;
        _queue = queue;
        _queueFamily = queueFamily;
        _surface = surface;
        _context = new SkiaVulkanContext(instance.VkHandle, physicalDevice.VkHandle, device.VkHandle, queue.VkHandle, (uint)queueFamily);
    }

    /// <summary>When false, <see cref="Present"/> leaves the image for the game's own renderer.</summary>
    public bool PresentToWindow { get; set; } = true;

    public IBrutalTarget CreateTarget(int width, int height) => new VulkanTarget(_context, width, height);

    public void Present(IBrutalTarget target, int width, int height)
    {
        if (!PresentToWindow)
        {
            return;
        }

        VulkanTarget vulkanTarget = (VulkanTarget)target;
        _presenter ??= new VulkanPresenter(_physicalDevice, _device, _queue, _queueFamily, _surface);
        if (!ReferenceEquals(_presentedTarget, vulkanTarget) || _presentedWidth != width || _presentedHeight != height)
        {
            _presenter.Configure(width, height, vulkanTarget.Image, vulkanTarget.Format);
            _presentedTarget = vulkanTarget;
            _presentedWidth = width;
            _presentedHeight = height;
        }
        _presenter.Present();
    }

    public void Dispose()
    {
        _presenter?.Dispose();
        _presenter = null;
        _presentedTarget = null;
        _context.Dispose();
    }
}
