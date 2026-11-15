using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Raylib_cs;
using RaylibGum.Tests.Helpers;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A run of equal blends must not pay for a native raylib blend call per item, and skipping those
/// calls must never leave a stale blend mode: the game's own mode is reset once at the start of a
/// Gum pass, and every state change Gum makes itself (Additive, bakes, aborted passes) is tracked.
/// </summary>
public class RaylibBlendNativeCallTests : BaseTestClass
{
    private const int CanvasSize = 120;
    private static readonly Color Backdrop = new Color((byte)0, (byte)0, (byte)255, (byte)255);

    private static Texture2D MakeTexture(int alpha)
    {
        Image image = GenImageColor(4, 4, new Color((byte)255, (byte)0, (byte)0, (byte)alpha));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    private static SpriteRuntime Sprite(Texture2D texture, int x, int y, Blend? blend = null)
    {
        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 30;
        sprite.Height = 30;
        sprite.X = x;
        sprite.Y = y;
        sprite.Texture = texture;
        if (blend.HasValue)
        {
            sprite.Blend = blend;
        }
        Gum.GumService.Default.Root.Children.Add(sprite);
        return sprite;
    }

    // Draws one Gum pass into a render texture over the backdrop, with an optional ambient mode set by the game.
    private static (PixelGrid Pixels, int NativeCalls) DrawPass(BlendMode? ambient)
    {
        Gum.GumService.Default.Root.UpdateLayout();
        RenderTexture2D target = LoadRenderTexture(CanvasSize, CanvasSize);
        BatchDrawCallCounter counter = Renderer.Self.BatchDrawCallCounter;
        try
        {
            BeginDrawing();
            BeginTextureMode(target);
            ClearBackground(Backdrop);
            if (ambient.HasValue)
            {
                BeginBlendMode(ambient.Value);
            }
            int before = counter.NativeBlendModeCallCount;
            Gum.GumService.Default.Draw();
            int calls = counter.NativeBlendModeCallCount - before;
            if (ambient.HasValue)
            {
                EndBlendMode();
            }
            EndTextureMode();
            EndDrawing();
            return (RaylibPixelHarness.Read(target, CanvasSize, CanvasSize), calls);
        }
        finally
        {
            UnloadRenderTexture(target);
        }
    }

    // RGB only: raylib squares source alpha into a render texture's own alpha (#4204).
    private static void AssertRgb(Color actual, int r, int g, int b, string because)
    {
        string message = $"{because}: was ({actual.R},{actual.G},{actual.B}), expected ({r},{g},{b})";
        Math.Abs(actual.R - r).ShouldBeLessThanOrEqualTo(30, message);
        Math.Abs(actual.G - g).ShouldBeLessThanOrEqualTo(30, message);
        Math.Abs(actual.B - b).ShouldBeLessThanOrEqualTo(30, message);
    }

    private static void ShouldBeNormalHalfRed(PixelGrid pixels, int x, int y, string because)
    {
        // Half-alpha red over blue by source alpha: (128, 0, 127).
        AssertRgb(pixels[x, y], 128, 0, 127, because);
    }

    private static void ShouldBeAdditiveHalfRed(PixelGrid pixels, int x, int y)
    {
        AssertRgb(pixels[x, y], 128, 0, 255, "additive");
    }

    [Fact]
    public void Draw_ManyNormalSprites_CallsNativeBlendModeOncePerPass()
    {
        Texture2D texture = MakeTexture(255);
        for (int i = 0; i < 200; i++)
        {
            Sprite(texture, (i % 10) * 10, (i / 10) * 5);
        }

        (PixelGrid _, int calls) = DrawPass(ambient: null);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        calls.ShouldBe(1);
    }

    [Fact]
    public void Draw_NormalAdditiveNormal_TogglesAndDrawsEachCorrectly()
    {
        Texture2D texture = MakeTexture(128);
        Sprite(texture, 0, 0);
        Sprite(texture, 40, 0, Blend.Additive);
        Sprite(texture, 80, 0);

        (PixelGrid pixels, int calls) = DrawPass(ambient: null);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        ShouldBeNormalHalfRed(pixels, 15, 15, "first");
        ShouldBeAdditiveHalfRed(pixels, 55, 15);
        ShouldBeNormalHalfRed(pixels, 95, 15, "third");
        // One reset at the start of the pass, then Begin and End around the additive sprite.
        calls.ShouldBe(3);
    }

    [Fact]
    public void Draw_AmbientAdditiveBeforePass_IsResetOnceAndEveryItemDrawsNormal()
    {
        Texture2D texture = MakeTexture(128);
        Sprite(texture, 0, 0);
        Sprite(texture, 40, 0);
        Sprite(texture, 80, 0);

        (PixelGrid pixels, int calls) = DrawPass(BlendMode.Additive);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        ShouldBeNormalHalfRed(pixels, 15, 15, "first");
        ShouldBeNormalHalfRed(pixels, 55, 15, "second");
        ShouldBeNormalHalfRed(pixels, 95, 15, "third");
        calls.ShouldBe(1);
    }

    [Fact]
    public void Draw_TwoPassesWithTheGameSettingAdditiveBetween_BothDrawNormal()
    {
        Texture2D texture = MakeTexture(128);
        Sprite(texture, 0, 0);

        (PixelGrid first, int firstCalls) = DrawPass(ambient: null);
        (PixelGrid second, int secondCalls) = DrawPass(BlendMode.Additive);
        (PixelGrid third, int thirdCalls) = DrawPass(ambient: null);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        ShouldBeNormalHalfRed(first, 15, 15, "first pass");
        ShouldBeNormalHalfRed(second, 15, 15, "pass after the game set additive");
        ShouldBeNormalHalfRed(third, 15, 15, "pass after the game reset");
        (firstCalls + secondCalls + thirdCalls).ShouldBe(3);
    }

    [Fact]
    public void Draw_BakeInTheMiddle_ItemsAfterItStillDrawNormalUnderAmbientAdditive()
    {
        Texture2D texture = MakeTexture(128);
        Sprite(texture, 0, 0);

        ContainerRuntime bake = new();
        bake.Width = 30;
        bake.Height = 30;
        bake.X = 40;
        bake.IsRenderTarget = true;
        SpriteRuntime inside = new();
        inside.WidthUnits = DimensionUnitType.Absolute;
        inside.HeightUnits = DimensionUnitType.Absolute;
        inside.Width = 30;
        inside.Height = 30;
        inside.Texture = texture;
        bake.Children.Add(inside);
        Gum.GumService.Default.Root.Children.Add(bake);

        Sprite(texture, 80, 0);

        (PixelGrid pixels, int _) = DrawPass(BlendMode.Additive);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        ShouldBeNormalHalfRed(pixels, 15, 15, "before the bake");
        ShouldBeNormalHalfRed(pixels, 95, 15, "after the bake");
        // The baked container composites a half-alpha texture over blue: red rises, blue falls.
        Color baked = pixels[55, 15];
        baked.R.ShouldBeGreaterThan((byte)60);
        baked.B.ShouldBeLessThan((byte)230);
    }

    [Fact]
    public void Draw_AfterAPassAbortedMidBake_NextPassIsNotLeftInThePremultiplyMode()
    {
        // A pass that threw between BeginRenderTargetBlend and EndRenderTargetBlend never cleared its flags.
        BatchDrawCallCounter counter = Renderer.Self.BatchDrawCallCounter;
        counter.BeginPass(Renderer.Self.RenderStateChangeStatistics);
        counter.BeginRenderTargetBlend();
        counter.EndPass();

        Texture2D texture = MakeTexture(128);
        Sprite(texture, 0, 0);
        Sprite(texture, 40, 0);

        (PixelGrid pixels, int calls) = DrawPass(BlendMode.Additive);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        ShouldBeNormalHalfRed(pixels, 15, 15, "first");
        ShouldBeNormalHalfRed(pixels, 55, 15, "second");
        calls.ShouldBe(1);
    }
}
