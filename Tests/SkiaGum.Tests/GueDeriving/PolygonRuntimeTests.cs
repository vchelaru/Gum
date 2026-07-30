using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Numerics;

namespace SkiaGum.Tests.GueDeriving;

// These tests pin down the post-unification defaults of PolygonRuntime on the Skia
// backend (issue #2757). After #2757 lands, the canonical PolygonRuntime source lives
// in MonoGameGum/GueDeriving/PolygonRuntime.cs and is file-linked into SkiaGum.csproj;
// Skia inherits SkiaShapeRuntime so it picks up the shared color/stroke API.
public class PolygonRuntimeTests
{
    public PolygonRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // ---- Dispatcher routing pins (issue #3639 / ADR 0011) -----------------------------------
    // These drive the STRING property name through the production Skia dispatcher (via
    // SetProperty) and assert the value lands on PolygonRuntime (inherited from
    // SkiaShapeRuntime, the same runtime family as Circle/Rectangle/Arc). Mirrors those shapes'
    // existing TrySetPropertyOnRuntime routing rather than Sprite/NineSlice's hand-written cases.

    [Fact]
    public void Dispatch_Alpha_RoutesToRuntime()
    {
        PolygonRuntime sut = new();

        sut.SetProperty("Alpha", 128);

        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Dispatch_RedGreenBlue_RouteToRuntime()
    {
        PolygonRuntime sut = new();

        sut.SetProperty("Red", 10);
        sut.SetProperty("Green", 20);
        sut.SetProperty("Blue", 30);

        sut.Red.ShouldBe(10);
        sut.Green.ShouldBe(20);
        sut.Blue.ShouldBe(30);
    }

    [Fact]
    public void Dispatch_FillColor_RoutesToRuntime()
    {
        PolygonRuntime sut = new();

        sut.SetProperty("FillColor", SKColors.CornflowerBlue);

        sut.FillColor.ShouldBe(SKColors.CornflowerBlue);
    }

    [Fact]
    public void Dispatch_StrokeWidth_RoutesToRuntime()
    {
        PolygonRuntime sut = new();

        sut.SetProperty("StrokeWidth", 3f);

        sut.StrokeWidth.ShouldBe(3f);
    }

    [Fact]
    public void ContainedRenderable_ShouldBePolygon()
    {
        PolygonRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<Polygon>();
    }

    [Fact]
    public void IsClosed_ShouldBeTrue_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.IsClosed.ShouldBeTrue();
    }

    // Pre-unification bug: the old SkiaGum PolygonRuntime.IsClosed setter wrote `false`
    // literally instead of `value`. Fixed on convergence (#2757).
    [Fact]
    public void IsClosed_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.IsClosed = false;
        sut.IsClosed.ShouldBeFalse();
        sut.IsClosed = true;
        sut.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPolygonHasFewerThanThreePoints()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[] { new Vector2(0, 0), new Vector2(10, 10) });
        sut.IsPointInside(5, 5).ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnTrue_WhenPointIsInsidePolygon()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[]
        {
            new Vector2(0, 0),
            new Vector2(0, 32),
            new Vector2(32, 32),
            new Vector2(32, 0),
        });
        sut.IsPointInside(16, 16).ShouldBeTrue();
    }

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPointIsOutsidePolygon()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[]
        {
            new Vector2(0, 0),
            new Vector2(0, 32),
            new Vector2(32, 32),
            new Vector2(32, 0),
        });
        sut.IsPointInside(50, 50).ShouldBeFalse();
    }

    [Fact]
    public void SetPoints_ShouldUpdatePoints()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[]
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6),
        });
        sut.Points.Count.ShouldBe(3);
        sut.Points[0].ShouldBe(new SKPoint(1, 2));
        sut.Points[2].ShouldBe(new SKPoint(5, 6));
    }

    [Fact]
    public void InsertPointAt_ShouldInsertPoint()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[] { new Vector2(0, 0), new Vector2(10, 10) });
        sut.InsertPointAt(new Vector2(5, 5), 1);
        sut.Points.Count.ShouldBe(3);
        sut.Points[1].ShouldBe(new SKPoint(5, 5));
    }

    [Fact]
    public void RemovePointAtIndex_ShouldRemovePoint()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[] { new Vector2(0, 0), new Vector2(5, 5), new Vector2(10, 10) });
        sut.RemovePointAtIndex(1);
        sut.Points.Count.ShouldBe(2);
        sut.Points[1].ShouldBe(new SKPoint(10, 10));
    }

    [Fact]
    public void SetPointAt_ShouldReplacePoint()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[] { new Vector2(0, 0), new Vector2(5, 5), new Vector2(10, 10) });
        sut.SetPointAt(new Vector2(99, 99), 1);
        sut.Points[1].ShouldBe(new SKPoint(99, 99));
    }

    [Fact]
    public void StrokeColor_ShouldBeWhite_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.StrokeColor.ShouldBe(SKColors.White);
    }

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    // StrokeDashLength + StrokeGapLength on Skia come from SkiaShapeRuntime (inherited);
    // RenderableShapeBase's cached paint engages SKPathEffect.CreateDash when both lengths
    // are positive. These tests pin the round-trip — the dashing visual is exercised by the
    // gallery rather than asserted here.
    [Fact]
    public void StrokeDashLength_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.StrokeDashLength = 4;
        sut.StrokeDashLength.ShouldBe(4);
    }

    [Fact]
    public void StrokeGapLength_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.StrokeGapLength = 2;
        sut.StrokeGapLength.ShouldBe(2);
    }
}
