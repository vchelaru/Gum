using Raylib_cs;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Spike for #8263: rlgl's custom-separate blend with GL_FUNC_REVERSE_SUBTRACT on color gives
/// result = clamp(dest - src) per RGB channel, leaving dest alpha unchanged.
/// </summary>
public class SubtractBlendSpike8263Tests : BaseTestClass
{
    private const int GlZero = 0;
    private const int GlOne = 1;
    private const int GlSrcAlpha = 0x0302;
    private const int GlFuncAdd = 0x8006;
    private const int GlFuncReverseSubtract = 0x800B;

    [Theory]
    [InlineData(255, 123)]
    [InlineData(82, 0)]
    public void ReverseSubtract_OverOpaqueGray_GivesClampedDifference(int behind, int expected)
    {
        RenderTexture2D target = LoadRenderTexture(8, 8);
        BeginDrawing();
        BeginTextureMode(target);
        ClearBackground(new Color((byte)behind, (byte)behind, (byte)behind, (byte)255));
        Rlgl.SetBlendFactorsSeparate(GlSrcAlpha, GlOne, GlZero, GlOne, GlFuncReverseSubtract, GlFuncAdd);
        BeginBlendMode(BlendMode.CustomSeparate);
        DrawRectangle(0, 0, 8, 8, new Color((byte)132, (byte)132, (byte)132, (byte)255));
        EndBlendMode();
        EndTextureMode();
        EndDrawing();

        Image image = LoadImageFromTexture(target.Texture);
        Color pixel = GetImageColor(image, 4, 4);
        UnloadImage(image);
        UnloadRenderTexture(target);

        System.IO.File.AppendAllText(
            "/private/tmp/claude-504/-Users-vicpersonal--claude-spares-spare-OiarL5/5e830539-c122-4f0b-b5cd-adeb57ad90ee/scratchpad/8263-raylib.txt",
            $"behind={behind} -> {pixel.R},{pixel.G},{pixel.B},{pixel.A}\n");

        System.Math.Abs(pixel.R - expected).ShouldBeLessThanOrEqualTo(1);
        System.Math.Abs(pixel.G - expected).ShouldBeLessThanOrEqualTo(1);
        System.Math.Abs(pixel.B - expected).ShouldBeLessThanOrEqualTo(1);
        pixel.A.ShouldBe((byte)255);
    }
}
