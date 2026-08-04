using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Drawing;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Math.Geometry;

public class FilledStrokedRectangleTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldReturnMoreOpaqueOfFillAndStrokeColor()
    {
        FilledStrokedRectangle sut = new();

        sut.FillColor = Color.FromArgb(100, 255, 255, 255);
        sut.StrokeColor = Color.FromArgb(200, 255, 255, 255);
        sut.Alpha.ShouldBe(200);

        sut.FillColor = Color.FromArgb(200, 255, 255, 255);
        sut.StrokeColor = Color.FromArgb(100, 255, 255, 255);
        sut.Alpha.ShouldBe(200);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        Should.NotThrow(() => new FilledStrokedRectangle());
    }

    [Fact]
    public void FillColor_And_StrokeColor_ShouldBeIndependent()
    {
        FilledStrokedRectangle sut = new()
        {
            FillColor = Color.Red,
            StrokeColor = Color.Blue
        };

        sut.FillColor.ShouldBe(Color.Red);
        sut.StrokeColor.ShouldBe(Color.Blue);
    }

    [Fact]
    public void IsFilled_ShouldDefaultToFalse()
    {
        FilledStrokedRectangle sut = new();
        sut.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_And_StrokeWidth_ShouldGateIndependently()
    {
        // Fill only, no stroke:
        FilledStrokedRectangle fillOnly = new()
        {
            IsFilled = true,
            StrokeWidth = 0
        };
        fillOnly.IsFilled.ShouldBeTrue();
        fillOnly.StrokeWidth.ShouldBe(0);

        // Stroke only, no fill -- setting StrokeWidth must not implicitly turn on IsFilled,
        // and setting IsFilled off above must not have reset StrokeWidth's own default elsewhere.
        FilledStrokedRectangle strokeOnly = new()
        {
            IsFilled = false,
            StrokeWidth = 5
        };
        strokeOnly.IsFilled.ShouldBeFalse();
        strokeOnly.StrokeWidth.ShouldBe(5);
    }

    [Fact]
    public void Parent_ShouldAddSelfToNewParentsChildren()
    {
        FilledStrokedRectangle sut = new();
        LineRectangle parent = new();

        sut.Parent = parent;

        parent.Children.ShouldContain(sut);
    }

    [Fact]
    public void Parent_ShouldRemoveSelfFromOldParentsChildren()
    {
        FilledStrokedRectangle sut = new();
        LineRectangle oldParent = new();
        LineRectangle newParent = new();

        sut.Parent = oldParent;
        sut.Parent = newParent;

        oldParent.Children.ShouldNotContain(sut);
        newParent.Children.ShouldContain(sut);
    }

    [Fact]
    public void StrokeWidth_ShouldDefaultToOne()
    {
        FilledStrokedRectangle sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void Width_And_Height_ShouldRoundTrip()
    {
        FilledStrokedRectangle sut = new()
        {
            Width = 100,
            Height = 50
        };

        sut.Width.ShouldBe(100);
        sut.Height.ShouldBe(50);
    }
}
