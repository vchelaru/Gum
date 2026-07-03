using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests proving each Gum <see cref="Blend"/> value produces its own distinct
/// result on raylib (issue #3470) instead of every non-<c>Additive</c> value silently collapsing
/// to straight alpha. Uses the same render-target pixel-readback harness as
/// <see cref="RenderTargetTests"/> — the alpha-affecting blends (SubtractAlpha/ReplaceAlpha/
/// MinAlpha) only have an observable effect against a persisted destination alpha, which only a
/// baked render target gives us headlessly.
/// </summary>
public class BlendModeRenderTests : BaseTestClass
{
    private static void DrawOnce()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
    }

    // Render targets are stored bottom-up in GL (see RenderTargetTests), but every case here reads
    // the exact center pixel, which is unaffected by a vertical flip.
    private static Color ReadRenderTargetCenter(RenderTexture2D renderTexture)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, renderTexture.Texture.Width / 2, renderTexture.Texture.Height / 2);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    private static ContainerRuntime RenderTargetCell(int size)
    {
        ContainerRuntime container = new();
        container.Width = size;
        container.Height = size;
        container.IsRenderTarget = true;
        return container;
    }

    private static ColoredRectangleRuntime Background(int size, Color color)
    {
        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = size;
        rectangle.Height = size;
        rectangle.Color = color;
        return rectangle;
    }

    // SpriteRuntime defaults WidthUnits/HeightUnits to PercentageOfSourceFile, so Width/Height must
    // be forced to Absolute here — otherwise "Width = size" is read as "size% of the source
    // texture", shrinking a 4px test texture to a near-invisible sliver.
    private static SpriteRuntime MaskerSprite(int size, Color textureColor, Blend blend)
    {
        Image image = GenImageColor(4, 4, textureColor);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = size;
        sprite.Height = size;
        sprite.Texture = texture;
        sprite.Blend = blend;
        return sprite;
    }

    [Fact]
    public void Draw_ReplaceBlendSprite_OverwritesDestinationColorIgnoringAlpha()
    {
        int size = 32;
        ContainerRuntime cell = RenderTargetCell(size);
        cell.Children.Add(Background(size, new Color((byte)0, (byte)255, (byte)0, (byte)255)));
        cell.Children.Add(MaskerSprite(size, new Color((byte)255, (byte)0, (byte)0, (byte)128), Blend.Replace));

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        // Replace overwrites the destination outright (color factors One/Zero) — the half-alpha
        // red texture must fully replace the green background, not blend with it.
        center.R.ShouldBeGreaterThan((byte)200);
        center.G.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_ReplaceAlphaBlendSprite_OverwritesDestinationAlphaOnly()
    {
        int size = 32;
        ContainerRuntime cell = RenderTargetCell(size);
        cell.Children.Add(Background(size, new Color((byte)255, (byte)0, (byte)0, (byte)255)));
        cell.Children.Add(MaskerSprite(size, new Color((byte)0, (byte)0, (byte)0, (byte)50), Blend.ReplaceAlpha));

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        // ReplaceAlpha leaves color untouched (color factors Zero/One) but overwrites alpha with
        // the masker's alpha (alpha factors One/Zero) rather than blending it with the background's.
        center.R.ShouldBeGreaterThan((byte)200);
        center.A.ShouldBeInRange((byte)30, (byte)70);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_SubtractAlphaBlendSprite_PunchesHoleInDestinationAlpha()
    {
        int size = 32;
        ContainerRuntime cell = RenderTargetCell(size);
        cell.Children.Add(Background(size, new Color((byte)255, (byte)0, (byte)0, (byte)255)));
        cell.Children.Add(MaskerSprite(size, new Color((byte)0, (byte)0, (byte)0, (byte)255), Blend.SubtractAlpha));

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        // SubtractAlpha reverse-subtracts the masker's opaque alpha from the destination's opaque
        // alpha, punching a fully-transparent hole (255 - 255 = 0).
        center.A.ShouldBeLessThan((byte)20);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_MinAlphaBlendSprite_KeepsLowerOfTheTwoAlphas()
    {
        int size = 32;
        ContainerRuntime cell = RenderTargetCell(size);
        cell.Children.Add(Background(size, new Color((byte)255, (byte)0, (byte)0, (byte)200)));
        cell.Children.Add(MaskerSprite(size, new Color((byte)0, (byte)0, (byte)0, (byte)100), Blend.MinAlpha));

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        // MinAlpha keeps whichever alpha is lower — min(200, 100) = 100.
        center.A.ShouldBeInRange((byte)80, (byte)120);

        GumService.Default.Root.Children.Clear();
    }
}
