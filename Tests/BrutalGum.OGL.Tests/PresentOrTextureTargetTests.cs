using Gum;
using Shouldly;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>Gum.Brutal.Vulkan's choice between drawing into the swapchain and into its own textures, driven by fakes.</summary>
public class PresentOrTextureTargetTests
{
    private sealed class FakeDirectPass : IBrutalDirectPass
    {
        private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(8, 8));
        public int Begins { get; private set; }
        public SKCanvas Canvas => _surface.Canvas;
        public void Begin(int width, int height) => Begins++;
        public void End() { }
    }

    /// <summary>A target over fakes, driven as Gum's Draw drives it. The test sets how it presents and how many textures it keeps.</summary>
    private sealed class TargetDriver
    {
        private bool _frame;

        public TargetDriver()
        {
            Direct = new FakeDirectPass();
            Textures = new List<FakeBrutalTarget>();
            Events = new List<string>();
            Target = new PresentOrTextureTarget(
                () => PresentToWindow,
                () => ImageCount,
                () => { bool frame = _frame; _frame = false; return frame; },
                Direct,
                () => { FakeBrutalTarget texture = new(8, 8); Textures.Add(texture); return texture; },
                () => Events.Add("wait, disposed: " + string.Join(",", Textures.Select(texture => texture.Disposed))),
                8, 8);
        }

        public bool PresentToWindow { get; set; }
        public int ImageCount { get; set; } = 1;
        public FakeDirectPass Direct { get; }
        public List<FakeBrutalTarget> Textures { get; }
        public List<string> Events { get; }
        public PresentOrTextureTarget Target { get; }

        /// <summary>A pass Draw announced, as each frame's.</summary>
        public void FramePass()
        {
            _frame = true;
            Target.Begin();
            Target.End();
        }

        /// <summary>The pass the target holder makes when it creates a target, to capture its canvas.</summary>
        public void CapturePass()
        {
            Target.Begin();
            Target.EndWithoutDrawing();
        }
    }

    [Fact]
    public void Begin_CapturePass_UsesTheCurrentTextureWithoutAdvancing()
    {
        TargetDriver driver = new() { ImageCount = 3 };

        driver.CapturePass();
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[0]);

        driver.FramePass();
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[0]);
        driver.FramePass();
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[1]);
        driver.Textures.Count.ShouldBe(2);
    }

    [Fact]
    public void Begin_FewerImages_FreesTheExtraTexturesAfterTheGpuWait()
    {
        TargetDriver driver = new() { ImageCount = 3 };
        driver.FramePass();
        driver.FramePass();
        driver.FramePass();

        driver.ImageCount = 1;
        driver.FramePass();

        driver.Events.ShouldBe(new[] { "wait, disposed: False,False,False" });
        driver.Textures[0].Disposed.ShouldBeFalse();
        driver.Textures[1].Disposed.ShouldBeTrue();
        driver.Textures[2].Disposed.ShouldBeTrue();
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[0]);
        driver.FramePass();
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[0]);
        driver.Textures.Count.ShouldBe(3);
    }

    [Fact]
    public void Begin_MoreImages_MovesOnFromTheTextureDrawnLast()
    {
        TargetDriver driver = new() { ImageCount = 1 };
        driver.FramePass();
        driver.FramePass();

        driver.ImageCount = 3;
        driver.FramePass();

        driver.Textures.Count.ShouldBe(2);
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[1]);
        driver.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Begin_OneImage_DrawsEveryFrameIntoTheSameTexture()
    {
        TargetDriver driver = new() { ImageCount = 1 };

        driver.FramePass();
        driver.FramePass();
        driver.FramePass();

        driver.Textures.Count.ShouldBe(1);
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[0]);
        driver.Textures[0].BeginCount.ShouldBe(3);
        driver.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Begin_PresentingAgainAfterCompositing_FreesEveryTextureAfterTheGpuWait()
    {
        TargetDriver driver = new() { PresentToWindow = true, ImageCount = 2 };

        driver.FramePass();
        driver.Target.Texture.ShouldBeNull();
        driver.Direct.Begins.ShouldBe(1);

        driver.PresentToWindow = false;
        driver.FramePass();
        driver.FramePass();
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[1]);

        driver.PresentToWindow = true;
        driver.FramePass();

        driver.Target.IsDirectPass.ShouldBeTrue();
        driver.Target.Texture.ShouldBeNull();
        driver.Textures.ShouldAllBe(texture => texture.Disposed);
        driver.Events.ShouldBe(new[] { "wait, disposed: False,False" });

        driver.PresentToWindow = false;
        driver.FramePass();
        driver.Textures.Count.ShouldBe(3);
        driver.Target.Texture.ShouldBeSameAs(driver.Textures[2]);
    }

    [Fact]
    public void Begin_ThreeImages_DrawsIntoEachInTurnAndBackToTheFirst()
    {
        TargetDriver driver = new() { ImageCount = 3 };
        List<IBrutalTarget?> drawn = new();

        for (int i = 0; i < 4; i++)
        {
            driver.FramePass();
            drawn.Add(driver.Target.Texture);
        }

        driver.Textures.Count.ShouldBe(3);
        drawn.ShouldBe(new IBrutalTarget?[] { driver.Textures[0], driver.Textures[1], driver.Textures[2], driver.Textures[0] });
        driver.Textures.Select(texture => texture.BeginCount).ShouldBe(new[] { 2, 1, 1 });
        driver.Events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PresentOrTextureTarget.MaxImageCount + 1)]
    public void CheckImageCount_OutOfRange_Throws(int count)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PresentOrTextureTarget.CheckImageCount(count));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(PresentOrTextureTarget.MaxImageCount)]
    public void CheckImageCount_InRange_DoesNotThrow(int count)
    {
        Should.NotThrow(() => PresentOrTextureTarget.CheckImageCount(count));
    }
}
