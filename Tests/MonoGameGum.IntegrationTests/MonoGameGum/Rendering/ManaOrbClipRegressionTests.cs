using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Reproduces issue #4091: the GameUiSamples <c>HollowKnightComponents/ManaOrb</c> component's
/// <c>WaveMaskSprite</c> uses <see cref="Gum.RenderingLibrary.Blend.MinAlpha"/> to mask the
/// <c>WaveTop</c>/<c>ColoredRectangleInstance</c> wave content to the orb's circular bounds inside
/// the <c>RenderTargetContainer</c> bake. <c>MinAlpha</c> deliberately leaves color untouched and
/// only clips alpha (<c>ColorSourceBlend=Zero, ColorDestinationBlend=One</c>), so after masking, a
/// clipped pixel's leftover color is still premultiplied against its OLD (pre-mask) alpha rather
/// than its new (zero) alpha. <see cref="Renderer.DrawRenderTargetToScreen"/> composites the baked
/// texture back with a premultiplied blend (<c>BlendState.AlphaBlend</c>) whenever the container's
/// own blend is unconfigured (#1696), which bleeds that leftover color through instead of treating
/// the masked pixel as transparent.
///
/// Loads the actual sample project/component (not a hand-built synthetic scene) so the exact
/// codegen/state-application path the sample uses is exercised.
/// </summary>
public class ManaOrbClipRegressionTests : BaseTestClass
{
    [Fact]
    public void WaveContent_MaskedByMinAlpha_DoesNotLeakPastOrbBounds()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        var elementSave = ObjectFinder.Self.GetElementSave("HollowKnightComponents/ManaOrb");
        elementSave.ShouldNotBeNull();

        GraphicalUiElement manaOrb = elementSave!.ToGraphicalUiElement(managers, addToManagers: true);
        manaOrb.X = 50;
        manaOrb.Y = 50;
        manaOrb.UpdateLayout();

        // Mirrors ManaOrb.CustomInitialize()/PercentFull=50 (GameUiSamples/Components/HollowKnightComponents/ManaOrb.cs),
        // which the real sample runs on construction but which the raw ElementSave.ToGraphicalUiElement
        // path does not (no Forms wrapper => no CustomInitialize).
        var emptyState = manaOrb.ElementSave.AllStates.First(item => item.Name == "Empty");
        var fullState = manaOrb.ElementSave.AllStates.First(item => item.Name == "Full");
        manaOrb.InterpolateBetween(emptyState, fullState, 0.5f);
        manaOrb.UpdateLayout();

        // Two warm-up draws: SpriteRenderer.CurrentZoom (used to snap the bake camera to whole
        // pixels in RenderToRenderTarget) is only populated once a real BeginSpriteBatch cycle has
        // run, which happens during the main compositing pass after baking — not during the bake
        // pass itself. The first draw bakes with an uninitialized zoom; later draws re-bake
        // correctly. See NestedRenderTargetTextureSourceTests for the same warm-up pattern.
        renderer.Draw(managers);
        renderer.Draw(managers);

        // (52,148) is a point on the WaveMaskSprite's own mask texture (local (2,98) inside the
        // 100x100 RenderTargetContainer, which sits at absolute (50,50)) where the mask's alpha is
        // 0 (outside the circle) but WaveTop/ColoredRectangleInstance still paint non-transparent
        // color there before the mask draws — exactly the leftover-color-at-zero-alpha shape the
        // premultiplied composite-back blit mishandles.
        Color sampled = SampleMainLayerPixel(gd, renderer, managers, sampleX: 52, sampleY: 148);

