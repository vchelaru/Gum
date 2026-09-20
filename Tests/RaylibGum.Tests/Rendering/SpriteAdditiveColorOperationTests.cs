using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests proving raylib applies an authored
/// <see cref="AnimationFrameColorOperation.Add"/> frame as an additive overlay pass (#4821 gap 2),
/// mirroring MonoGame/KNI/FNA's <c>ColorOperation.Add</c> + <c>Renderer.DrawAdditiveColorOverlay</c>
/// via a second <c>DrawTexturePro</c> under a premultiplied ColorTextureAlpha-style shader with
/// <c>AddColorPreserveDestinationAlpha</c>-equivalent blend factors.
/// Uses the same render-target pixel-readback harness as the sibling <see cref="SpriteColorOperationTests"/>.
/// </summary>
public class SpriteAdditiveColorOperationTests : BaseTestClass
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

    [Fact]
    public void Draw_SpriteWithAddColorOperationFrame_SaturatesTextureTowardWhiteTint()
    {
        Image image = GenImageColor(4, 4, new Color((byte)0, (byte)0, (byte)255, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 32;
        sprite.Height = 32;
        sprite.Texture = texture;

        AnimationChain chain = new() { Name = "AddChain" };
        chain.Add(new AnimationFrame
        {
            FrameLength = 1.0f,
            Texture = texture,
            Red = 255,
            Green = 255,
            Blue = 255,
            ColorOperation = AnimationFrameColorOperation.Add,
        });
        AnimationChainList chainList = new();
        chainList.Add(chain);
        sprite.AnimationChains = chainList;
        sprite.CurrentChainName = "AddChain";

        ContainerRuntime cell = new();
        cell.Width = 32;
        cell.Height = 32;
        cell.IsRenderTarget = true;
        cell.Children.Add(sprite);

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        // Fully-opaque blue texture pixel. Add + white(255) should saturate every drawn pixel to
        // white regardless of the underlying art - the MonsterProjectWeb "un-revealed silhouette"
        // use case from #4792.
        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);

        center.R.ShouldBeGreaterThan((byte)240);
        center.G.ShouldBeGreaterThan((byte)240);
        center.B.ShouldBeGreaterThan((byte)240);
    }

    [Fact]
    public void Draw_SpriteWithNoColorOperation_IsUnaffectedByAdditiveOverlay()
    {
        // Pins that a sprite which never authors an Add frame draws exactly as it did before this
        // feature - no stray overlay pass for the common case.
        Image image = GenImageColor(4, 4, new Color((byte)0, (byte)0, (byte)255, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 32;
        sprite.Height = 32;
        sprite.Texture = texture;
        sprite.Color = new Color((byte)255, (byte)255, (byte)255, (byte)255);

        ContainerRuntime cell = new();
        cell.Width = 32;
        cell.Height = 32;
        cell.IsRenderTarget = true;
        cell.Children.Add(sprite);

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);

        center.R.ShouldBeLessThan((byte)50);
        center.G.ShouldBeLessThan((byte)50);
        center.B.ShouldBeGreaterThan((byte)200);
    }
}
