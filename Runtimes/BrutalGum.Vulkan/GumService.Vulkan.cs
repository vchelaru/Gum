#if BRUTAL
#nullable enable

using Brutal.GlfwApi;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Gum.Forms;
using System;
using Gum.Input;

namespace Gum;

public partial class GumService
{
    private VulkanBackend? VulkanBackend => QueueLockedBackend.Unwrap(_backend) as VulkanBackend;

    /// <summary>
    /// Initializes Gum for a BRUTAL application drawing with Vulkan 1.3, optionally loading a Gum project.
    /// Gum creates its Skia context on the objects you pass, a render target at the framebuffer size
    /// (recreated when it changes; make the window resizable if you want Gum to follow it), and, while
    /// <see cref="PresentToWindow"/> is true, a swapchain over <paramref name="surface"/> that <see cref="Draw"/>
    /// presents to. Gum never destroys the objects you pass.
    /// </summary>
    /// <param name="window">The GLFW window input is read from.</param>
    /// <param name="instance">The Vulkan instance.</param>
    /// <param name="physicalDevice">The physical device behind <paramref name="device"/>.</param>
    /// <param name="device">The logical device, created with dynamic rendering (Vulkan 1.3) and, to present, the swapchain extension.</param>
    /// <param name="queue">A graphics queue. Skia, and the presenter, submit to it.</param>
    /// <param name="queueFamily">The family index <paramref name="queue"/> belongs to. It must be able to present to <paramref name="surface"/> when presenting.</param>
    /// <param name="surface">The surface of <paramref name="window"/>. May be default when <see cref="PresentToWindow"/> is set to false before the first <see cref="Draw"/>.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    /// <param name="acquireQueueLock">Takes your queue lock; see the <see cref="IBrutalInput"/> overload.</param>
    public void Initialize(
        GlfwWindow window, Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily,
        VkSurfaceKHR surface, string? gumProjectFile = null, Func<IDisposable>? acquireQueueLock = null) =>
        Initialize(new BrutalGlfwInput(window), instance, physicalDevice, device, queue, queueFamily, surface, gumProjectFile, acquireQueueLock);

    /// <summary>
    /// Initializes Gum like the overload without <paramref name="visualsVersion"/>, with an explicit set of default Forms
    /// visuals. Skia supports <see cref="DefaultVisualsVersion.V3"/> only; any other value throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="window">The GLFW window input is read from.</param>
    /// <param name="instance">The Vulkan instance.</param>
    /// <param name="physicalDevice">The physical device behind <paramref name="device"/>.</param>
    /// <param name="device">The logical device, created with dynamic rendering (Vulkan 1.3) and, to present, the swapchain extension.</param>
    /// <param name="queue">A graphics queue. Skia, and the presenter, submit to it.</param>
    /// <param name="queueFamily">The family index <paramref name="queue"/> belongs to.</param>
    /// <param name="surface">The surface of <paramref name="window"/>.</param>
    /// <param name="visualsVersion">The default Forms visuals to register.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    /// <param name="acquireQueueLock">Takes your queue lock; see the <see cref="IBrutalInput"/> overload.</param>
    public void Initialize(
        GlfwWindow window, Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily,
        VkSurfaceKHR surface, DefaultVisualsVersion visualsVersion, string? gumProjectFile = null,
        Func<IDisposable>? acquireQueueLock = null) =>
        Initialize(new BrutalGlfwInput(window), instance, physicalDevice, device, queue, queueFamily, surface, visualsVersion, gumProjectFile, acquireQueueLock);

    /// <summary>
    /// Initializes Gum for a BRUTAL application drawing with Vulkan 1.3 whose window and events are not
    /// GLFW's (SDL3, a custom loop). The host supplies cursor, keyboard, clipboard and gamepad state through
    /// <paramref name="input"/>; the Vulkan objects behave as in the <see cref="GlfwWindow"/> overload.
    /// </summary>
    /// <param name="input">The host's window, mouse, keyboard, clipboard and gamepad state.</param>
    /// <param name="instance">The Vulkan instance.</param>
    /// <param name="physicalDevice">The physical device behind <paramref name="device"/>.</param>
    /// <param name="device">The logical device, created with dynamic rendering (Vulkan 1.3) and, to present, the swapchain extension.</param>
    /// <param name="queue">A graphics queue. Skia, and the presenter, submit to it.</param>
    /// <param name="queueFamily">The family index <paramref name="queue"/> belongs to. It must be able to present to <paramref name="surface"/> when presenting.</param>
    /// <param name="surface">The surface of the host's window. May be default when <see cref="PresentToWindow"/> is set to false before the first <see cref="Draw"/>.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    /// <param name="acquireQueueLock">
    /// Optional. Vulkan needs a <c>VkQueue</c> externally synchronized, and Gum submits to <paramref name="queue"/>.
    /// If your game also submits to it (or waits on the device) from another thread, pass a function that takes
    /// the lock you use for those calls and returns an <see cref="IDisposable"/> that releases it. Gum calls it
    /// around its own queue work and never takes it while already holding it, so a non-re-entrant lock is fine.
    /// Return a cached disposable to keep the draw loop free of allocations. Leave it null for a game that only
    /// touches the queue from the thread that calls Gum.
    /// </param>
    public void Initialize(
        IBrutalInput input, Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily,
        VkSurfaceKHR surface, string? gumProjectFile = null, Func<IDisposable>? acquireQueueLock = null) =>
        Initialize(input, CreateBackend(instance, physicalDevice, device, queue, queueFamily, surface, acquireQueueLock), gumProjectFile);

