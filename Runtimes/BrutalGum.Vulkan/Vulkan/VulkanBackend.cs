#nullable enable

using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using SkiaGameRendering;
using SkiaGameRendering.Core.VK;
using SkiaSharp;
using System;

namespace Gum;

/// <summary>
/// <see cref="IBrutalTarget"/> for Vulkan. While Gum presents (and the build has BRUTAL_VULKAN_DIRECT), a drawn pass
/// goes straight into the swapchain image through the backend's <c>SkiaDirectCanvas</c> and no image of Gum's own
/// exists. Otherwise a pass draws into a SkiaGameRendering.Brutal render target, created at the first such pass.
/// </summary>
internal sealed class VulkanTarget : IBrutalTarget
{
    private readonly VulkanBackend _backend;
    private readonly SkiaVulkanContext _context;
    private readonly int _width;
    private readonly int _height;
    private SkiaRenderTarget2D? _texture;
    private bool _capturePass;

    public VulkanTarget(VulkanBackend backend, SkiaVulkanContext context, int width, int height)
    {
        _backend = backend;
        _context = context;
        _width = width;
        _height = height;
    }

    /// <summary>True when the current or last pass drew into the swapchain image.</summary>
    public bool IsDirectPass { get; private set; }

    // A capture pass while Gum presents has no image to draw into; Gum gets its canvas at the first drawn pass.
    public SKCanvas Canvas => IsDirectPass ? _backend.DirectCanvas : _capturePass ? null! : _texture!.Canvas;

    /// <summary>Gum's own image, or null while Gum draws straight into the swapchain (or before its first pass).</summary>
    public VkImage? Image => _texture is null ? null : new VkImage((nint)_texture.VkImage);

    public VkFormat? Format => _texture is null ? null : (VkFormat)_texture.VkFormat;

    public void Begin()
    {
        bool isFrame = _backend.TakeFrame();
        IsDirectPass = false;
        _capturePass = false;
#if BRUTAL_VULKAN_DIRECT
        if (_backend.PresentToWindow)
        {
            if (isFrame)
            {
                _backend.BeginDirect(_width, _height);
                IsDirectPass = true;
            }
            else
            {
                // The holder's capture pass at create time: acquiring a swapchain image here would leave it unpresented.
                _capturePass = true;
            }
            return;
        }
#endif
        _texture ??= new SkiaRenderTarget2D(_context, _width, _height);
        _texture.Begin();
    }

    public void End()
    {
        if (IsDirectPass)
        {
            _backend.EndDirect();
        }
        else if (!_capturePass)
        {
            _texture!.End();
        }
    }

    public void EndWithoutDrawing() => End();

    public void Dispose() => _texture?.Dispose();
}

/// <summary>Owns the SkiaVulkanContext over the game's Vulkan objects and, unless turned off, the swapchain presenter.</summary>
internal sealed class VulkanBackend : IBrutalGpuBackend
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
    private VulkanTarget? _presentedTarget;
    private int _presentedWidth;
    private int _presentedHeight;
    private bool _framePending;
#if BRUTAL_VULKAN_DIRECT
    private SkiaDirectCanvas? _direct;
    private bool _directConfigured;
#endif

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

    public IBrutalTarget CreateTarget(int width, int height) => new VulkanTarget(this, _context, width, height);

    private VulkanPresenter EnsurePresenter() =>
        _presenter ??= new VulkanPresenter(
            _physicalDevice, _device, _queue, _queueFamily, _surface, _acquireQueueLock
#if BRUTAL_VULKAN_DIRECT
            , releaseSwapchainImageUsers: () => _direct?.ReleaseCachedSurfaces()
#endif
            );

#if BRUTAL_VULKAN_DIRECT
    internal SKCanvas DirectCanvas => _direct!.Canvas;

    /// <summary>Acquires the next swapchain image (rebuilding the swapchain on a size change) and begins drawing into it.</summary>
    internal void BeginDirect(int width, int height)
    {
        VulkanPresenter presenter = EnsurePresenter();
        _direct ??= new SkiaDirectCanvas(_context, cacheSurfaces: true);
        if (!_directConfigured || _presentedWidth != width || _presentedHeight != height)
        {
            presenter.Configure(width, height, null, default);
            _directConfigured = true;
            _presentedTarget = null;
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

    internal void EndDirect() => _direct!.End(finalLayout: ImageLayoutPresentSrcKhr);
#else
    internal SKCanvas DirectCanvas => throw new InvalidOperationException("Built without BRUTAL_VULKAN_DIRECT.");

    internal void EndDirect() => throw new InvalidOperationException("Built without BRUTAL_VULKAN_DIRECT.");
#endif

    public void Present(IBrutalTarget target, int width, int height)
    {
        if (!PresentToWindow)
        {
            return;
        }

        VulkanTarget vulkanTarget = (VulkanTarget)target;
        VulkanPresenter presenter = EnsurePresenter();
#if BRUTAL_VULKAN_DIRECT
        if (vulkanTarget.IsDirectPass)
        {
            presenter.PresentDirect();
            return;
        }
        _directConfigured = false;
#endif
        if (!ReferenceEquals(_presentedTarget, vulkanTarget) || _presentedWidth != width || _presentedHeight != height)
        {
            presenter.Configure(width, height, vulkanTarget.Image, vulkanTarget.Format ?? default);
            _presentedTarget = vulkanTarget;
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
        _presentedTarget = null;
#if BRUTAL_VULKAN_DIRECT
        _direct?.Dispose();
        _direct = null;
#endif
        _context.Dispose();
    }
}
