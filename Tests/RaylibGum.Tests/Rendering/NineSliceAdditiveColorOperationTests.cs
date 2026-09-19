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
/// Pixel-readback test proving raylib's NineSlice applies an authored
/// <see cref="AnimationFrameColorOperation.Add"/> frame as an additive overlay pass (#4821 gap 4),
/// mirroring <see cref="SpriteAdditiveColorOperationTests"/>.
/// </summary>
public class NineSliceAdditiveColorOperationTests : BaseTestClass
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
    public void Draw_NineSliceWithAddColorOperationFrame_SaturatesTextureTowardWhiteTint()
    {
        Image image = GenImageColor(12, 12, new Color((byte)0, (byte)0, (byte)255, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        NineSliceRuntime nineSlice = new();
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.Width = 32;
        nineSlice.Height = 32;
        nineSlice.Texture = texture;

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
        nineSlice.AnimationChains = chainList;
        nineSlice.CurrentChainName = "AddChain";

        ContainerRuntime cell = new();
        cell.Width = 32;
        cell.Height = 32;
        cell.IsRenderTarget = true;
        cell.Children.Add(nineSlice);

        GumService.Default.Root.Children.Add(cell);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(cell)!.Value);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);

        center.R.ShouldBeGreaterThan((byte)240);
        center.G.ShouldBeGreaterThan((byte)240);
        center.B.ShouldBeGreaterThan((byte)240);
    }
}
