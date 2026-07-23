using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins how a <see cref="SpriteRuntime"/> that references a render-target container via
/// <see cref="Sprite.RenderTargetTextureSource"/> renders the baked content: differing sprite
/// size, multiple sprites sharing one source, an explicit sub-region, size derivation, and the
/// unbaked-source case. Guards the push→pull convergence (issue #3986).
/// </summary>
public class RenderTargetTextureSourceSpriteTests : BaseTestClass
{
    [Fact]
    public void SpriteReferencingUnbakedSource_DrawsNothing()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Not a render target and invisible → nothing bakes and it never composites itself.
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 64;
        container.Height = 64;
        container.Visible = false;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        Color sampled = SampleCapturePixel(gd, renderer, managers, 32, 32);
        sampled.R.ShouldBeLessThan((byte)50);
    }

    [Fact]
    public void SpriteSmallerThanSource_DrawsAtSpriteRect()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.Visible = false;
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 40;
        sprite.Height = 40;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        Color inside = SampleCapturePixel(gd, renderer, managers, 10, 10);
        inside.R.ShouldBeGreaterThan((byte)150);
        ((int)inside.R - inside.G).ShouldBeGreaterThan(80);

        // The sprite is 40x40 — the source is 64x64. If placement used the source/texture size the
        // sprite would fill 64x64 and this pixel would be red.
        Color outside = SampleCapturePixel(gd, renderer, managers, 55, 55);
        outside.R.ShouldBeLessThan((byte)50);
    }

    [Fact]
    public void TwoSpritesReferencingSameSource_BothRenderBakedContent()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.Visible = false;
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        SpriteRuntime spriteLeft = new();
        spriteLeft.X = 0;
        spriteLeft.Y = 0;
        spriteLeft.Width = 64;
        spriteLeft.Height = 64;
        spriteLeft.RenderTargetTextureSource = container;
        spriteLeft.AddToManagers(managers, null);
        spriteLeft.UpdateLayout();

        SpriteRuntime spriteRight = new();
        spriteRight.X = 64;
        spriteRight.Y = 0;
        spriteRight.Width = 64;
        spriteRight.Height = 64;
        spriteRight.RenderTargetTextureSource = container;
        spriteRight.AddToManagers(managers, null);
        spriteRight.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        Color leftSample = SampleCapturePixel(gd, renderer, managers, 16, 16);
        leftSample.R.ShouldBeGreaterThan((byte)150);
        ((int)leftSample.R - leftSample.G).ShouldBeGreaterThan(80);

        Color rightSample = SampleCapturePixel(gd, renderer, managers, 80, 16);
        rightSample.R.ShouldBeGreaterThan((byte)150);
        ((int)rightSample.R - rightSample.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void SpriteWithSourceRectangle_SamplesSubRegionOfBakedTarget()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Baked target: red left half, green right half.
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;
        container.Visible = false;

#pragma warning disable CS0618
        ColoredRectangleRuntime redLeft = new();
        ColoredRectangleRuntime greenRight = new();
#pragma warning restore CS0618
        redLeft.X = 0;
        redLeft.Width = 32;
        redLeft.Height = 64;
        redLeft.Color = Color.Red;
        container.AddChild(redLeft);
        greenRight.X = 32;
        greenRight.Width = 32;
        greenRight.Height = 64;
        greenRight.Color = Color.Green;
        container.AddChild(greenRight);
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        // Only the left (red) half of the baked target, stretched over the whole 64x64 sprite.
        sprite.SourceRectangle = new Rectangle(0, 0, 32, 64);
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        // The selected sub-region (left, red) is what the sprite samples.
        Color redRegion = SampleCapturePixel(gd, renderer, managers, 10, 32);
        redRegion.R.ShouldBeGreaterThan((byte)150);
        ((int)redRegion.R - redRegion.G).ShouldBeGreaterThan(80);

        // The green right half is excluded by the source rectangle — if it were ignored the whole
        // red|green texture would draw and this pixel would read green.
        Color excludedRegion = SampleCapturePixel(gd, renderer, managers, 48, 32);
        ((int)excludedRegion.G - excludedRegion.R).ShouldBeLessThan(80);
    }

    [Fact]
    public void SpriteReferencingRenderTarget_ResolvesBakedTargetViaRendererPull()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.Visible = false;
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso cacheOwner = (IRenderableIpso)container.RenderableComponent;

        SpriteRuntime sprite = new();
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        // Pull model: the sprite resolves the baked target from the renderer at draw time; the
        // renderer never pushes a Texture onto the sprite.
        renderer.TryGetBakedRenderTargetFor(cacheOwner).ShouldNotBeNull();
    }

    [Fact]
    public void SpriteTextureSize_DerivesFromRenderTargetSource()
    {
        Sprite sprite = new(null);
        InvisibleRenderable source = new();
        source.Width = 80;
        source.Height = 40;

        sprite.RenderTargetTextureSource = source;

        sprite.TextureWidth.ShouldBe(80f);
        sprite.TextureHeight.ShouldBe(40f);
        ((IAspectRatio)sprite).AspectRatio.ShouldBe(2f);
    }

    private static double _hostTime;

    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0;
        managers.Activity(hostTime);
    }

    private static ContainerRuntime CreateRedRenderTargetContainer()
    {
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);

        return container;
    }

    private static Color SampleCapturePixel(GraphicsDevice gd, Renderer renderer, SystemManagers managers, int x, int y)
    {
        const int w = 128;
        const int h = 128;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(capture);
        gd.Clear(Color.Black);
        renderer.Draw(managers);
        gd.SetRenderTarget(null);

        Color[] pixels = new Color[w * h];
        capture.GetData(pixels);
        return pixels[y * w + x];
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
