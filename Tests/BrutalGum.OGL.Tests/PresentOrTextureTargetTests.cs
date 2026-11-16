using Gum;
using Shouldly;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>Gum.Brutal.Vulkan's choice between drawing into the swapchain and into its own texture, driven by fakes.</summary>
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

    [Fact]
    public void Begin_PresentingAgainAfterCompositing_FreesTheTextureAfterTheGpuWait()
    {
        bool presentToWindow = true;
        bool frame = false;
        List<string> events = new();
        FakeDirectPass direct = new();
        List<FakeBrutalTarget> textures = new();
        PresentOrTextureTarget target = new(
            () => presentToWindow,
            () => { bool f = frame; frame = false; return f; },
            direct,
            () => { FakeBrutalTarget texture = new(8, 8); textures.Add(texture); return texture; },
            () => events.Add($"wait, texture disposed: {textures.Count > 0 && textures[0].Disposed}"),
            8, 8);

        void Pass() { frame = true; target.Begin(); target.End(); }

        Pass();
        target.Texture.ShouldBeNull();
        direct.Begins.ShouldBe(1);

        presentToWindow = false;
        Pass();
        target.Texture.ShouldBeSameAs(textures[0]);

        presentToWindow = true;
        Pass();

        target.IsDirectPass.ShouldBeTrue();
        target.Texture.ShouldBeNull();
        textures[0].Disposed.ShouldBeTrue();
        events.ShouldBe(new[] { "wait, texture disposed: False" });
    }
}