        Color background = Color.CornflowerBlue;
        System.Math.Abs(sampled.R - background.R).ShouldBeLessThan(10);
        System.Math.Abs(sampled.G - background.G).ShouldBeLessThan(10);
        System.Math.Abs(sampled.B - background.B).ShouldBeLessThan(10);
    }

    // #5707: the test above only asserts that a pixel OUTSIDE the orb is transparent, so a sample
    // that renders nothing at all passes it. This pins the other half: the orb's own content must
    // be visible inside the circle.
    [Fact]
    public void ManaOrb_HalfFull_DrawsItsContentInsideTheOrb()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        var elementSave = ObjectFinder.Self.GetElementSave("HollowKnightComponents/ManaOrb");
        elementSave.ShouldNotBeNull();

        GraphicalUiElement manaOrb = elementSave!.ToGraphicalUiElement(managers, addToManagers: true);
        manaOrb.X = 50;
        manaOrb.Y = 50;
        manaOrb.UpdateLayout();

        var emptyState = manaOrb.ElementSave!.AllStates.First(item => item.Name == "Empty");
        var fullState = manaOrb.ElementSave!.AllStates.First(item => item.Name == "Full");
        manaOrb.InterpolateBetween(emptyState, fullState, 0.5f);
        manaOrb.UpdateLayout();

        renderer.Draw(managers);
        renderer.Draw(managers);

        // The orb is the 100x100 component at absolute (50,50); (100,130) is inside the circle, in
        // the lower (filled) half at 50%.
        Color sampled = SampleMainLayerPixel(gd, renderer, managers, sampleX: 100, sampleY: 130);

        Color background = Color.CornflowerBlue;
        int difference = System.Math.Abs(sampled.R - background.R)
            + System.Math.Abs(sampled.G - background.G)
            + System.Math.Abs(sampled.B - background.B);
        difference.ShouldBeGreaterThan(60);
    }

    // #5707: the sample draws the orb as a component nested inside the HollowKnightHudScreen. Inflating
    // the whole screen from its ElementSave (rather than the orb alone, as the tests above do) is the
    // path that stopped drawing the orb after #4666 batched layout during inflation.
    [Fact]
    public void ManaOrb_NestedInHudScreen_DrawsItsContent()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        var screenSave = ObjectFinder.Self.GetElementSave("HollowKnightHudScreen");
        screenSave.ShouldNotBeNull();

        // Mirrors ManaOrb.Generated.cs RegisterRuntimeType: the sample instantiates the orb through a
        // registered factory that calls SetGraphicalUiElement directly, nested inside the screen's
        // own inflation.
        var orbElement = ObjectFinder.Self.GetElementSave("HollowKnightComponents/ManaOrb");
        orbElement.ShouldNotBeNull();
        ElementSaveExtensions.RegisterGueInstantiation("HollowKnightComponents/ManaOrb", () =>
        {
            var visual = new Gum.GueDeriving.ContainerRuntime();
            orbElement!.SetGraphicalUiElement(visual, managers);

            // ManaOrb's Forms wrapper runs CustomInitialize (PercentFull = 50) as it is created,
            // which interpolates between the Empty and Full states while the screen's inflation is
            // still in progress.
            var emptyState = orbElement!.AllStates.First(item => item.Name == "Empty");
            var fullState = orbElement!.AllStates.First(item => item.Name == "Full");
            visual.InterpolateBetween(emptyState, fullState, 0.5f);
            return visual;
        });

        GraphicalUiElement screen = screenSave!.ToGraphicalUiElement(managers, addToManagers: true);
        screen.UpdateLayout();


        renderer.Draw(managers);
        renderer.Draw(managers);

        const int w = 400;
        const int h = 300;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(Color.CornflowerBlue);
        renderer.Draw(managers);
        gd.SetRenderTarget(null);
        Color[] pixels = new Color[w * h];
        capture.GetData(pixels);

        // In the sample the orb occupies x 25..125, y 27..127 of the HUD. Count pixels there that
        // differ from the empty HUD background sampled in the clear area beside the buttons.
        Color background = pixels[(150 * w) + 300];
        int drawn = 0;
        for (int y = 27; y < 127; y++)
        {
            for (int x = 25; x < 125; x++)
            {
                Color pixel = pixels[(y * w) + x];
                int difference = System.Math.Abs(pixel.R - background.R)
                    + System.Math.Abs(pixel.G - background.G)
                    + System.Math.Abs(pixel.B - background.B);
                if (difference > 24)
                {
                    drawn++;
                }
            }
        }

        drawn.ShouldBeGreaterThan(1000);
    }

    private static Color SampleMainLayerPixel(GraphicsDevice gd, Renderer renderer, SystemManagers managers, int sampleX, int sampleY)
    {
        const int w = 300;
        const int h = 300;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(capture);
        gd.Clear(Color.CornflowerBlue);
        renderer.Draw(managers);
        gd.SetRenderTarget(null);

        Color[] pixels = new Color[w * h];
        capture.GetData(pixels);
        return pixels[(sampleY * w) + sampleX];
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test, loading the
    /// real GameUiSamples project so <see cref="ObjectFinder.Self"/> resolves the actual
    /// <c>ManaOrb</c> component exactly as the sample app does.
    /// </summary>
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
            GumService.Initialize(this, FindGameUiSamplesGumProjectFile());
        }

        // Walks up from the test binary's output directory to the repo root, then anchors on the
        // real GameUiSamples project file — reproducing #4091 requires the actual sample
        // component (state application, codegen instantiation), not a hand-built synthetic scene.
        private static string FindGameUiSamplesGumProjectFile()
        {
            string current = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                string candidate = Path.Combine(
                    current, "Samples", "GameUiSamples", "Content", "GumProject", "GameUiSamplesGumProject.gumx");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                string? parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || parent == current)
                {
                    break;
                }
                current = parent;
            }
            throw new InvalidOperationException("could not locate GameUiSamples project from " + AppContext.BaseDirectory);
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
