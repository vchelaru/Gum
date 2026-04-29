using Gum.DataTypes;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

public class ResolveRadiusTests
{
    [Fact]
    public void Absolute_ShouldReturnValue_Unchanged()
    {
        RenderableShapeBase.ResolveRadius(value: 25f, DimensionUnitType.Absolute, width: 200f)
            .ShouldBe(25f);
    }

    [Fact]
    public void PercentageOfParent_ShouldReturn_PercentageOfWidth()
    {
        RenderableShapeBase.ResolveRadius(value: 100f, DimensionUnitType.PercentageOfParent, width: 200f)
            .ShouldBe(200f);

        RenderableShapeBase.ResolveRadius(value: 50f, DimensionUnitType.PercentageOfParent, width: 200f)
            .ShouldBe(100f);
    }

    [Fact]
    public void RelativeToParent_ShouldReturn_WidthPlusValue()
    {
        RenderableShapeBase.ResolveRadius(value: 0f, DimensionUnitType.RelativeToParent, width: 200f)
            .ShouldBe(200f);

        RenderableShapeBase.ResolveRadius(value: -10f, DimensionUnitType.RelativeToParent, width: 200f)
            .ShouldBe(190f);
    }

    [Fact]
    public void UnsupportedUnit_ShouldFallBack_ToValueUnchanged()
    {
        RenderableShapeBase.ResolveRadius(value: 25f, DimensionUnitType.Ratio, width: 200f)
            .ShouldBe(25f);

        RenderableShapeBase.ResolveRadius(value: 25f, DimensionUnitType.RelativeToChildren, width: 200f)
            .ShouldBe(25f);
    }
}
