using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests proving the raylib Sprite honors <see cref="ColorOperation.ColorTextureAlpha"/>
/// (issue #3486, umbrella #3432): the texture's alpha is used as a mask and the sprite is filled with
/// its tint <c>Color</c>, so the texture's own RGB is discarded — matching MonoGame's ColorTextureAlpha
/// technique. The default <see cref="ColorOperation.Modulate"/> still multiplies texture RGB by the tint.
/// Uses the same render-target pixel-readback harness as the sibling <see cref="BlendModeRenderTests"/>:
/// a single opaque sprite whose texture color and tint color differ is baked into a render-target cell,
/// so Modulate (texture*tint) and ColorTextureAlpha (tint, texture-alpha-masked) land on visibly
/// different pixels. The cell center is unaffected by the RT's bottom-up GL vertical flip.
/// </summary>
public class SpriteColorOperationTests : BaseTestClass
{
    private static void DrawOnce()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
    }

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

    // SpriteRuntime defaults WidthUnits/HeightUnits to PercentageOfSourceFile, so Width/Height must be
    // forced to Absolute — otherwise "Width = size" is read as "size% of the 4px source", shrinking the
    // sprite to a sliver that misses the sampled center. ColorOperation lives on the renderable only
    // (parity with MonoGame, which exposes no SpriteRuntime.ColorOperation), so it is set via the
    // contained Sprite through RenderableComponent.
    private static Color BakeSpriteCell(int size, Color textureColor, Color tint, ColorOperation colorOperation)
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
        sprite.Color = tint;
        ((Sprite)sprite.RenderableComponent).ColorOperation = colorOperation;

        ContainerRuntime cell = new();
        cell.Width = size;
        cell.Height = size;
        cell.IsRenderTarget = true;
        cell.Children.Add(sprite);

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
        return center;
    }

    [Fact]
    public void Draw_ColorTextureAlphaSprite_FillsWithTintColorIgnoringTextureRgb()
    {
        Color center = BakeSpriteCell(
            32,
            textureColor: new Color((byte)0, (byte)0, (byte)255, (byte)255),
            tint: new Color((byte)255, (byte)0, (byte)0, (byte)255),
            colorOperation: ColorOperation.ColorTextureAlpha);

        // The blue texture's RGB is discarded; the sprite is filled with its red tint, masked by the
        // texture's (fully opaque) alpha. So the baked pixel reads red — not the blue*red = black a
        // Modulate draw would produce.
        center.R.ShouldBeGreaterThan((byte)200);
        center.G.ShouldBeLessThan((byte)50);
        center.B.ShouldBeLessThan((byte)50);
    }

    [Fact]
    public void Draw_ModulateSprite_MultipliesTextureRgbByTint()
    {
        Color center = BakeSpriteCell(
            32,
            textureColor: new Color((byte)0, (byte)0, (byte)255, (byte)255),
            tint: new Color((byte)255, (byte)0, (byte)0, (byte)255),
            colorOperation: ColorOperation.Modulate);

        // The default Modulate multiplies texture (blue) by tint (red) => black. Pins the untouched
        // default path and contrasts it with ColorTextureAlpha above.
        center.R.ShouldBeLessThan((byte)50);
        center.B.ShouldBeLessThan((byte)50);
    }
}
