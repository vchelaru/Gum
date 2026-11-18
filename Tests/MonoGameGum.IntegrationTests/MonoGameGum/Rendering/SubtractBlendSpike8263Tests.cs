using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Spike for #8263: can a Gum.BlendState built on ReverseSubtract give result = clamp(dest - src)
/// per RGB channel, leaving dest alpha unchanged?
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SubtractBlendSpike8263Tests : BaseTestClass
{
    private const int CaptureSize = 32;
    private readonly ITestOutputHelper _output;

    public SubtractBlendSpike8263Tests(ITestOutputHelper output) => _output = output;

    private static Gum.BlendState CreateSubtract() => new()
    {
        // rgb = dst*1 - src*srcA (straight alpha); a = dst
        ColorSourceBlend = Gum.Blend.SourceAlpha,
        ColorDestinationBlend = Gum.Blend.One,
        ColorBlendFunction = Gum.BlendFunction.ReverseSubtract,
        AlphaSourceBlend = Gum.Blend.Zero,
        AlphaDestinationBlend = Gum.Blend.One,
        AlphaBlendFunction = Gum.BlendFunction.Add,
        BlendFactor = System.Drawing.Color.White,
        ColorWriteChannels = Gum.ColorWriteChannels.All,
        ColorWriteChannels1 = Gum.ColorWriteChannels.All,
        ColorWriteChannels2 = Gum.ColorWriteChannels.All,
        ColorWriteChannels3 = Gum.ColorWriteChannels.All,
    };

    [Theory]
    [InlineData(255, 123)]
    [InlineData(82, 0)]
    public void ReverseSubtract_OverOpaqueGray_GivesClampedDifference(int behind, int expected)
    {
        XnaColor pixel = Render((byte)behind);
        System.IO.File.AppendAllText("/private/tmp/claude-504/-Users-vicpersonal--claude-spares-spare-OiarL5/5e830539-c122-4f0b-b5cd-adeb57ad90ee/scratchpad/8263-monogame.txt", $"behind={behind} -> {pixel.R},{pixel.G},{pixel.B},{pixel.A}\n");

        System.Math.Abs(pixel.R - expected).ShouldBeLessThanOrEqualTo(1);
        System.Math.Abs(pixel.G - expected).ShouldBeLessThanOrEqualTo(1);
        System.Math.Abs(pixel.B - expected).ShouldBeLessThanOrEqualTo(1);
        pixel.A.ShouldBe((byte)255);
    }

    private static XnaColor Render(byte behind)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new() { Width = CaptureSize, Height = CaptureSize };
#pragma warning disable CS0618
        root.AddChild(new ColoredRectangleRuntime
        {
            Width = CaptureSize, Height = CaptureSize, Color = new XnaColor(behind, behind, behind, (byte)255)
        });
        ColoredRectangleRuntime top = new()
        {
            Width = CaptureSize, Height = CaptureSize, Color = new XnaColor((byte)132, (byte)132, (byte)132, (byte)255)
        };
#pragma warning restore CS0618
        ((SolidRectangle)top.RenderableComponent).BlendState = CreateSubtract();
        root.AddChild(top);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.Black);
        managers.Renderer.Draw(managers);
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);
        root.RemoveFromManagers();
        return pixels[(16 * CaptureSize) + 16];
    }
}
