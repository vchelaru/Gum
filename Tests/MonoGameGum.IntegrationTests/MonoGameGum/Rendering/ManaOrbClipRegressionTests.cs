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
