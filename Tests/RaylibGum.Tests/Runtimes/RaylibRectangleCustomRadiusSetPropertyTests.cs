using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// The string-dispatch path (SetProperty) for Rectangle's per-corner radius overrides, which is how a
// saved .gumx applies them (#6983 coverage sweep, mirroring RaylibNineSliceBorderScaleTests).
public class RaylibRectangleCustomRadiusSetPropertyTests : BaseTestClass
{
    [Theory]
    [InlineData("CustomRadiusTopLeft")]
    [InlineData("CustomRadiusTopRight")]
    [InlineData("CustomRadiusBottomLeft")]
    [InlineData("CustomRadiusBottomRight")]
    public void SetProperty_CustomRadius_ShouldLandOnRuntime(string propertyName)
    {
        RectangleRuntime sut = new();

        sut.SetProperty(propertyName, (float?)7f);

        ((float?)typeof(RectangleRuntime).GetProperty(propertyName)!.GetValue(sut)).ShouldBe(7f);
    }

    [Theory]
    [InlineData("CustomRadiusTopLeft")]
    [InlineData("CustomRadiusTopRight")]
    [InlineData("CustomRadiusBottomLeft")]
    [InlineData("CustomRadiusBottomRight")]
    public void SetProperty_CustomRadiusNull_ShouldClearTheOverride(string propertyName)
    {
        RectangleRuntime sut = new();
        sut.SetProperty(propertyName, (float?)7f);

        sut.SetProperty(propertyName, null);

        ((float?)typeof(RectangleRuntime).GetProperty(propertyName)!.GetValue(sut)).ShouldBeNull();
    }

    [Fact]
    public void SetProperty_CustomRadiusZero_ShouldBeKeptAsZeroNotNull()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("CustomRadiusTopLeft", (float?)0f);

        sut.CustomRadiusTopLeft.ShouldBe(0f);
    }
}
