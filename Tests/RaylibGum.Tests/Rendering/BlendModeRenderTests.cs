using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests proving each Gum <see cref="Blend"/> value produces its own distinct,
/// correct result on raylib (issue #3470) instead of every non-<c>Additive</c> value silently
/// collapsing to straight alpha. Uses the same render-target pixel-readback harness as
/// <see cref="RenderTargetTests"/> — the alpha-affecting blends (SubtractAlpha/ReplaceAlpha/
/// MinAlpha) only have an observable effect against a persisted destination alpha, which only a
/// baked render target gives us headlessly.
///
/// <para>Each case nests the masked cell inside an outer render-target container with its own
/// solid-blue background, and asserts on the <b>outer, composited</b> pixel rather than just the
/// cell's raw bake. A follow-up to the original fix found that reading only the raw bake missed a
/// real bug: the cell's baked pixel can carry leftover full-intensity color at near-zero alpha,
/// which is invisible until something composites that bake elsewhere — exactly what raylib's
/// render-target pipeline always does (<c>TryCompositeRenderTarget</c>'s <c>AlphaPremultiply</c>
/// blend trusts the bake is already premultiplied). Uncaught, that leftover color bled through as a
/// visibly wrong tint (e.g. SubtractAlpha showing magenta instead of revealing the blue background)
/// — see <see cref="Gum.Renderables.BlendModeExtensions"/>'s remarks for the fix.</para>
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

    // Wraps a masked cell (red background + alpha-blended masker) inside an outer render-target
    // container with a solid-blue background, so the composite step — not just the cell's own
    // bake — is exercised. Returns (raw cell bake, composited outer) center pixels.
    private static (Color cell, Color outer) DrawMaskedCellOverBlueBackground(
        int size, Color backgroundColor, Color maskerTextureColor, Blend maskerBlend)
    {
        ContainerRuntime outer = RenderTargetCell(size);
        outer.Children.Add(Background(size, new Color((byte)0, (byte)0, (byte)255, (byte)255)));

        ContainerRuntime cell = RenderTargetCell(size);
        cell.Children.Add(Background(size, backgroundColor));
        cell.Children.Add(MaskerSprite(size, maskerTextureColor, maskerBlend));
        outer.Children.Add(cell);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color cellPixel = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);
        Color outerPixel = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);

        GumService.Default.Root.Children.Clear();

        return (cellPixel, outerPixel);
    }

    [Fact]
    public void Draw_ReplaceBlendSprite_CompositesAsProperTranslucentOverwrite()
    {
        (Color cell, Color outer) = DrawMaskedCellOverBlueBackground(
            32,
            backgroundColor: new Color((byte)0, (byte)255, (byte)0, (byte)255),
            maskerTextureColor: new Color((byte)255, (byte)0, (byte)0, (byte)128),
            maskerBlend: Blend.Replace);

        // Replace overwrites the destination outright (ignoring the green background entirely). The
        // raw bake stores the masker's color premultiplied by its own alpha (~128 at alpha ~128,
        // ratio 1:1) rather than raw full-intensity red — that premultiplication is exactly what
        // keeps the composite below correct.
        cell.R.ShouldBeInRange((byte)110, (byte)145);
        cell.G.ShouldBeLessThan((byte)50);
        cell.A.ShouldBeInRange((byte)110, (byte)145);

        // Composited over the outer blue background, a half-alpha red overwrite must look like a
        // roughly even red/blue blend — not full-intensity red leaking through (the original bug:
        // an un-premultiplied bake reads its own alpha as "keep everything", showing solid red with
        // no hint of the blue behind it) and not pure blue (over-erased).
        outer.R.ShouldBeInRange((byte)100, (byte)160);
        outer.B.ShouldBeInRange((byte)100, (byte)160);
    }

    [Fact]
    public void Draw_ReplaceAlphaBlendSprite_CompositesWithoutLeakingFullIntensityColor()
    {
        (Color cell, Color outer) = DrawMaskedCellOverBlueBackground(
            32,
            backgroundColor: new Color((byte)255, (byte)0, (byte)0, (byte)255),
            maskerTextureColor: new Color((byte)0, (byte)0, (byte)0, (byte)50),
            maskerBlend: Blend.ReplaceAlpha);

        // ReplaceAlpha overwrites alpha with the masker's alpha rather than blending it with the
        // background's.
        cell.A.ShouldBeInRange((byte)30, (byte)70);

        // Composited over blue: at ~20% alpha, red should barely tint a mostly-blue result — not
        // show full-intensity red bleeding through (the bug: the bake's un-scaled red survives the
        // near-zero alpha and gets added on top of the blue instead of mostly replaced by it).
        outer.R.ShouldBeInRange((byte)20, (byte)90);
        outer.B.ShouldBeGreaterThan((byte)150);
    }

    [Fact]
    public void Draw_SubtractAlphaBlendSprite_PunchesASeeThroughHoleRevealingBackground()
    {
        (Color cell, Color outer) = DrawMaskedCellOverBlueBackground(
            32,
            backgroundColor: new Color((byte)255, (byte)0, (byte)0, (byte)255),
            maskerTextureColor: new Color((byte)0, (byte)0, (byte)0, (byte)255),
            maskerBlend: Blend.SubtractAlpha);

        // SubtractAlpha reverse-subtracts the masker's opaque alpha from the destination's opaque
        // alpha, punching a fully-transparent hole (255 - 255 = 0).
        cell.A.ShouldBeLessThan((byte)20);

        // Composited over blue, a fully-punched hole must reveal pure blue — not the pink/magenta
        // the original bug produced (leftover un-zeroed red bleeding through the "transparent" hole).
        outer.R.ShouldBeLessThan((byte)40);
        outer.B.ShouldBeGreaterThan((byte)220);
    }

    [Fact]
    public void Draw_MinAlphaBlendSprite_CompositesWithoutLeakingFullIntensityColor()
    {
        (Color cell, Color outer) = DrawMaskedCellOverBlueBackground(
            32,
            backgroundColor: new Color((byte)255, (byte)0, (byte)0, (byte)200),
            maskerTextureColor: new Color((byte)0, (byte)0, (byte)0, (byte)100),
            maskerBlend: Blend.MinAlpha);

        // MinAlpha keeps whichever alpha is lower — min(200, 100) = 100.
        cell.A.ShouldBeInRange((byte)80, (byte)120);

        // Composited over blue: red should be dimmed toward blue, not showing full-intensity red on
        // top of an also-visible blue (the double-exposure look the original bug produced).
        outer.R.ShouldBeInRange((byte)40, (byte)120);
        outer.B.ShouldBeGreaterThan((byte)120);
    }
}
