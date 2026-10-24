using System;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// GraphicalUiElement.RunWithAllLayoutSuspended runs the layout deferred on every element the change
/// dirtied, not only under the flush root (#6736).
/// </summary>
public class GlobalSuspensionFlushTests : BaseTestClass
{
    private static ContainerRuntime CreateChildOf300WideParent()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Width = 300;
        ContainerRuntime child = new();
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Width = 10;
        parent.Children!.Add(child);
        return child;
    }

    [Fact]
    public void ExceptionInChange_LaterChangeStillLaysOut()
    {
        ContainerRuntime child = CreateChildOf300WideParent();
        Should.Throw<InvalidOperationException>(() => GraphicalUiElement.RunWithAllLayoutSuspended(
            () => { child.Width = 50; throw new InvalidOperationException(); }, flushRoot: null));

        GraphicalUiElement.RunWithAllLayoutSuspended(() => child.Width = 80, flushRoot: null);

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBeFalse();
        child.AbsoluteWidth.ShouldBe(80);
    }

    [Fact]
    public void CloneMadeDuringChange_LaterChangeLaysOutTheClone()
    {
        ContainerRuntime child = CreateChildOf300WideParent();
        GraphicalUiElement clone = null!;
        GraphicalUiElement.RunWithAllLayoutSuspended(() =>
        {
            child.Width = 50;
            clone = child.Clone();
        }, flushRoot: null);
        child.Parent!.Children!.Add(clone);
        clone.UpdateLayout();

        GraphicalUiElement.RunWithAllLayoutSuspended(() => clone.Width = 80, flushRoot: null);

        clone.AbsoluteWidth.ShouldBe(80);
    }

    [Fact]
    public void ParentlessElementOtherThanTheFlushRoot_LaysOut()
    {
        ContainerRuntime root = new();
        root.WidthUnits = DimensionUnitType.Absolute;
        root.Width = 10;

        GraphicalUiElement.RunWithAllLayoutSuspended(() => root.Width = 80, flushRoot: null);

        root.AbsoluteWidth.ShouldBe(80);
    }
}
