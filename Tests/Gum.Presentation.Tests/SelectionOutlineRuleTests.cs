using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe.Editors;
using Shouldly;

namespace Gum.Presentation.Tests;

public class SelectionOutlineRuleTests : BaseTestClass
{
    private ComponentSave AddComponent(string name, string? baseType = null, params string[] hidden)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.VariablesHiddenFromInstances.AddRange(hidden);
        ObjectFinder.Self.GumProjectSave ??= new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);
        return component;
    }

    private InstanceSave AddInstance(string baseType, bool locked = false)
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" + ObjectFinder.Self.GumProjectSave!.Screens.Count };
        InstanceSave instance = new InstanceSave { Name = "inst", BaseType = baseType, Locked = locked, ParentContainer = screen };
        screen.Instances.Add(instance);
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);
        return instance;
    }

    private static bool ShouldShow(params object?[] tags) =>
        SelectionOutlineRule.ShouldShow(tags, ObjectFinder.Self);

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenNeitherSizeIsHidden()
    {
        AddComponent("Plain");
        ShouldShow(AddInstance("Plain")).ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenOnlyOtherVariablesAreHidden()
    {
        AddComponent("Other", hidden: new[] { "X", "Rotation" });
        ShouldShow(AddInstance("Other")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void ShouldShow_ShouldBeTrue_WhenOneAxisIsHidden(string hidden)
    {
        AddComponent("C", hidden: hidden);
        ShouldShow(AddInstance("C")).ShouldBeTrue();
    }

    [Fact]
    public void ShouldShow_ShouldBeTrue_WhenBothAxesAreHidden()
    {
        AddComponent("C", hidden: new[] { "Width", "Height" });
        ShouldShow(AddInstance("C")).ShouldBeTrue();
    }

    [Fact]
    public void ShouldShow_ShouldBeTrue_WhenSizeIsHiddenOnABaseType()
    {
        AddComponent("Base", hidden: "Width");
        AddComponent("Derived", baseType: "Base");
        ShouldShow(AddInstance("Derived")).ShouldBeTrue();
    }

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenSizeIsHiddenOnlyOnADerivedType()
    {
        AddComponent("Base");
        AddComponent("Derived", baseType: "Base", hidden: "Width");
        ShouldShow(AddInstance("Base")).ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenInstanceIsLocked()
    {
        AddComponent("C", hidden: new[] { "Width", "Height" });
        ShouldShow(AddInstance("C", locked: true)).ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenSeveralAreSelected()
    {
        AddComponent("C", hidden: new[] { "Width", "Height" });
        ShouldShow(AddInstance("C"), AddInstance("C")).ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenNothingIsSelected()
    {
        ShouldShow().ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ShouldBeFalse_WhenSelectionIsAnElement()
    {
        ComponentSave component = AddComponent("C", hidden: new[] { "Width", "Height" });
        ShouldShow(component).ShouldBeFalse();
    }
}
