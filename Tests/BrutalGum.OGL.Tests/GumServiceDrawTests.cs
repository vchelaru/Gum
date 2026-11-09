using Gum;
using Gum.Wireframe;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>
/// The one-call flow the game sees: Initialize, Update, Draw. Gum creates the target, resizes it with the
/// framebuffer and presents it, with a fake backend standing in for OpenGL or Vulkan.
/// </summary>
public class GumServiceDrawTests : BaseTestClass
{
    private static void WithBackend(FakeBrutalInput input, Action<FakeBrutalGpuBackend> body)
    {
        FakeBrutalGpuBackend backend = new FakeBrutalGpuBackend();
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(input, backend);
            body(backend);
        }
        finally
        {
            GumService.Default.Uninitialize();
            SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
            GumService.Default.Initialize(surface.Canvas, TestAssemblyInitialize.Input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
            SkiaHostTestBase.CaptureRenderableBaseline();
            Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
        }
    }

    [Fact]
    public void Initialize_CreatesTheTargetAtTheFramebufferSize()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 640, FramebufferHeight = 480 };

        WithBackend(input, backend =>
        {
            backend.Targets.Count.ShouldBe(1);
            backend.Targets[0].Width.ShouldBe(640);
            backend.Targets[0].Height.ShouldBe(480);
            GumService.Default.IsInitialized.ShouldBeTrue();
        });
    }

    [Fact]
    public void Draw_TakesNoCanvas_BeginsEndsAndPresentsTheTargetOnce()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithBackend(input, backend =>
        {
            int beginsBefore = backend.Targets[0].BeginCount;

            GumService.Default.Draw();

            backend.Targets[0].BeginCount.ShouldBe(beginsBefore + 1);
            backend.Targets[0].EndCount.ShouldBe(1);
            backend.Presents.ShouldBe(new[] { (backend.Targets[0], 320, 200) });
        });
    }

    [Fact]
    public void Draw_AfterTheFramebufferSizeChanges_RecreatesTheTargetAndResizesGum()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithBackend(input, backend =>
        {
            GumService.Default.Draw();
            input.FramebufferWidth = 640;
            input.FramebufferHeight = 360;

            GumService.Default.Draw();

            backend.Targets.Count.ShouldBe(2);
            backend.Targets[0].Disposed.ShouldBeTrue();
            backend.Targets[1].Width.ShouldBe(640);
            backend.Targets[1].Height.ShouldBe(360);
            backend.Presents[^1].ShouldBe((backend.Targets[1], 640, 360));
            GraphicalUiElement.CanvasWidth.ShouldBe(640);
            GraphicalUiElement.CanvasHeight.ShouldBe(360);
            // The old target is freed only after the GPU finished the frame that was sampling it.
            backend.WaitForGpuCount.ShouldBe(1);
        });
    }

    [Fact]
    public void Draw_WithAnUnchangedSize_KeepsTheTarget()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithBackend(input, backend =>
        {
            GumService.Default.Draw();
            GumService.Default.Draw();

            backend.Targets.Count.ShouldBe(1);
            backend.Presents.Count.ShouldBe(2);
            backend.WaitForGpuCount.ShouldBe(0);
        });
    }

    [Fact]
    public void Draw_WithAnEmptyFramebuffer_DoesNothing()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 320, FramebufferHeight = 200 };

        WithBackend(input, backend =>
        {
            input.FramebufferWidth = 0;
            input.FramebufferHeight = 0;

            GumService.Default.Draw();

            backend.Presents.ShouldBeEmpty();
            backend.Targets.Count.ShouldBe(1);
        });
    }

    [Fact]
    public void Draw_ClearsTheTargetToTheClearColor()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 64, FramebufferHeight = 64 };

        WithBackend(input, backend =>
        {
            GumService.Default.ClearColor = new SKColor(255, 0, 0);
            try
            {
                GumService.Default.Draw();

                backend.Targets[0].PixelAt(10, 10).ShouldBe(new SKColor(255, 0, 0));
            }
            finally
            {
                GumService.Default.ClearColor = SKColors.Transparent;
            }
        });
    }

    [Fact]
    public void Uninitialize_DisposesTheTargetAndTheBackend()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 64, FramebufferHeight = 64 };

        WithBackend(input, backend =>
        {
            GumService.Default.Uninitialize();

            backend.Disposed.ShouldBeTrue();
            backend.Targets[0].Disposed.ShouldBeTrue();
        });
    }
}
