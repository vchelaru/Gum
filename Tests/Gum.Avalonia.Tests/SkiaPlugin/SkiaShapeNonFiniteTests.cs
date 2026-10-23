using System;
using Shouldly;
using SkiaGum.Renderables;
using Xunit;

namespace Gum.Avalonia.Tests.SkiaPlugin;

/// <summary>
/// The tool's Skia shapes (linked from SkiaGum) reject NaN and infinity on X, Y, Width, Height and Rotation,
/// and a +infinity StrokeWidth, because SkiaPlugin compiles with FULL_DIAGNOSTICS (#6481).
/// </summary>
public class SkiaShapeNonFiniteTests
{
    private static RenderableShapeBase Create(string shape) => shape switch
    {
        "Arc" => new Arc(),
        "Circle" => new Circle(),
        "Line" => new SkiaGum.Renderables.Line(),
        "RoundedRectangle" => new RoundedRectangle(),
        _ => throw new ArgumentException(shape),
    };

    [Theory]
    [InlineData("Arc", "X", float.NaN)]
    [InlineData("Circle", "Y", float.PositiveInfinity)]
    [InlineData("Line", "Width", float.NegativeInfinity)]
    [InlineData("RoundedRectangle", "Height", float.NaN)]
    [InlineData("Circle", "Rotation", float.PositiveInfinity)]
    [InlineData("RoundedRectangle", "StrokeWidth", float.PositiveInfinity)]
    public void NonFiniteValue_IsRejected(string shape, string variable, float value)
    {
        RenderableShapeBase renderable = Create(shape);

        Should.Throw<ArgumentException>(() =>
        {
            switch (variable)
            {
                case "X": renderable.X = value; break;
                case "Y": renderable.Y = value; break;
                case "Width": renderable.Width = value; break;
                case "Height": renderable.Height = value; break;
                case "Rotation": renderable.Rotation = value; break;
                case "StrokeWidth": renderable.StrokeWidth = value; break;
            }
        });
    }
}
