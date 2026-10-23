using Gum.Wireframe;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// A rotated render-target container must look like the same container drawn without a render
/// target: its children turn with it and nothing inside its own rectangle is clipped (#6602, the
/// MonoGame side of Skia's #6588). Text resamples differently when the baked image is turned, so
/// scenes are compared by shape: how many pixels each child covers and where their center is.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RotatedRenderTargetTests : BaseTestClass
{
    const int Size = 240;
    const float ShapeCenterTolerance = 0.5f;
    const float TextCenterTolerance = 1.5f;

    static readonly XnaColor Red = new XnaColor((byte)255, (byte)0, (byte)0, (byte)255);
    static readonly XnaColor Blue = new XnaColor((byte)0, (byte)0, (byte)255, (byte)255);

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
            container.AddChild(FilledRect(0, 40, 20, 20, Blue));
            container.AddChild(new TextRuntime { X = 0, Y = 20, Text = "WW", Color = XnaColor.White });
            parent.AddChild(container);
            return parent;
        }

        XnaColor[] plain = Capture(Build(isRenderTarget: false));
        XnaColor[] viaRenderTarget = Capture(Build(isRenderTarget: true));

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

        XnaColor[] plain = Capture(Build(useRenderTargets: false));
        XnaColor[] viaRenderTarget = Capture(Build(useRenderTargets: true));

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
                XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center,
                YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center,
            };
            container.AddChild(SolidRect(40, 0, 20, 20, Red));
            container.AddChild(SolidRect(0, 40, 20, 20, Blue));
            return container;
        }

        XnaColor[] plain = Capture(Build(isRenderTarget: false));
        XnaColor[] viaRenderTarget = Capture(Build(isRenderTarget: true));

        AssertSameCoverage(plain, viaRenderTarget, IsRed, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
        AssertSameCoverage(plain, viaRenderTarget, IsBlue, minimumCount: 300, ShapeCenterTolerance, countTolerancePercent: 10);
    }

    // An upright render target partly off camera bakes only its visible part, clamped to the camera
    // (#3478), and still lands where the plain container draws. The capture is the viewport, so the
    // camera is Size x Size.
    [Theory]
    [InlineData(-30f, -30f)]
    [InlineData(200f, 200f)]
    public void Draw_UprightRenderTargetPartlyOffCamera_MatchesWithoutRenderTarget(float x, float y)
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

        XnaColor[] plain = Capture(Build(isRenderTarget: false));
        XnaColor[] viaRenderTarget = Capture(Build(isRenderTarget: true));

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

    // The corner rectangle of a container rotated 30 degrees counterclockwise lands above the
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

        XnaColor[] pixels = Capture(container);

        // Local (50, 10), the corner rectangle's center, turned 30 degrees counterclockwise about
        // the container origin (60, 60): (60 + 50cos30 + 10sin30, 60 - 50sin30 + 10cos30).
        IsRed(pixels[(44 * Size) + 108]).ShouldBeTrue($"pixel was {pixels[(44 * Size) + 108]}");
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

    record struct Coverage(int Count, float CenterX, float CenterY);

    static bool IsRed(XnaColor c) => c.R > 150 && c.G < 80 && c.B < 80;

    static bool IsBlue(XnaColor c) => c.B > 150 && c.G < 80 && c.R < 80;

    static bool IsTextColored(XnaColor c) => c.R > 100 && c.G > 100 && c.B > 100;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; it is the SolidRectangle-backed runtime the issue reports.
    static ColoredRectangleRuntime SolidRect(float x, float y, float width, float height, XnaColor color) =>
        new() { X = x, Y = y, Width = width, Height = height, Color = color };
#pragma warning restore CS0618

    static RectangleRuntime FilledRect(float x, float y, float width, float height, XnaColor color) =>
        new() { X = x, Y = y, Width = width, Height = height, IsFilled = true, FillColor = color, StrokeWidth = 0 };

    static void AssertSameCoverage(XnaColor[] expected, XnaColor[] actual, System.Func<XnaColor, bool> predicate,
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

    static Coverage Measure(XnaColor[] pixels, System.Func<XnaColor, bool> predicate)
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

    static XnaColor[] Capture(GraphicalUiElement root)
    {
        using ShapeGameLease game = new ShapeGameLease();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Size * Size];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }
}
