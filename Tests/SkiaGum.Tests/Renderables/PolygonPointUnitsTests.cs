using Gum.Converters;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// PointXUnits / PointYUnits on the Skia polygon (#6537). Points are measured against the polygon's own bounds, the
/// same way an X/Y position is measured against its parent. Units without a meaning for a polygon point are reported
/// through PropertyAssignmentError and leave the previous unit in place, so drawing never throws.
/// </summary>
public class PolygonPointUnitsTests : IDisposable
{
    readonly List<string> _reported = new List<string>();
    readonly Action<string> _handler;

    public PolygonPointUnitsTests()
    {
        _handler = message => _reported.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += _handler;
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= _handler;
    }

    // Bounds are (20, 20) with size 100 x 60. Every case describes the same square: local x 10..30, local y 12..30,
    // so absolute x 30..50, y 32..50.
    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall, 10f, 30f, GeneralUnitType.PixelsFromSmall, 12f, 30f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, -90f, -70f, GeneralUnitType.PixelsFromLarge, -48f, -30f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, -40f, -20f, GeneralUnitType.PixelsFromMiddle, -18f, 0f)]
    [InlineData(GeneralUnitType.Percentage, 10f, 30f, GeneralUnitType.Percentage, 20f, 50f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, -90f, -70f, GeneralUnitType.Percentage, 20f, 50f)]
    public void DrawBound_PointUnits_PlacePointsRelativeToBounds(GeneralUnitType xUnits, float left, float right,
        GeneralUnitType yUnits, float top, float bottom)
    {
        Polygon polygon = CreateFilledPolygon(new[]
        {
            new SKPoint(left, top),
            new SKPoint(right, top),
            new SKPoint(right, bottom),
            new SKPoint(left, bottom),
        });
        polygon.PointXUnits = xUnits;
        polygon.PointYUnits = yUnits;

        using SKBitmap bitmap = Draw(polygon, new SKRect(20, 20, 120, 80));

        bitmap.GetPixel(40, 41).ShouldBe(SKColors.Red);
        bitmap.GetPixel(25, 41).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(60, 41).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(40, 27).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(40, 55).Alpha.ShouldBe((byte)0);
        _reported.ShouldBeEmpty();
    }

    [Fact]
    public void DrawBound_IsClosed_ClosesToConvertedFirstPoint()
    {
        // In 100 x 60 bounds these are (50, 60), (100, 60), (100, 30). Closing to the unconverted first point
        // (50, 100) would fill a wedge below y = 60.
        Polygon polygon = CreateFilledPolygon(new[]
        {
            new SKPoint(50, 100),
            new SKPoint(100, 100),
            new SKPoint(100, 50),
        });
        polygon.PointXUnits = GeneralUnitType.Percentage;
        polygon.PointYUnits = GeneralUnitType.Percentage;

        using SKBitmap bitmap = Draw(polygon, new SKRect(0, 0, 100, 60));

        bitmap.GetPixel(90, 55).ShouldBe(SKColors.Red);
        bitmap.GetPixel(60, 75).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void IsPointInside_PixelsFromLarge_MeasuresFromRightAndBottom()
    {
        Polygon polygon = CreateFilledPolygon(new[]
        {
            new SKPoint(-20, -20),
            new SKPoint(0, -20),
            new SKPoint(0, 0),
            new SKPoint(-20, 0),
        });
        polygon.Width = 100;
        polygon.Height = 60;
        polygon.PointXUnits = GeneralUnitType.PixelsFromLarge;
        polygon.PointYUnits = GeneralUnitType.PixelsFromLarge;

        polygon.IsPointInside(90, 50).ShouldBeTrue();
        polygon.IsPointInside(10, 10).ShouldBeFalse();
    }

    [Theory]
    [InlineData(GeneralUnitType.PercentageOfFile)]
    [InlineData(GeneralUnitType.PercentageOfOtherDimension)]
    [InlineData(GeneralUnitType.MaintainFileAspectRatio)]
    [InlineData(GeneralUnitType.PixelsFromBaseline)]
#pragma warning disable CS0618 // obsolete, still rejected
    [InlineData(GeneralUnitType.PixelsFromMiddleInverted)]
#pragma warning restore CS0618
    public void PointUnits_WithoutMeaningForPoints_AreReportedAndKeepPreviousUnit(GeneralUnitType unit)
    {
        Polygon polygon = CreateFilledPolygon(new[]
        {
            new SKPoint(10, 12),
            new SKPoint(30, 12),
            new SKPoint(30, 30),
            new SKPoint(10, 30),
        });
        polygon.PointXUnits = GeneralUnitType.Percentage;
        polygon.PointYUnits = GeneralUnitType.Percentage;

        polygon.PointXUnits = unit;
        polygon.PointYUnits = unit;

        polygon.PointXUnits.ShouldBe(GeneralUnitType.Percentage);
        polygon.PointYUnits.ShouldBe(GeneralUnitType.Percentage);
        _reported.Count.ShouldBe(2);
        _reported[0].ShouldContain(nameof(Polygon.PointXUnits));
        _reported[0].ShouldContain(unit.ToString());
        _reported[1].ShouldContain(nameof(Polygon.PointYUnits));
        Should.NotThrow(() => Draw(polygon, new SKRect(0, 0, 100, 60)).Dispose());
    }

    private static Polygon CreateFilledPolygon(SKPoint[] points)
    {
        Polygon polygon = new Polygon();
        polygon.Points = new List<SKPoint>(points);
        polygon.IsFilled = true;
        polygon.IsAntialiased = false;
        polygon.Color = SKColors.Red;
        return polygon;
    }

    private static SKBitmap Draw(Polygon polygon, SKRect bounds)
    {
        SKBitmap bitmap = new SKBitmap(new SKImageInfo(200, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        polygon.DrawBound(bounds, canvas, 0);
        canvas.Flush();
        return bitmap;
    }
}
