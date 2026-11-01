using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #7290: a host that draws Gum inside its own BeginTextureMode (the headless screenshot
/// path does) lost every draw around a blurred dropshadow. Creating the shadow's offscreen
/// textures, and its blur passes, unbind the host's texture and raylib restores nothing, so the
/// batch pending from earlier shapes flushed to the screen and later shapes drew there too.
/// </summary>
public class HostRenderTextureShadowTests : BaseTestClass
{
    private static RectangleRuntime Box(float x, float y, byte r, byte g, byte b)
    {
        RectangleRuntime box = new();
        box.X = x;
        box.Y = y;
        box.Width = 30;
        box.Height = 30;
        box.IsFilled = true;
        box.FillColor = new Color(r, g, b, (byte)255);
        return box;
    }

    private static Color ReadPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    private static (int First, int Second) CountDrawCallsFirstTwoFrames(bool insideHostTexture)
    {
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime shadowed = Box(10 + i * 40, 10, 0, 0, 255);
            shadowed.HasDropshadow = true;
            shadowed.DropshadowBlur = 5;
            shadowed.DropshadowAlpha = 255;
            GumService.Default.Root.Children.Add(shadowed);
        }
        GumService.Default.Root.UpdateLayout();

        RenderTexture2D host = LoadRenderTexture(256, 128);
        try
        {
            int drawCalls = 0;
            int secondFrameDrawCalls = 0;
            for (int frame = 0; frame < 2; frame++)
            {
                BeginDrawing();
                if (insideHostTexture)
                {
                    BeginTextureMode(host);
                }
                GumService.Default.Draw();
                if (insideHostTexture)
                {
                    EndTextureMode();
                }
                EndDrawing();

                int count = global::RenderingLibrary.Graphics.Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
                if (frame == 0)
                {
                    drawCalls = count;
                }
                else
                {
                    secondFrameDrawCalls = count;
                }
            }

            return (drawCalls, secondFrameDrawCalls);
        }
        finally
        {
            UnloadRenderTexture(host);
            GumService.Default.Root.Children.Clear();
        }
    }

    [Fact]
    public void Draw_FirstFrameWithShadowedBoxes_DrawCallCountMatchesWithAndWithoutHostTexture()
    {
        // Fresh shadow render textures are created (and their pending batch flushed) on each first
        // frame; that flush must be counted the same wherever the frame is drawn.
        (int onScreenFirst, int onScreenSecond) = CountDrawCallsFirstTwoFrames(insideHostTexture: false);
        (int hostFirst, int hostSecond) = CountDrawCallsFirstTwoFrames(insideHostTexture: true);

        hostFirst.ShouldBe(onScreenFirst);
        // The second frame reuses the cached blur (#7303), so it is cheaper than the first and the same either way.
        hostSecond.ShouldBe(onScreenSecond);
        onScreenSecond.ShouldBeLessThan(onScreenFirst);
    }

    [Fact]
    public void Draw_BlurredShadowInsideHostRenderTexture_KeepsShapesBeforeAndAfterInHostTexture()
    {
        RectangleRuntime before = Box(10, 10, 255, 0, 0);

        RectangleRuntime shadowed = Box(10, 60, 0, 0, 255);
        shadowed.HasDropshadow = true;
        shadowed.DropshadowColor = new Color((byte)0, (byte)0, (byte)0, (byte)255);
        shadowed.DropshadowOffsetX = 0;
        shadowed.DropshadowOffsetY = 0;
        shadowed.DropshadowBlur = 4;

        RectangleRuntime after = Box(60, 10, 0, 255, 0);

        GumService.Default.Root.Children.Add(before);
        GumService.Default.Root.Children.Add(shadowed);
        GumService.Default.Root.Children.Add(after);
        GumService.Default.Root.UpdateLayout();

        RenderTexture2D host = LoadRenderTexture(128, 128);
        try
        {
            BeginDrawing();
            BeginTextureMode(host);
            ClearBackground(Color.Black);
            GumService.Default.Draw();
            EndTextureMode();
            EndDrawing();

            ReadPixel(host, 25, 25).R.ShouldBeGreaterThan((byte)200);
            ReadPixel(host, 25, 75).B.ShouldBeGreaterThan((byte)200);
            ReadPixel(host, 75, 25).G.ShouldBeGreaterThan((byte)200);
        }
        finally
        {
            UnloadRenderTexture(host);
            GumService.Default.Root.Children.Clear();
        }
    }
}
