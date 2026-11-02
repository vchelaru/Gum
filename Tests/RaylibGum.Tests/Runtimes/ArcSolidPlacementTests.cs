using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7383 - a solid (or dashed) Arc (its rounded caps and its dropshadow too) is the same shape as on Skia: a stroke of
/// constant thickness along the ellipse inscribed in its box, turned about the box's top-left corner. The
/// samples were read from the same scene rendered on Skia (opaque blue 0,0,255 over white, a 6,6 offset
/// shadow of blur 8 in black, alpha 200), at points away from antialiased edges.
/// </summary>
public class ArcSolidPlacementTests : BaseTestClass
{
    private const int Size = 200;
    private const int Tolerance = 25;
    private const int ShadowTolerance = 40;

    // Renders an arc whose box center is (100, 100) after turning.
    private static Color[,] Render(float width, float height, float rotation, float sweep, float thickness,
        bool isEndRounded, bool hasDropshadow, float dashLength = 0, float gapLength = 0)
    {
        ContainerRuntime container = new();
        container.Width = Size;
        container.Height = Size;
        container.IsRenderTarget = true;

        RectangleRuntime background = new();
        background.Width = Size;
        background.Height = Size;
        background.IsFilled = true;
        background.FillColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
        container.Children.Add(background);

        double radians = rotation * System.Math.PI / 180;
        ArcRuntime arc = new() { StartAngle = sweep == 360 ? 0 : 30, SweepAngle = sweep, Thickness = thickness };
        arc.X = (float)(100 - (width / 2 * System.Math.Cos(radians) + height / 2 * System.Math.Sin(radians)));
        arc.Y = (float)(100 - (-width / 2 * System.Math.Sin(radians) + height / 2 * System.Math.Cos(radians)));
        arc.Width = width;
        arc.Height = height;
        arc.Rotation = rotation;
        arc.IsEndRounded = isEndRounded;
        arc.StrokeDashLength = dashLength;
        arc.StrokeGapLength = gapLength;
        arc.Red = 0; arc.Green = 0; arc.Blue = 255; arc.Alpha = 255;
        if (hasDropshadow)
        {
            arc.HasDropshadow = true;
            arc.DropshadowOffsetX = 6;
            arc.DropshadowOffsetY = 6;
            arc.DropshadowBlurX = 8;
            arc.DropshadowBlurY = 8;
            arc.DropshadowRed = 0; arc.DropshadowGreen = 0; arc.DropshadowBlue = 0; arc.DropshadowAlpha = 200;
        }
        container.Children.Add(arc);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        Image image = LoadImageFromTexture(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value.Texture);
        ImageFlipVertical(ref image);
        Color[,] pixels = new Color[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                pixels[x, y] = GetImageColor(image, x, y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return pixels;
    }

    private static void ShouldMatchSkia(Color[,] pixels, (int X, int Y, int R, int G, int B)[] samples)
    {
        foreach ((int x, int y, int r, int g, int b) in samples)
        {
            // A gray sample is the blurred shadow, which is only approximately the same as Skia's.
            int tolerance = r == g && g == b && r != 255 ? ShadowTolerance : Tolerance;
            Color actual = pixels[x, y];
            ((float)actual.R).ShouldBe(r, tolerance, $"red at ({x},{y})");
            ((float)actual.G).ShouldBe(g, tolerance, $"green at ({x},{y})");
            ((float)actual.B).ShouldBe(b, tolerance, $"blue at ({x},{y})");
        }
    }

    [Fact]
    public void ArcInNonSquareBox_ThickRing_MatchesSkia()
    {
        ShouldMatchSkia(Render(140, 80, 0, 270, 30, isEndRounded: false, hasDropshadow: false), WideRingSamples);
    }

    [Fact]
    public void ArcInNonSquareBox_RotatedAndRounded_MatchesSkia()
    {
        ShouldMatchSkia(Render(140, 80, 30, 180, 8, isEndRounded: true, hasDropshadow: false), WideRotated30RoundThinSamples);
    }

    [Fact]
    public void RotatedArcInSquareBox_FollowsTheRotation()
    {
        ShouldMatchSkia(Render(100, 100, 90, 270, 30, isEndRounded: false, hasDropshadow: false), SquareRotated90Samples);
    }

    [Fact]
    public void RotatedFullRingInNonSquareBox_WithDropshadow_MatchesSkia()
    {
        ShouldMatchSkia(Render(140, 80, 90, 360, 30, isEndRounded: false, hasDropshadow: true), WideRotated90FullShadowSamples);
    }

    [Fact]
    public void RotatedRoundedArcInSquareBox_WithDropshadow_MatchesSkia()
    {
        ShouldMatchSkia(Render(100, 100, 180, 180, 8, isEndRounded: true, hasDropshadow: true), SquareRotated180RoundShadowSamples);
    }

    [Fact]
    public void RoundedArcInNonSquareBox_DropshadowIncludesTheCaps()
    {
        ShouldMatchSkia(Render(140, 80, 0, 270, 30, isEndRounded: true, hasDropshadow: true), WideRoundShadowSamples);
    }

    [Fact]
    public void DashedArcInNonSquareBox_RotatedAndRounded_MatchesSkia()
    {
        ShouldMatchSkia(Render(140, 80, 30, 270, 10, isEndRounded: true, hasDropshadow: false, dashLength: 12, gapLength: 8),
            DashedRoundWideRotated30Samples);
    }

    [Fact]
    public void DashedArcInTallBox_WithDropshadow_MatchesSkia()
    {
        ShouldMatchSkia(Render(80, 140, 0, 270, 10, isEndRounded: false, hasDropshadow: true, dashLength: 12, gapLength: 8),
            DashedButtTallShadowSamples);
    }

    private static readonly (int X, int Y, int R, int G, int B)[] WideRingSamples =
    {
        (46,98,0,0,255), (121,78,0,0,255), (104,113,0,0,255), (58,70,0,0,255), (82,72,0,0,255), (88,128,0,0,255), (71,74,0,0,255), (44,110,0,0,255), (108,134,0,0,255), (138,70,0,0,255), (68,125,0,0,255), (110,83,0,0,255), (67,9,255,255,255), (103,17,255,255,255), (76,97,255,255,255), (178,92,255,255,255), (158,14,255,255,255), (129,43,255,255,255), (65,18,255,255,255), (186,131,255,255,255), (174,94,255,255,255), (193,12,255,255,255)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] WideRotated30RoundThinSamples =
    {
        (109,56,0,0,255), (92,63,0,0,255), (136,54,0,0,255), (41,112,0,0,255), (130,54,0,0,255), (91,63,0,0,255), (128,54,0,0,255), (114,56,0,0,255), (64,80,0,0,255), (39,117,0,0,255), (119,56,0,0,255), (55,143,0,0,255), (156,42,255,255,255), (180,116,255,255,255), (160,68,255,255,255), (160,37,255,255,255), (131,71,255,255,255), (123,140,255,255,255), (180,35,255,255,255), (43,24,255,255,255), (29,23,255,255,255), (135,78,255,255,255)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] SquareRotated90Samples =
    {
        (71,117,0,0,255), (132,127,0,0,255), (130,119,0,0,255), (140,111,0,0,255), (107,133,0,0,255), (128,102,0,0,255), (139,114,0,0,255), (138,122,0,0,255), (142,113,0,0,255), (123,106,0,0,255), (74,101,0,0,255), (66,97,0,0,255), (26,169,255,255,255), (73,33,255,255,255), (4,153,255,255,255), (131,166,255,255,255), (47,44,255,255,255), (163,16,255,255,255), (157,119,255,255,255), (141,53,255,255,255), (193,104,255,255,255), (183,96,255,255,255)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] WideRotated90FullShadowSamples =
    {
        (131,93,0,0,255), (85,157,0,0,255), (70,111,0,0,255), (123,84,0,0,255), (107,137,0,0,255), (95,50,0,0,255), (85,57,0,0,255), (114,121,0,0,255), (69,106,0,0,255), (92,64,0,0,255), (101,164,0,0,255), (120,93,0,0,255), (193,33,255,255,255), (32,143,255,255,255), (17,122,255,255,255), (123,9,255,255,255), (44,183,255,255,255), (62,16,255,255,255), (194,161,255,255,255), (78,165,255,255,255), (193,84,255,255,255), (18,94,255,255,255), (103,112,248,248,248), (102,81,248,248,248), (150,101,242,242,242), (151,93,247,247,247), (101,100,246,246,246), (143,138,248,248,248), (151,90,248,248,248), (108,19,242,242,242), (126,162,249,249,249), (114,19,249,249,249)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] SquareRotated180RoundShadowSamples =
    {
        (68,133,0,0,255), (92,145,0,0,255), (145,93,0,0,255), (146,92,0,0,255), (126,137,0,0,255), (104,144,0,0,255), (70,135,0,0,255), (84,143,0,0,255), (122,140,0,0,255), (143,83,0,0,255), (112,144,0,0,255), (81,141,0,0,255), (77,31,255,255,255), (196,116,255,255,255), (153,22,255,255,255), (59,91,255,255,255), (188,12,255,255,255), (169,39,255,255,255), (72,158,255,255,255), (109,51,255,255,255), (167,24,255,255,255), (47,153,255,255,255), (129,102,246,246,246), (46,115,240,240,240), (45,115,247,247,247), (50,128,247,247,247), (127,64,248,248,248), (127,80,248,248,248), (46,120,243,243,243), (45,116,246,246,246), (88,130,242,242,242), (54,108,246,246,246)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] WideRoundShadowSamples =
    {
        (113,77,0,0,255), (59,113,0,0,255), (81,123,0,0,255), (103,87,0,0,255), (128,111,0,0,255), (43,98,0,0,255), (54,104,0,0,255), (79,84,0,0,255), (119,78,0,0,255), (75,73,0,0,255), (102,80,0,0,255), (122,78,0,0,255), (37,42,255,255,255), (50,177,255,255,255), (82,42,255,255,255), (10,5,255,255,255), (15,142,255,255,255), (104,165,255,255,255), (158,33,255,255,255), (76,47,255,255,255), (123,50,255,255,255), (137,3,255,255,255), (107,101,246,246,246), (158,113,249,249,249), (45,134,248,248,248), (160,112,247,247,247), (67,145,246,246,246), (149,140,248,248,248), (164,110,247,247,247), (112,151,247,247,247), (102,101,246,246,246), (82,149,248,248,248)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] DashedRoundWideRotated30Samples =
    {
        (121,124,0,0,255), (101,136,0,0,255), (40,126,0,0,255), (44,124,0,0,255), (114,131,0,0,255), (102,138,0,0,255), (74,71,0,0,255), (76,70,0,0,255), (40,132,0,0,255), (118,127,0,0,255), (58,141,0,0,255), (82,140,0,0,255), (79,70,0,0,255), (128,58,0,0,255), (151,171,255,255,255), (193,114,255,255,255), (25,51,255,255,255), (125,33,255,255,255), (31,17,255,255,255), (144,154,255,255,255), (183,173,255,255,255), (12,58,255,255,255), (16,8,255,255,255), (68,24,255,255,255)
    };

    private static readonly (int X, int Y, int R, int G, int B)[] DashedButtTallShadowSamples =
    {
        (85,39,0,0,255), (97,36,0,0,255), (72,60,0,0,255), (126,65,0,0,255), (99,35,0,0,255), (74,140,0,0,255), (64,97,0,0,255), (70,137,0,0,255), (99,167,0,0,255), (126,60,0,0,255), (80,158,0,0,255), (131,63,0,0,255), (67,81,0,0,255), (84,155,0,0,255), (153,3,255,255,255), (93,139,255,255,255), (71,17,255,255,255), (169,98,255,255,255), (173,137,255,255,255), (31,59,255,255,255), (180,88,255,255,255), (87,117,255,255,255), (114,195,255,255,255), (79,179,255,255,255), (94,51,177,177,177), (132,76,221,221,221), (77,93,231,231,231), (81,73,219,219,219), (109,165,202,202,202), (83,86,249,249,249), (106,43,109,109,109), (134,49,241,241,241), (118,171,217,217,217), (83,54,206,206,206)
    };
}
