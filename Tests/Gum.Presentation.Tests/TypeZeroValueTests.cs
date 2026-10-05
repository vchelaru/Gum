using Gum.PropertyGridHelpers;
using Shouldly;

namespace Gum.Presentation.Tests;

public class TypeZeroValueTests
{
    [Fact]
    public void For_ReturnsATypedZero_ForEachNumericAndBoolType()
    {
        TypeZeroValue.For("float").ShouldBe(0f);
        TypeZeroValue.For("double").ShouldBe(0.0);
        TypeZeroValue.For("int").ShouldBe(0);
        TypeZeroValue.For("bool").ShouldBe(false);
    }

    [Fact]
    public void For_ReturnsNull_ForTypesWithNoZero()
    {
        TypeZeroValue.For("string").ShouldBeNull();
        TypeZeroValue.For("Color").ShouldBeNull();
        TypeZeroValue.For(null).ShouldBeNull();
    }
}
