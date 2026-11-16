#nullable enable

using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using SkiaGameRendering;
using SkiaGameRendering.Core.VK;
using SkiaSharp;
using System;

namespace Gum;

/// <summary>Gum's own image: <see cref="IBrutalTarget"/> over a SkiaGameRendering.Brutal Vulkan render target.</summary>
internal sealed class VulkanTextureTarget : IBrutalTarget
{
    private readonly SkiaRenderTarget2D _target;

    public VulkanTextureTarget(SkiaVulkanContext context, int width, int height) =>
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
    , IBrutalDirectPass
{
    private const uint ImageLayoutColorAttachmentOptimal = 2;
    private const uint ImageLayoutPresentSrcKhr = 1000001002;

    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly Queue _queue;
    private readonly int _queueFamily;
    private readonly VkSurfaceKHR _surface;
    private readonly SkiaVulkanContext _context;
    private VulkanPresenter? _presenter;
    private VulkanTextureTarget? _presentedTexture;
    private int _presentedWidth;
    private int _presentedHeight;
    private bool _framePending;
    private SkiaDirectCanvas? _direct;
    private bool _directConfigured;

    private readonly Func<IDisposable>? _acquireQueueLock;

    // acquireQueueLock is the host's queue lock. It goes to Skia (its submits) and the presenter (its submit and
    // present); the device wait and the disposals are locked by QueueLockedBackend, which wraps this backend.
    public VulkanBackend(
        Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily, VkSurfaceKHR surface,
        Func<IDisposable>? acquireQueueLock = null)
    {
        _acquireQueueLock = acquireQueueLock;
        _physicalDevice = physicalDevice;
        _device = device;
        _queue = queue;
        _queueFamily = queueFamily;
        _surface = surface;
        _context = new SkiaVulkanContext(instance.VkHandle, physicalDevice.VkHandle, device.VkHandle, queue.VkHandle, (uint)queueFamily, acquireQueueLock);
    }

    /// <summary>When false, <see cref="Present"/> leaves the image for the game's own renderer.</summary>
    public bool PresentToWindow { get; set; } = true;

    /// <summary>The swapchain image last presented, its format and size, or null; for a test that reads the window's pixels back.</summary>
    public (VkImage Image, VkFormat Format, int Width, int Height)? LastPresented =>
        _presenter?.LastPresentedImage is VkImage image
            ? (image, _presenter.SwapchainFormat, _presenter.SwapchainSize.Width, _presenter.SwapchainSize.Height)
            : null;

    public void WaitForGpu() => _device.WaitIdle();

    public void BeginFrame() => _framePending = true;

    /// <summary>True when Draw announced this Begin with <see cref="BeginFrame"/>; clears the flag.</summary>
    internal bool TakeFrame()
    {
        bool pending = _framePending;
        _framePending = false;
        return pending;
    }

    public IBrutalTarget CreateTarget(int width, int height) =>
        new PresentOrTextureTarget(
            () => PresentToWindow, TakeFrame,
            this,
            () => new VulkanTextureTarget(_context, width, height),
            WaitForGpuLocked, width, height);

    // Called from inside a pass, where Gum holds no queue lock of its own; vkDeviceWaitIdle needs every queue synchronized.
    private void WaitForGpuLocked()
    {
        using (_acquireQueueLock?.Invoke())
        {
            _device.WaitIdle();
        }
    }

    private VulkanPresenter EnsurePresenter() =>
        _presenter ??= new VulkanPresenter(
            _physicalDevice, _device, _queue, _queueFamily, _surface, _acquireQueueLock
            , releaseSwapchainImageUsers: () => _direct?.ReleaseCachedSurfaces()
            );

    SKCanvas IBrutalDirectPass.Canvas => _direct!.Canvas;

    /// <summary>Acquires the next swapchain image (rebuilding the swapchain on a size change) and begins drawing into it.</summary>
    void IBrutalDirectPass.Begin(int width, int height)
    {
        VulkanPresenter presenter = EnsurePresenter();
        _direct ??= new SkiaDirectCanvas(_context, cacheSurfaces: true);
        if (!_directConfigured || _presentedWidth != width || _presentedHeight != height)
        {
            presenter.Configure(width, height, null, default);
            _directConfigured = true;
            _presentedTexture = null;
            _presentedWidth = width;
            _presentedHeight = height;
        }
        VkImage image = presenter.AcquireForDirect();
        (int swapchainWidth, int swapchainHeight) = presenter.SwapchainSize;
        // Transparent, like Gum's own image starts each pass; the opaque window shows it as black.
        _direct.Begin(
            (ulong)(nint)image.VkHandle, (uint)presenter.SwapchainFormat, swapchainWidth, swapchainHeight,
            ImageLayoutColorAttachmentOptimal, clear: true, imageUsage: (uint)presenter.SwapchainUsage);
    }

    void IBrutalDirectPass.End() => _direct!.End(finalLayout: ImageLayoutPresentSrcKhr);

    public void Present(IBrutalTarget target, int width, int height)
    {
        if (!PresentToWindow)
        {
            return;
        }

        PresentOrTextureTarget switching = (PresentOrTextureTarget)target;
        VulkanPresenter presenter = EnsurePresenter();
        if (switching.IsDirectPass)
        {
            presenter.PresentDirect();
            return;
        }
        _directConfigured = false;
        VulkanTextureTarget texture = (VulkanTextureTarget)switching.Texture!;
        if (!ReferenceEquals(_presentedTexture, texture) || _presentedWidth != width || _presentedHeight != height)
        {
            presenter.Configure(width, height, texture.Image, texture.Format);
            _presentedTexture = texture;
            _presentedWidth = width;
            _presentedHeight = height;
        }
        presenter.Present();
    }

    public void Dispose()
    {
        // The presenter releases the direct canvas's cached surfaces before it destroys the swapchain.
        _presenter?.Dispose();
        _presenter = null;
        _presentedTexture = null;
        _direct?.Dispose();
        _direct = null;
        _context.Dispose();
    }
}
