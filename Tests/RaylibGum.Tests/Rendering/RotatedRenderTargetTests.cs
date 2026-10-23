using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A render-target container must draw what the same container draws without a render target,
/// rotated or not (#6617, mirroring MonoGame's and Skia's RotatedRenderTargetTests). Each scene is
/// drawn inside an upright render-target wrapper whose baked texture is the capture, so the scene's
/// own composite lands in a texture the test can read back.
/// </summary>
public class RotatedRenderTargetTests : BaseTestClass
{
    const int Size = 240;
    const float ShapeCenterTolerance = 0.5f;
    const float TextCenterTolerance = 1.5f;

    static readonly Color Red = new((byte)255, (byte)0, (byte)0, (byte)255);
    static readonly Color Blue = new((byte)0, (byte)0, (byte)255, (byte)255);

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(30f, 0f)]
    [InlineData(0f, 30f)]
    [InlineData(30f, -50f)]
    public void Draw_RenderTarget_MatchesSameContainerWithoutRenderTarget(float containerRotation, float parentRotation)
    {
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime parent = new() { X = 60, Y = 60, Width = 0, Height = 0, Rotation = parentRotation };
            ContainerRuntime container = new()
            {
                Width = 60,
                Height = 60,
                Rotation = containerRotation,
                IsRenderTarget = isRenderTarget,
            };
            container.AddChild(SolidRect(40, 0, 20, 20, Red));
            container.AddChild(SolidRect(0, 40, 20, 20, Blue));
            container.AddChild(new TextRuntime { X = 0, Y = 20, Text = "WW", Color = Color.White });
            parent.AddChild(container);
            return parent;
        }

        Color[] plain = Capture(Build(isRenderTarget: false));
        Color[] viaRenderTarget = Capture(Build(isRenderTarget: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsTextColored, minimumCount: 30, TextCenterTolerance, countTolerancePercent: 25);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Draw_NestedRotatedContainers_MatchWithoutRenderTargets(bool isOuterRenderTarget, bool isInnerRenderTarget)
    {
        GraphicalUiElement Build(bool useRenderTargets)
        {
            ContainerRuntime outer = new()
            {
                X = 100, Y = 100, Width = 90, Height = 90, Rotation = 30,
                IsRenderTarget = useRenderTargets && isOuterRenderTarget,
            };
            outer.AddChild(SolidRect(0, 60, 20, 20, Blue));
            ContainerRuntime inner = new()
            {
                X = 20, Y = 0, Width = 60, Height = 60, Rotation = 20,
                IsRenderTarget = useRenderTargets && isInnerRenderTarget,
            };
            inner.AddChild(SolidRect(10, 20, 20, 20, Red));
            outer.AddChild(inner);
            return outer;
        }

        Color[] plain = Capture(Build(useRenderTargets: false));
        Color[] viaRenderTarget = Capture(Build(useRenderTargets: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    [Fact]
    public void Draw_RotatedRenderTargetWithCenterOrigin_MatchesWithoutRenderTarget()
    {
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime container = new()
            {
                X = 120, Y = 120, Width = 60, Height = 60, Rotation = 30, IsRenderTarget = isRenderTarget,
                XOrigin = HorizontalAlignment.Center,
                YOrigin = VerticalAlignment.Center,
            };
            container.AddChild(SolidRect(40, 0, 20, 20, Red));
            container.AddChild(SolidRect(0, 40, 20, 20, Blue));
            return container;
        }

        Color[] plain = Capture(Build(isRenderTarget: false));
        Color[] viaRenderTarget = Capture(Build(isRenderTarget: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    // An upright render target partly off camera bakes only its visible part, clamped to the camera
    // (#3478), and still lands where the plain container draws. The camera is the 800x600 test
    // window; the wrapper sits so the container crosses the camera's top-left or bottom-right edge.
    [Theory]
    [InlineData(0f, 0f, -30f, -30f)]
    [InlineData(560f, 360f, 200f, 200f)]
    public void Draw_UprightRenderTargetPartlyOffCamera_MatchesWithoutRenderTarget(
        float wrapperX, float wrapperY, float x, float y)
    {
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime container = new()
            {
                X = x, Y = y, Width = 60, Height = 60, IsRenderTarget = isRenderTarget,
            };
            container.AddChild(SolidRect(0, 0, 60, 60, Blue));
            container.AddChild(SolidRect(25, 30, 30, 25, Red));
            return container;
        }

        Color[] plain = Capture(Build(isRenderTarget: false), wrapperX, wrapperY);
        Color[] viaRenderTarget = Capture(Build(isRenderTarget: true), wrapperX, wrapperY);

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 50, ShapeCenterTolerance, countTolerancePercent: 5);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 150, ShapeCenterTolerance, countTolerancePercent: 5);
    }

    // A render target truncates a child that overflows it; turning the container keeps the same
    // truncated area (a 50x20 child at x 40 keeps only the part inside the 60x60 container).
    [Fact]
    public void Draw_RotatedRenderTarget_KeepsTheUnrotatedClippedArea()
    {
        GraphicalUiElement Build(float rotation)
        {
            ContainerRuntime container = new()
            {
                X = 100, Y = 100, Width = 60, Height = 60, Rotation = rotation, IsRenderTarget = true,
            };
            container.AddChild(SolidRect(40, 10, 50, 20, Red));
            return container;
        }

        int uprightCount = Measure(Capture(Build(rotation: 0)), IsRed).Count;
        int turnedCount = Measure(Capture(Build(rotation: 30)), IsRed).Count;

        // Untruncated, the child would cover 1000 pixels.
        uprightCount.ShouldBeInRange(300, 450);
        turnedCount.ShouldBeInRange(uprightCount * 9 / 10, uprightCount * 11 / 10);
    }

    // The corner child of a container rotated 30 degrees counterclockwise lands above the
    // container's origin, outside its axis-aligned rectangle, with or without a render target.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Draw_RotatedContainer_PutsCornerChildAboveOrigin(bool isRenderTarget)
    {
        ContainerRuntime container = new()
        {
            X = 60, Y = 60, Width = 60, Height = 60, Rotation = 30, IsRenderTarget = isRenderTarget,
        };
        container.AddChild(SolidRect(40, 0, 20, 20, Red));

        Color[] pixels = Capture(container);

        // Local (50, 10), the corner child's center, turned 30 degrees counterclockwise about the
        // container origin (60, 60): (60 + 50cos30 + 10sin30, 60 - 50sin30 + 10cos30).
        Color pixel = pixels[(44 * Size) + 108];
        IsRed(pixel).ShouldBeTrue($"pixel was {pixel.R},{pixel.G},{pixel.B},{pixel.A}");
    }

    // A turned render target is not clamped to the camera; the part on camera still lands where the
    // plain container draws, at either edge of the 800x600 test camera.
    [Theory]
    [InlineData(0f, 0f, -20f, 10f)]
    [InlineData(560f, 360f, 170f, 190f)]
    public void Draw_RotatedRenderTargetPartlyOffCamera_MatchesWithoutRenderTarget(
        float wrapperX, float wrapperY, float x, float y)
    {
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime container = new()
            {
                X = x, Y = y, Width = 60, Height = 60, Rotation = 30, IsRenderTarget = isRenderTarget,
            };
            container.AddChild(SolidRect(0, 0, 60, 60, Blue));
            container.AddChild(SolidRect(25, 30, 30, 25, Red));
            return container;
        }

        Color[] plain = Capture(Build(isRenderTarget: false), wrapperX, wrapperY);
        Color[] viaRenderTarget = Capture(Build(isRenderTarget: true), wrapperX, wrapperY);

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 50, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 150, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    // A camera zoom scales the bake and the composite; a turned render target still matches.
    [Fact]
    public void Draw_RotatedRenderTargetWithCameraZoom_MatchesWithoutRenderTarget()
    {
        const float zoom = 2;
        GraphicalUiElement Build(bool isRenderTarget)
        {
            ContainerRuntime container = new()
            {
                X = 40, Y = 40, Width = 40, Height = 40, Rotation = 30, IsRenderTarget = isRenderTarget,
            };
            container.AddChild(SolidRect(25, 0, 15, 15, Red));
            container.AddChild(SolidRect(0, 25, 15, 15, Blue));
            return container;
        }

        Color[] plain = Capture(Build(isRenderTarget: false), zoom: zoom);
        Color[] viaRenderTarget = Capture(Build(isRenderTarget: true), zoom: zoom);

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 600, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 600, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    // A ClipsChildren container inside a rotated render target keeps its child's overflow inside the
    // turned clip rectangle (#6618). The 40x40 clip sits at local (40, 0) and holds a 100x100 child,
    // so exactly the 40x40 clip shows, centered at local (60, 20) turned about the container origin.
    [Theory]
    [InlineData(0f)]
    [InlineData(30f)]
    [InlineData(-50f)]
    public void Draw_ClipsChildrenInsideRotatedRenderTarget_ShowsTheTurnedClipRectangle(float rotation)
    {
        ContainerRuntime container = new()
        {
            X = 100, Y = 100, Width = 100, Height = 100, Rotation = rotation, IsRenderTarget = true,
        };
        ContainerRuntime clip = new() { X = 40, Y = 0, Width = 40, Height = 40, ClipsChildren = true };
        clip.AddChild(SolidRect(0, 0, 100, 100, Red));
        container.AddChild(clip);

        Coverage coverage = Measure(Capture(container), IsRed);

        double radians = rotation * System.Math.PI / 180;
        float expectedX = 100 + (float)((60 * System.Math.Cos(radians)) + (20 * System.Math.Sin(radians)));
        float expectedY = 100 + (float)((-60 * System.Math.Sin(radians)) + (20 * System.Math.Cos(radians)));
        string label = coverage.ToString();
        coverage.Count.ShouldBeInRange(1500, 1700, label);
        coverage.CenterX.ShouldBe(expectedX, 1f, label);
        coverage.CenterY.ShouldBe(expectedY, 1f, label);
    }

    // A clip near the far corner of the container, whose child overflows the clip on every side.
    [Fact]
    public void Draw_ClipsChildrenNearCornerOfRotatedRenderTarget_ShowsTheTurnedClipRectangle()
    {
        ContainerRuntime container = new()
        {
            X = 100, Y = 100, Width = 100, Height = 100, Rotation = 30, IsRenderTarget = true,
        };
        ContainerRuntime clip = new() { X = 60, Y = 60, Width = 40, Height = 40, ClipsChildren = true };
        clip.AddChild(SolidRect(-20, -20, 100, 100, Red));
        container.AddChild(clip);

        Coverage coverage = Measure(Capture(container), IsRed);

        // Clip center is local (80, 80) turned 30 degrees about (100, 100).
        float expectedX = 100 + (float)((80 * System.Math.Cos(System.Math.PI / 6)) + (80 * System.Math.Sin(System.Math.PI / 6)));
        float expectedY = 100 + (float)((-80 * System.Math.Sin(System.Math.PI / 6)) + (80 * System.Math.Cos(System.Math.PI / 6)));
        string label = coverage.ToString();
        coverage.Count.ShouldBeInRange(1500, 1700, label);
        coverage.CenterX.ShouldBe(expectedX, 1f, label);
        coverage.CenterY.ShouldBe(expectedY, 1f, label);
    }

    // A render-target container that is itself ClipsChildren and turned shows its whole turned
    // rectangle: the bake already crops to it, so the composite needs no further clip (#6618).
    [Theory]
    [InlineData(0f)]
    [InlineData(30f)]
    [InlineData(90f)]
    public void Draw_RotatedRenderTargetThatClipsChildren_ShowsTheWholeTurnedRectangle(float rotation)
    {
        ContainerRuntime container = new()
        {
            X = 120, Y = 120, Width = 80, Height = 80, Rotation = rotation, IsRenderTarget = true, ClipsChildren = true,
        };
        container.AddChild(SolidRect(0, 0, 80, 80, Red));

        Coverage coverage = Measure(Capture(container), IsRed);

        double radians = rotation * System.Math.PI / 180;
        float expectedX = 120 + (float)((40 * System.Math.Cos(radians)) + (40 * System.Math.Sin(radians)));
        float expectedY = 120 + (float)((-40 * System.Math.Sin(radians)) + (40 * System.Math.Cos(radians)));
        string label = coverage.ToString();
        coverage.Count.ShouldBeInRange(6000, 6600, label);
        coverage.CenterX.ShouldBe(expectedX, 1f, label);
        coverage.CenterY.ShouldBe(expectedY, 1f, label);
    }

    record struct Coverage(int Count, float CenterX, float CenterY);

    static bool IsRed(Color c) => c.R > 150 && c.G < 80 && c.B < 80;

    static bool IsBlue(Color c) => c.B > 150 && c.G < 80 && c.R < 80;

    static bool IsTextColored(Color c) => c.R > 100 && c.G > 100 && c.B > 100;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; it is the SolidRectangle-backed runtime MonoGame's copy uses.
    static ColoredRectangleRuntime SolidRect(float x, float y, float width, float height, Color color) =>
        new() { X = x, Y = y, Width = width, Height = height, Color = color };
#pragma warning restore CS0618

    static void AssertSameCoverage(Color[] expected, Color[] actual, System.Func<Color, bool> predicate,
        int minimumCount, float centerTolerance, int countTolerancePercent)
    {
        Coverage e = Measure(expected, predicate);
        Coverage a = Measure(actual, predicate);
        string label = $"expected={e} actual={a}";
        e.Count.ShouldBeGreaterThan(minimumCount, label);
        a.Count.ShouldBeInRange(
            e.Count * (100 - countTolerancePercent) / 100, e.Count * (100 + countTolerancePercent) / 100, label);
        a.CenterX.ShouldBe(e.CenterX, centerTolerance, label);
        a.CenterY.ShouldBe(e.CenterY, centerTolerance, label);
    }

    static Coverage Measure(Color[] pixels, System.Func<Color, bool> predicate)
    {
        int count = 0;
        long sumX = 0;
        long sumY = 0;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (predicate(pixels[(y * Size) + x]))
                {
                    count++;
                    sumX += x;
                    sumY += y;
                }
            }
        }
        return count == 0 ? new Coverage(0, -1, -1) : new Coverage(count, sumX / (float)count, sumY / (float)count);
    }

    // Draws the scene inside an upright Size x Size render-target wrapper at (wrapperX, wrapperY) on
    // a black background and returns the wrapper's baked texture, rows in top-left draw order.
    // With a camera zoom the wrapper is Size / zoom units wide, so its texture is still Size pixels.
    Color[] Capture(GraphicalUiElement scene, float wrapperX = 0, float wrapperY = 0, float zoom = 1)
    {
        ContainerRuntime wrapper = new()
        {
            X = wrapperX, Y = wrapperY, Width = Size / zoom, Height = Size / zoom, IsRenderTarget = true,
        };
        wrapper.AddChild(SolidRect(0, 0, Size / zoom, Size / zoom, Color.Black));
        wrapper.AddChild(scene);

        GumService.Default.Root.Children.Add(wrapper);
        GumService.Default.Root.UpdateLayout();

        Renderer.Self.Camera.Zoom = zoom;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                BeginDrawing();
                ClearBackground(Color.Black);
                GumService.Default.Draw();
                EndDrawing();
            }
        }
        finally
        {
            Renderer.Self.Camera.Zoom = 1;
        }

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(wrapper)!.Value;
        renderTexture.Texture.Width.ShouldBe(Size);
        renderTexture.Texture.Height.ShouldBe(Size);

        Color[] pixels = new Color[Size * Size];
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            // A render texture is stored bottom-up in GL, so the image's rows are flipped.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[(y * Size) + x] = GetImageColor(image, x, Size - 1 - y);
                }
            }
        }
        finally
        {
            UnloadImage(image);
        }

        GumService.Default.Root.Children.Clear();
        return pixels;
    }
}
