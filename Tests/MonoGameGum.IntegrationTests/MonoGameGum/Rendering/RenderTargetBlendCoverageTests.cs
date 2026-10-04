using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms;
using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using GumBlend = Gum.RenderingLibrary.Blend;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pixel pins for the render-target bake blends (#5675, #5676): a textured Sprite mask (the
/// structural blend match through the Sprite's XNA round-trip), SubtractAlpha as a destination-out
/// when destination alpha is below 1 or masks stack, SubtractAlpha in a mixed shape batch, Normal and
/// Additive shapes inside a bake, and SubtractAlpha outside a render target.
/// </summary>
public class RenderTargetBlendCoverageTests : BaseTestClass
{
    private const int CaptureSize = 100;
    private const int Tolerance = 8;
    private static readonly XnaColor BackdropColor = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor FillColor = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);
    private const int HalfAlpha = 128;

    private static float Mix(int fill, int backdrop, float fillWeight) =>
        fill * fillWeight + backdrop * (1 - fillWeight);

    private static void AssertNear(XnaColor actual, float r, float g, float b)
    {
        Math.Abs(actual.R - r).ShouldBeLessThanOrEqualTo(Tolerance);
        Math.Abs(actual.G - g).ShouldBeLessThanOrEqualTo(Tolerance);
        Math.Abs(actual.B - b).ShouldBeLessThanOrEqualTo(Tolerance);
    }

    private static void AssertNear(XnaColor actual, XnaColor expected) =>
        AssertNear(actual, expected.R, expected.G, expected.B);

    private static void AssertMix(XnaColor actual, float fillWeight) =>
        AssertNear(actual,
            Mix(FillColor.R, BackdropColor.R, fillWeight),
            Mix(FillColor.G, BackdropColor.G, fillWeight),
            Mix(FillColor.B, BackdropColor.B, fillWeight));

    private static XnaColor At(XnaColor[] pixels, int x, int y) => pixels[(y * CaptureSize) + x];

    // ---- (a) textured Sprite as the mask ----

    [Theory]
    [InlineData(GumBlend.SubtractAlpha)]
    [InlineData(GumBlend.ReplaceAlpha)]
    [InlineData(GumBlend.MinAlpha)]
    public void TexturedSpriteMask_InRenderTarget_MixesFillAndBackdrop(GumBlend blend)
    {
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddFill(rt, FillColor);
            rt.AddChild(NewTexturedMask(gd, HalfAlpha, blend));
        });

        // 50% mask: SubtractAlpha leaves 50% of the fill, ReplaceAlpha writes 50%, MinAlpha clamps
        // the opaque fill to 50%. All three show an even mix inside the mask, plain fill outside.
        AssertMix(At(pixels, 50, 50), HalfAlpha / 255f);
        AssertNear(At(pixels, 5, 5), FillColor);
    }

    [Fact]
    public void TexturedSpriteSubtractAlpha_OpaqueMask_InRenderTarget_ShowsBackdropThroughHole()
    {
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddFill(rt, FillColor);
            rt.AddChild(NewTexturedMask(gd, 255, GumBlend.SubtractAlpha));
        });

        AssertNear(At(pixels, 50, 50), BackdropColor);
        AssertNear(At(pixels, 5, 5), FillColor);
    }

    // ---- (b) SubtractAlpha is a destination-out: dest * (1 - srcAlpha) ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubtractAlpha_TwoStackedHalfMasks_LeaveQuarterOfFill(bool useCircle)
    {
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddFill(rt, FillColor);
            AddSubtractMask(rt, useCircle, HalfAlpha);
            AddSubtractMask(rt, useCircle, HalfAlpha);
        });

        // The old math (dest - src on alpha) cleared the second half too and showed only the backdrop.
        AssertMix(At(pixels, 50, 50), 0.25f);
        AssertNear(At(pixels, 5, 5), FillColor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubtractAlpha_HalfMask_OverHalfAlphaDestination_LeavesQuarterOfFill(bool useCircle)
    {
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            // Destination alpha 0.5 in the bake: dest * (1 - 0.5) = 0.25. Old math (0.5 - 0.5) = 0.
            AddFill(rt, new XnaColor(FillColor.R, FillColor.G, FillColor.B, (byte)HalfAlpha));
            AddSubtractMask(rt, useCircle, HalfAlpha);
        });

        AssertMix(At(pixels, 50, 50), 0.25f);
        AssertMix(At(pixels, 5, 5), HalfAlpha / 255f);
    }

    // ---- (c) mixed batch, Normal / Additive shapes in a bake, SubtractAlpha outside a bake ----

    [Fact]
    public void SubtractAlphaCircle_AfterNormalCircle_InRenderTarget_CutsOnlyTheMaskArea()
    {
        XnaColor green = new XnaColor((byte)30, (byte)200, (byte)30, (byte)255);
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddFill(rt, FillColor);
            rt.AddChild(NewCircle(0, 0, CaptureSize, green));
            CircleRuntime mask = NewCircle(35, 35, 30, XnaColor.White);
            mask.Blend = GumBlend.SubtractAlpha;
            rt.AddChild(mask);
        });

        AssertNear(At(pixels, 50, 50), BackdropColor);
        AssertNear(At(pixels, 50, 25), green);
    }

    [Fact]
    public void NormalCircle_InRenderTarget_DrawsItsOwnColorOverFill()
    {
        XnaColor circleColor = new XnaColor((byte)30, (byte)200, (byte)30, (byte)255);
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddFill(rt, FillColor);
            rt.AddChild(NewCircle(20, 20, 60, circleColor));
        });

        AssertNear(At(pixels, 50, 50), circleColor);
        AssertNear(At(pixels, 5, 5), FillColor);
    }

    [Fact]
    public void AdditiveCircle_InRenderTarget_AddsToFill()
    {
        XnaColor circleColor = new XnaColor((byte)30, (byte)60, (byte)100, (byte)255);
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddFill(rt, FillColor);
            CircleRuntime circle = NewCircle(20, 20, 60, circleColor);
            circle.Blend = GumBlend.Additive;
            rt.AddChild(circle);
        });

        AssertNear(At(pixels, 50, 50), FillColor.R + circleColor.R, FillColor.G + circleColor.G, FillColor.B + circleColor.B);
        AssertNear(At(pixels, 5, 5), FillColor);
    }

    // Outside a render target SubtractAlpha keeps its original math (dest alpha - src alpha, color
    // untouched), so the capture's color channels stay at the backdrop while alpha goes to 0. The bake
    // swap would instead zero the color.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubtractAlpha_OutsideRenderTarget_KeepsColorAndLowersAlpha(bool useCircle)
    {
        XnaColor[] pixels = Render((root, rt, gd) =>
        {
            AddSubtractMask(root, useCircle, 255);
        }, useRenderTarget: false);

        XnaColor inside = At(pixels, 50, 50);
        AssertNear(inside, BackdropColor);
        inside.A.ShouldBeLessThanOrEqualTo((byte)Tolerance);
        AssertNear(At(pixels, 5, 5), BackdropColor);
        At(pixels, 5, 5).A.ShouldBe((byte)255);
    }

    // ---- helpers ----

    private static SpriteRuntime NewTexturedMask(GraphicsDevice gd, int alpha, GumBlend blend)
    {
        Texture2D texture = new(gd, 4, 4);
        XnaColor[] white = new XnaColor[16];
        Array.Fill(white, XnaColor.White);
        texture.SetData(white);

        SpriteRuntime mask = new();
        mask.Texture = texture;
        mask.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        mask.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        mask.X = 20;
        mask.Y = 20;
        mask.Width = 60;
        mask.Height = 60;
        mask.Alpha = alpha;
        mask.Blend = blend;
        return mask;
    }

    private static void AddFill(ContainerRuntime parent, XnaColor color)
    {
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        ColoredRectangleRuntime fill = new() { Width = CaptureSize, Height = CaptureSize, Color = color };
        fill.Alpha = color.A;
        parent.AddChild(fill);
#pragma warning restore CS0618
    }

    private static CircleRuntime NewCircle(float x, float y, float size, XnaColor color)
    {
        CircleRuntime circle = new();
        circle.X = x;
        circle.Y = y;
        circle.Width = size;
        circle.Height = size;
        circle.IsFilled = true;
        circle.StrokeWidth = 0;
        circle.FillColor = color;
        return circle;
    }

    private static void AddSubtractMask(ContainerRuntime parent, bool useCircle, int alpha)
    {
        if (useCircle)
        {
            CircleRuntime circle = NewCircle(20, 20, 60, new XnaColor((byte)255, (byte)255, (byte)255, (byte)alpha));
            circle.Blend = GumBlend.SubtractAlpha;
            parent.AddChild(circle);
        }
        else
        {
#pragma warning disable CS0618
            ColoredRectangleRuntime mask = new() { X = 20, Y = 20, Width = 60, Height = 60, Color = XnaColor.White };
            mask.Alpha = alpha;
            mask.Blend = GumBlend.SubtractAlpha;
            parent.AddChild(mask);
#pragma warning restore CS0618
        }
    }

    private static XnaColor[] Render(Action<ContainerRuntime, ContainerRuntime, GraphicsDevice> build, bool useRenderTarget = true)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = BackdropColor });
#pragma warning restore CS0618

        ContainerRuntime renderTarget = new();
        renderTarget.Width = CaptureSize;
        renderTarget.Height = CaptureSize;
        renderTarget.IsRenderTarget = true;
        if (useRenderTarget)
        {
            root.AddChild(renderTarget);
        }

        build(root, renderTarget, gd);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this)
            {
                // Apos.Shapes uses an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
            ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.Black);

        protected override void Dispose(bool disposing)
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
