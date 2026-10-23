using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Issue #6611: the obsolete single-color members on CircleRuntime/RectangleRuntime color the
/// outline, as on MonoGame and raylib, and leave the fill alone. SetProperty is how a loaded
/// project applies them.
/// </summary>
public class ShapeRuntimeObsoleteColorTests
{
#pragma warning disable CS0618 // pins the obsolete members
    [Theory]
    [InlineData("Circle", false)]
    [InlineData("Rectangle", false)]
    [InlineData("Circle", true)]
    [InlineData("Rectangle", true)]
    public void ObsoleteChannels_WriteStrokeNotFill(string shape, bool throughSetProperty)
    {
        SkiaShapeRuntime sut = shape == "Circle" ? new CircleRuntime() : new RectangleRuntime();
        sut.IsFilled = true;
        sut.FillColor = new SKColor(1, 2, 3, 4);
        sut.StrokeColor = SKColors.White;

        if (throughSetProperty)
        {
            sut.SetProperty("Red", 10);
            sut.SetProperty("Green", 20);
            sut.SetProperty("Blue", 30);
            sut.SetProperty("Alpha", 40);
        }
        else if (sut is CircleRuntime circle)
        {
            circle.Red = 10;
            circle.Green = 20;
            circle.Blue = 30;
            circle.Alpha = 40;
        }
        else
        {
            RectangleRuntime rectangle = (RectangleRuntime)sut;
            rectangle.Red = 10;
            rectangle.Green = 20;
            rectangle.Blue = 30;
            rectangle.Alpha = 40;
        }

        sut.StrokeColor.ShouldBe(new SKColor(10, 20, 30, 40));
        sut.FillColor.ShouldBe(new SKColor(1, 2, 3, 4));
        ((RenderableShapeBase)sut.RenderableComponent!).Color.ShouldBe(new SKColor(1, 2, 3, 4));
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void ObsoleteChannels_Read_ReturnStroke(string shape)
    {
        SkiaShapeRuntime sut = shape == "Circle" ? new CircleRuntime() : new RectangleRuntime();
        sut.FillColor = new SKColor(1, 2, 3, 4);
        sut.StrokeColor = new SKColor(10, 20, 30, 40);

        int[] channels = sut is CircleRuntime circle
            ? new[] { circle.Red, circle.Green, circle.Blue, circle.Alpha }
            : new[] { ((RectangleRuntime)sut).Red, ((RectangleRuntime)sut).Green, ((RectangleRuntime)sut).Blue, ((RectangleRuntime)sut).Alpha };

        channels.ShouldBe(new[] { 10, 20, 30, 40 });
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void ObsoleteColor_WritesStrokeNotFill(string shape)
    {
        SkiaShapeRuntime sut = shape == "Circle" ? new CircleRuntime() : new RectangleRuntime();
        sut.IsFilled = true;
        sut.FillColor = new SKColor(1, 2, 3, 4);

        sut.Color = new SKColor(10, 20, 30, 40);

        sut.StrokeColor.ShouldBe(new SKColor(10, 20, 30, 40));
        sut.Color.ShouldBe(new SKColor(10, 20, 30, 40));
        ((RenderableShapeBase)sut.RenderableComponent!).Color.ShouldBe(new SKColor(1, 2, 3, 4));
    }
#pragma warning restore CS0618
}
