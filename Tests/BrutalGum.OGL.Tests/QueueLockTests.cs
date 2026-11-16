using Gum;
using MonoGameGum.TestsCommon;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;
using Xunit.Abstractions;

namespace BrutalGum.Tests;

/// <summary>
/// The host's queue lock (Gum.Brutal.Vulkan's acquireQueueLock). Vulkan needs a VkQueue externally
/// synchronized, so Gum takes the host's lock around the queue work it does itself: the device wait before a
/// resize, and freeing the targets and the backend. Skia's own submits take it inside SkiaGameRendering,
/// which a CPU fake cannot show. The lock must never be taken while already held, because a host's lock
/// may not be re-entrant, and must cost nothing per frame.
/// </summary>
public class QueueLockTests : BaseTestClass
{
    private sealed class FakeQueueLock : IDisposable
    {
        public int Acquired { get; private set; }
        public int Released { get; private set; }
        public int MaxDepth { get; private set; }
        public bool Held => Acquired > Released;

        // One cached disposable, as a host that wants a free draw loop would hand back.
        public Func<IDisposable> Acquire => AcquireCore;

        private IDisposable AcquireCore()
        {
            Acquired++;
            MaxDepth = Math.Max(MaxDepth, Acquired - Released);
            return this;
        }

        void IDisposable.Dispose() => Released++;
    }

    private sealed class ThrowingShape : SkiaGum.Renderables.RenderableShapeBase
    {
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation) =>
            throw new InvalidOperationException("draw failed");
    }

    private readonly ITestOutputHelper _output;

    public QueueLockTests(ITestOutputHelper output) => _output = output;

    private static void RestoreCanvasInitialization()
    {
        GumService.Default.Uninitialize();
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, TestAssemblyInitialize.Input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
        SkiaHostTestBase.CaptureRenderableBaseline();
        Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
    }

    private static void WithLockedBackend(
        FakeBrutalInput input, Action<FakeBrutalGpuBackend, FakeQueueLock> body)
    {
        FakeQueueLock queueLock = new FakeQueueLock();
        FakeBrutalGpuBackend backend = new FakeBrutalGpuBackend { IsLockHeld = () => queueLock.Held };
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(input, QueueLockedBackend.Wrap(backend, queueLock.Acquire));
            body(backend, queueLock);
        }
        finally
        {
            RestoreCanvasInitialization();
        }
    }

    [Fact]
    public void Wrap_WithoutALock_ReturnsTheBackendItself()
    {
        FakeBrutalGpuBackend backend = new FakeBrutalGpuBackend();

        QueueLockedBackend.Wrap(backend, null).ShouldBeSameAs(backend);
    }

    [Fact]
    public void Initialize_TakesNoLockOfItsOwn()
    {
        // Creating the first target runs SkiaGameRendering's own Begin/End, which take the lock themselves.
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithLockedBackend(input, (backend, queueLock) =>
        {
            queueLock.Acquired.ShouldBe(0);
            backend.Targets.Count.ShouldBe(1);
        });
    }

    [Fact]
    public void Draw_WithoutAResize_TakesNoLockOfItsOwnAndPresentsTheBackendsOwnTarget()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithLockedBackend(input, (backend, queueLock) =>
        {
            GumService.Default.Draw();
            GumService.Default.Draw();

            queueLock.Acquired.ShouldBe(0);
            backend.Presents.Count.ShouldBe(2);
            backend.Presents[0].Target.ShouldBeSameAs(backend.Targets[0]);
            backend.Targets[0].EndCount.ShouldBe(2);
        });
    }

    [Fact]
    public void Draw_AfterAResize_WaitsForTheGpuAndFreesTheOldTargetInsideTheLock()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithLockedBackend(input, (backend, queueLock) =>
        {
            GumService.Default.Draw();
            input.FramebufferWidth = 640;
            input.FramebufferHeight = 360;

            GumService.Default.Draw();

            backend.WaitForGpuLockStates.ShouldBe(new[] { true });
            backend.Targets[0].DisposedWhileLocked.ShouldBe(true);
            queueLock.Acquired.ShouldBe(2);
            queueLock.Released.ShouldBe(2);
            queueLock.MaxDepth.ShouldBe(1);
        });
    }

    [Fact]
    public void Uninitialize_FreesTheTargetAndTheBackendInsideTheLock_AndLeavesItReleased()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };
        FakeQueueLock queueLock = new FakeQueueLock();
        FakeBrutalGpuBackend backend = new FakeBrutalGpuBackend { IsLockHeld = () => queueLock.Held };
        GumService.Default.Uninitialize();
        GumService.Default.Initialize(input, QueueLockedBackend.Wrap(backend, queueLock.Acquire));
        GumService.Default.Draw();

        GumService.Default.Uninitialize();

        backend.Targets[0].DisposedWhileLocked.ShouldBe(true);
        backend.DisposedWhileLocked.ShouldBe(true);
        queueLock.Acquired.ShouldBe(2);
        queueLock.Released.ShouldBe(2);
        queueLock.MaxDepth.ShouldBe(1);
        queueLock.Held.ShouldBeFalse();

        RestoreCanvasInitialization();
    }

    [Fact]
    public void Draw_WhenDrawingThrows_EndsTheTargetSoTheHostLockIsReleased()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };
        FakeQueueLock queueLock = new FakeQueueLock();
        FakeBrutalGpuBackend backend = new FakeBrutalGpuBackend
        {
            // What SkiaGameRendering does: Begin takes the host lock and End releases it.
            OnTargetBegin = () => queueLock.Acquire(),
            OnTargetEnd = () => ((IDisposable)queueLock).Dispose(),
        };
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(input, QueueLockedBackend.Wrap(backend, queueLock.Acquire));
            Gum.Wireframe.GraphicalUiElement element = new Gum.Wireframe.GraphicalUiElement(new ThrowingShape());
            element.Width = 50;
            element.Height = 50;
            element.AddToRoot();

            Should.Throw<InvalidOperationException>(() => GumService.Default.Draw());

            queueLock.Held.ShouldBeFalse();
            backend.Presents.ShouldBeEmpty();
        }
        finally
        {
            GumService.Default.Root.Children.Clear();
            RestoreCanvasInitialization();
        }
    }

    [Fact]
    public void Draw_WithACachedLockDisposable_AllocatesNothing()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        GumService.Default.Uninitialize();
        GumService.Default.Initialize(input, new FakeBrutalGpuBackend());
        long unlocked = AllocationMeasurer.MeasureMinimum(() => GumService.Default.Draw(), 3, 100, 500).TotalBytes;

        FakeQueueLock queueLock = new FakeQueueLock();
        GumService.Default.Uninitialize();
        GumService.Default.Initialize(input, QueueLockedBackend.Wrap(new FakeBrutalGpuBackend(), queueLock.Acquire));
        long locked = AllocationMeasurer.MeasureMinimum(() => GumService.Default.Draw(), 3, 100, 500).TotalBytes;

        _output.WriteLine($"Draw allocation over 500 frames: no lock {unlocked} bytes, cached lock {locked} bytes");
        unlocked.ShouldBe(0);
        locked.ShouldBe(0);
        queueLock.Acquired.ShouldBe(0);

        RestoreCanvasInitialization();
    }
}
