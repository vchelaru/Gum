using System;
using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// ColorTextureAlpha's fog pass swaps the sprite's blend for a cached copy (#7837). Drawing a
/// ColorTextureAlpha sprite and then a Modulate sprite every frame switches between the copy and the
/// original on each frame, and must allocate nothing once warm.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ColorTextureAlphaAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public ColorTextureAlphaAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ColorTextureAlphaThenModulateSprite_AllocatesNothingPerFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        using Texture2D texture = new(gd, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.White);
        texture.SetData(pixels);

        ContainerRuntime root = new();
        root.Width = 64;
        root.Height = 32;
        root.AddChild(CreateSprite(texture, x: 0, ColorOperation.ColorTextureAlpha));
        root.AddChild(CreateSprite(texture, x: 32, ColorOperation.Modulate));
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D scene = new(gd, 64, 32, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);
        try
        {
            AllocationResult result = AllocationMeasurer.MeasureMinimum(
                () =>
                {
                    gd.SetRenderTarget(scene);
                    gd.Clear(Color.Black);
                    managers.Renderer.Draw(managers);
                    gd.SetRenderTarget(null);
                },
                attempts: 3,
                warmupIterations: 30,
                measuredIterations: 200);

            _output.WriteLine($"ColorTextureAlpha + Modulate sprite: {result.BytesPerIteration:N0} bytes/frame");

            // Liveness: the fog pass ran with the swapped copy (color source One), and the Modulate sprite
            // with the original blend (color source SourceAlpha), so both Begins were measured.
            BeginParameters[] states = managers.Renderer.SpriteRenderer.LastFrameDrawStates.ToArray();
            states.Any(state => state.BlendState?.ColorSourceBlend == global::Gum.Blend.One).ShouldBeTrue();
            states.Any(state => state.BlendState?.ColorSourceBlend == global::Gum.Blend.SourceAlpha).ShouldBeTrue();

            result.BytesPerIteration.ShouldBeLessThanOrEqualTo(0, $"measured {result.BytesPerIteration:N0} bytes/frame");
        }
        finally
        {
            root.RemoveFromManagers();
        }
    }

    private static SpriteRuntime CreateSprite(Texture2D texture, float x, ColorOperation colorOperation)
    {
        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 16;
        sprite.Height = 16;
        sprite.X = x;
        sprite.Color = new Color(255, 128, 0);
        sprite.Alpha = 128;
        sprite.ColorOperation = colorOperation;
        return sprite;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public global::Gum.GumService GumService { get; } = new global::Gum.GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
