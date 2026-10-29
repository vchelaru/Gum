using System;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

public class ShaderParameterMarkerTests
{
    [Fact]
    public void Format_Scalar_IsTheParameterName()
    {
        new ShaderParameterMarker("Thickness").Format().ShouldBe("Thickness");
    }

    [Theory]
    [InlineData(0, 4, "OutlineColor#0/4")]
    [InlineData(3, 4, "OutlineColor#3/4")]
    [InlineData(1, 2, "OutlineColor#1/2")]
    public void Format_Component_AppendsIndexAndCount(int index, int count, string expected)
    {
        new ShaderParameterMarker("OutlineColor", index, count).Format().ShouldBe(expected);
    }

    [Fact]
    public void Scalar_HasNoComponent()
    {
        ShaderParameterMarker marker = new ShaderParameterMarker("Thickness");

        marker.Parameter.ShouldBe("Thickness");
        marker.IsComponent.ShouldBeFalse();
        marker.ComponentIndex.ShouldBe(-1);
        marker.ComponentCount.ShouldBe(0);
    }

    [Fact]
    public void Component_ExposesIndexAndCount()
    {
        ShaderParameterMarker marker = new ShaderParameterMarker("Tint", 2, 3);

        marker.Parameter.ShouldBe("Tint");
        marker.IsComponent.ShouldBeTrue();
        marker.ComponentIndex.ShouldBe(2);
        marker.ComponentCount.ShouldBe(3);
    }

    [Theory]
    [InlineData("Thickness", "Thickness", -1, 0)]
    [InlineData("_Scale2", "_Scale2", -1, 0)]
    [InlineData("OutlineColor#1/4", "OutlineColor", 1, 4)]
    [InlineData("Tint#0/2", "Tint", 0, 2)]
    [InlineData("Tint_#3/4", "Tint_", 3, 4)]
    public void TryParse_ValidMarker_RecoversEveryPart(string text, string parameter, int index, int count)
    {
        bool parsed = ShaderParameterMarker.TryParse(text, out ShaderParameterMarker marker);

        parsed.ShouldBeTrue();
        marker.Parameter.ShouldBe(parameter);
        marker.ComponentIndex.ShouldBe(index);
        marker.ComponentCount.ShouldBe(count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#1/4")]
    [InlineData("Tint#")]
    [InlineData("Tint#1")]
    [InlineData("Tint#1/")]
    [InlineData("Tint#/4")]
    [InlineData("Tint#4/4")]
    [InlineData("Tint#-1/4")]
    [InlineData("Tint#0/1")]
    [InlineData("Tint#0/5")]
    [InlineData("Tint#a/4")]
    [InlineData("Tint#+1/4")]
    [InlineData("Tint#1/4/5")]
    [InlineData("Tint#1/4#2/4")]
    [InlineData("Out line")]
    [InlineData("Tint__X")]
    [InlineData("Tint #1/4")]
    public void TryParse_MalformedMarker_ReturnsFalse(string? text)
    {
        ShaderParameterMarker.TryParse(text, out ShaderParameterMarker marker).ShouldBeFalse();
        marker.Parameter.ShouldBe("");
    }

    [Theory]
    [InlineData("Thickness")]
    [InlineData("OutlineColor#2/4")]
    [InlineData("_Tint#0/3")]
    public void FormatThenTryParse_RoundTrips(string text)
    {
        ShaderParameterMarker.TryParse(text, out ShaderParameterMarker marker).ShouldBeTrue();

        marker.Format().ShouldBe(text);
    }

    [Fact]
    public void Equality_SameParts_AreEqual()
    {
        new ShaderParameterMarker("Tint", 1, 4).ShouldBe(new ShaderParameterMarker("Tint", 1, 4));
        new ShaderParameterMarker("Tint", 1, 4).ShouldNotBe(new ShaderParameterMarker("Tint", 2, 4));
        new ShaderParameterMarker("Tint", 1, 4).ShouldNotBe(new ShaderParameterMarker("Tint", 1, 3));
        new ShaderParameterMarker("Tint").ShouldNotBe(new ShaderParameterMarker("Tint", 0, 2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Out line")]
    [InlineData("Out#line")]
    [InlineData("Tint__X")]
    public void Constructor_UnsupportedParameterName_Throws(string parameter)
    {
        Should.Throw<ArgumentException>(() => new ShaderParameterMarker(parameter));
    }

    [Theory]
    [InlineData(-1, 4)]
    [InlineData(4, 4)]
    [InlineData(0, 1)]
    [InlineData(0, 5)]
    public void Constructor_ComponentOutOfRange_Throws(int index, int count)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ShaderParameterMarker("Tint", index, count));
    }
}
