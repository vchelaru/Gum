using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The reference number for a sprite's Alpha: an opaque white sprite at Alpha 128 over black draws at
/// half brightness. SkiaGum.Tests.Renderables.SpriteFlipAndAlphaTests pins the same number on Skia.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SpriteAlphaTests : BaseTestClass
{
    private const int Size = 32;

    [Fact]
    public void HalfAlphaSprite_OverBlack_DrawsAtHalfBrightness()
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        using Texture2D texture = new(gd, 4, 4, false, SurfaceFormat.Color);
        XnaColor[] white = new XnaColor[16];
        Array.Fill(white, XnaColor.White);
        texture.SetData(white);

        SpriteRuntime sprite = new()
        {
            Texture = texture,
            Width = Size,
            Height = Size,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Alpha = 128,
        };
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        using RenderTarget2D capture = new(gd, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.Black);
        managers.Renderer.Draw(managers);
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Size * Size];
        capture.GetData(pixels);
        sprite.RemoveFromManagers();

        ((int)pixels[(16 * Size) + 16].R).ShouldBeInRange(126, 130);
    }

    // #7837: ColorTextureAlpha (Silhouette) applied Alpha twice and drew 402000 instead of 804000.
    [Fact]
    public void HalfAlphaColorTextureAlphaSprite_OverBlack_DrawsTintAtHalfBrightness()
    {
        XnaColor pixel = RenderHalfAlphaOpaqueWhiteSprite(new XnaColor((byte)255, (byte)128, (byte)0, (byte)255),
            ColorOperation.ColorTextureAlpha);

        ((int)pixel.R).ShouldBeInRange(126, 130);
        ((int)pixel.G).ShouldBeInRange(62, 66);
        ((int)pixel.B).ShouldBeInRange(0, 2);
    }

    [Fact]
    public void HalfAlphaWhiteColorTextureAlphaSprite_OverBlack_DrawsAtHalfBrightness()
    {
        XnaColor pixel = RenderHalfAlphaOpaqueWhiteSprite(XnaColor.White, ColorOperation.ColorTextureAlpha);

        ((int)pixel.R).ShouldBeInRange(126, 130);
    }

    private static XnaColor RenderHalfAlphaOpaqueWhiteSprite(XnaColor tint, ColorOperation colorOperation)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        using Texture2D texture = new(gd, 4, 4, false, SurfaceFormat.Color);
        XnaColor[] white = new XnaColor[16];
        Array.Fill(white, XnaColor.White);
        texture.SetData(white);

        SpriteRuntime sprite = new()
        {
            Texture = texture,
            Width = Size,
            Height = Size,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Color = tint,
            Alpha = 128,
            ColorOperation = colorOperation,
        };
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        using RenderTarget2D capture = new(gd, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.Black);
        managers.Renderer.Draw(managers);
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Size * Size];
        capture.GetData(pixels);
        sprite.RemoveFromManagers();

        return pixels[(16 * Size) + 16];
    }
}
