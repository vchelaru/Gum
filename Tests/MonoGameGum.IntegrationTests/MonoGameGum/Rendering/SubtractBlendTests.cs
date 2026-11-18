using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// <see cref="Gum.RenderingLibrary.Blend.Subtract"/> (#8263): rgb = max(dest - src * srcAlpha, 0), dest alpha kept.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SubtractBlendTests : BaseTestClass
{
    private const int CaptureSize = 32;

    [Theory]
    [InlineData(255, 255, 123)]
    [InlineData(82, 255, 0)]
    [InlineData(255, 128, 189)]
    public void Subtract_OverOpaqueGray_GivesClampedDifference(int behind, int sourceAlpha, int expected)
    {
        XnaColor pixel = Render((byte)behind, (byte)sourceAlpha, inRenderTarget: false);

        AssertGray(pixel, expected);
    }

    [Theory]
    [InlineData(255, 123)]
    [InlineData(82, 0)]
    public void Subtract_InsideRenderTarget_GivesClampedDifference(int behind, int expected)
    {
        XnaColor pixel = Render((byte)behind, 255, inRenderTarget: true);

        AssertGray(pixel, expected);
    }

    // Apos.Shapes writes premultiplied color, so the shape must not weight it by alpha a second time.
    [Theory]
    [InlineData(255, 255, 123)]
    [InlineData(82, 255, 0)]
    [InlineData(255, 128, 189)]
    public void Subtract_AposRectangle_GivesClampedDifference(int behind, int sourceAlpha, int expected)
    {
        XnaColor pixel = Render((byte)behind, (byte)sourceAlpha, inRenderTarget: false, useAposShape: true);

        AssertGray(pixel, expected);
    }

    private static void AssertGray(XnaColor pixel, int expected)
    {
        string message = $"was {pixel.R},{pixel.G},{pixel.B},{pixel.A}";
        System.Math.Abs(pixel.R - expected).ShouldBeLessThanOrEqualTo(1, message);
        System.Math.Abs(pixel.G - expected).ShouldBeLessThanOrEqualTo(1, message);
        System.Math.Abs(pixel.B - expected).ShouldBeLessThanOrEqualTo(1, message);
        pixel.A.ShouldBe((byte)255, message);
    }

    private static XnaColor Render(byte behind, byte sourceAlpha, bool inRenderTarget, bool useAposShape = false)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new() { Width = CaptureSize, Height = CaptureSize };
        ContainerRuntime parent = root;
        if (inRenderTarget)
        {
            parent = new ContainerRuntime { Width = CaptureSize, Height = CaptureSize, IsRenderTarget = true };
            root.AddChild(parent);
        }
#pragma warning disable CS0618
        parent.AddChild(new ColoredRectangleRuntime
        {
            Width = CaptureSize, Height = CaptureSize, Color = new XnaColor(behind, behind, behind, (byte)255)
        });
        XnaColor sourceColor = new((byte)132, (byte)132, (byte)132, sourceAlpha);
        if (useAposShape)
        {
            parent.AddChild(new RectangleRuntime
            {
                Width = CaptureSize, Height = CaptureSize, IsFilled = true, StrokeWidth = 0,
                FillColor = sourceColor, Blend = Gum.RenderingLibrary.Blend.Subtract,
            });
        }
        else
        {
            parent.AddChild(new ColoredRectangleRuntime
            {
                Width = CaptureSize, Height = CaptureSize, Color = sourceColor,
                Blend = Gum.RenderingLibrary.Blend.Subtract,
            });
        }
#pragma warning restore CS0618
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