    /// <summary>
    /// Initializes Gum like the <see cref="IBrutalInput"/> overload without <paramref name="visualsVersion"/>, with an explicit
    /// set of default Forms visuals. Skia supports <see cref="DefaultVisualsVersion.V3"/> only; any other value throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="input">The host's window, mouse, keyboard, clipboard and gamepad state.</param>
    /// <param name="instance">The Vulkan instance.</param>
    /// <param name="physicalDevice">The physical device behind <paramref name="device"/>.</param>
    /// <param name="device">The logical device, created with dynamic rendering (Vulkan 1.3) and, to present, the swapchain extension.</param>
    /// <param name="queue">A graphics queue. Skia, and the presenter, submit to it.</param>
    /// <param name="queueFamily">The family index <paramref name="queue"/> belongs to.</param>
    /// <param name="surface">The surface of the host's window.</param>
    /// <param name="visualsVersion">The default Forms visuals to register.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    /// <param name="acquireQueueLock">Takes your queue lock; see the overload without <paramref name="visualsVersion"/>.</param>
    public void Initialize(
        IBrutalInput input, Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily,
        VkSurfaceKHR surface, DefaultVisualsVersion visualsVersion, string? gumProjectFile = null,
        Func<IDisposable>? acquireQueueLock = null) =>
        Initialize(input, CreateBackend(instance, physicalDevice, device, queue, queueFamily, surface, acquireQueueLock), gumProjectFile, visualsVersion);

    private static IBrutalGpuBackend CreateBackend(
        Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, int queueFamily,
        VkSurfaceKHR surface, Func<IDisposable>? acquireQueueLock) =>
        QueueLockedBackend.Wrap(
            new VulkanBackend(instance, physicalDevice, device, queue, queueFamily, surface, acquireQueueLock),
            acquireQueueLock);

    /// <summary>
    /// True (the default) to have <see cref="Draw"/> present Gum's image to the window. Set false when your
    /// own renderer puts it on screen: Draw then only renders into <see cref="RenderTargetImage"/>. Set it right
    /// after Initialize, before the first Draw.
    /// </summary>
    public bool PresentToWindow
    {
        get => VulkanBackend?.PresentToWindow ?? true;
        set
        {
            if (VulkanBackend is VulkanBackend backend)
            {
                backend.PresentToWindow = value;
            }
        }
    }

    /// <summary>
    /// Gum's frame, for a game with its own renderer (<see cref="PresentToWindow"/> false). Read it again after every
    /// <see cref="Draw"/>: the image is replaced when the framebuffer size changes. Contract: after Draw returns
    /// the image is in SHADER_READ_ONLY_OPTIMAL on the queue passed to Initialize (Skia's Begin and End record the
    /// transitions and submit on that queue). Sample it from a later submission on that same queue; from another
    /// queue, add your own synchronization. Do not write to it, and do not keep views of it across a resize. The
    /// bytes are sRGB in a UNORM format (<see cref="RenderTargetFormat"/>): a pass that writes to an sRGB target
    /// must not decode them again. Null before Initialize.
    /// </summary>
    public VkImage? RenderTargetImage => ((QueueLockedBackend.Unwrap(_targetHolder?.Target) as PresentOrTextureTarget)?.Texture as VulkanTextureTarget)?.Image;

    // Read by reflection from the sample's --capture mode, which proves a frame reached the window by reading
    // the swapchain image back: { handle, VkFormat, width, height }, or null before the first present.
    internal long[]? LastPresentedForTest =>
        VulkanBackend?.LastPresented is { } presented
            ? new long[] { (long)(nint)presented.Image.VkHandle, (long)presented.Format, presented.Width, presented.Height }
            : null;

    /// <summary>The format of <see cref="RenderTargetImage"/>.</summary>
    public VkFormat? RenderTargetFormat => ((QueueLockedBackend.Unwrap(_targetHolder?.Target) as PresentOrTextureTarget)?.Texture as VulkanTextureTarget)?.Format;
}
#endif
