using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #6066: ToGraphicalUiElement sets the thread-wide GraphicalUiElement.IsAllLayoutSuspended
// for the whole build and used to clear it only on the success path. A build that threw (a
// component constructor, a custom variable setter) left it stuck at true, so every later build
// skipped its own layout flush and no layout was ever applied again.
public class ToGraphicalUiElementLayoutSuspensionTests : BaseTestClass
{
    private class ThrowingRuntime : GraphicalUiElement
    {
        public ThrowingRuntime() : base(new InvisibleRenderable(), null)
        {
            throw new InvalidOperationException("Component construction failed.");
        }
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        base.Dispose();
    }

    private static StandardElementSave CreateStandard(string name)
    {
        StandardElementSave standard = new() { Name = name };
        standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
        return standard;
    }

    private static ScreenSave CreateScreenWithInstance(string screenName, string instanceName, string baseType)
    {
        ScreenSave screen = new() { Name = screenName };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave
        {
            Name = instanceName,
            BaseType = baseType,
            ParentContainer = screen
        });
        return screen;
    }

    [Fact]
    public void ToGraphicalUiElement_ShouldNotLeaveLayoutSuspended_WhenBuildThrows()
    {
        ComponentSave thrower = new() { Name = "Thrower" };
        thrower.States.Add(new StateSave { Name = "Default", ParentContainer = thrower });
        ElementSaveExtensions.RegisterGueInstantiation("Thrower", () => new ThrowingRuntime());

        GumProjectSave gumProject = new();
        gumProject.StandardElements.Add(CreateStandard("Container"));
        gumProject.Components.Add(thrower);
        ScreenSave screen = CreateScreenWithInstance("BrokenScreen", "Bad", "Thrower");
        gumProject.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = gumProject;

        Should.Throw<Exception>(() => screen.ToGraphicalUiElement());

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBeFalse();
    }

    [Fact]
    public void ToGraphicalUiElement_ShouldApplyLayout_WhenPreviousBuildThrew()
    {
        ComponentSave thrower = new() { Name = "Thrower" };
        thrower.States.Add(new StateSave { Name = "Default", ParentContainer = thrower });
        ElementSaveExtensions.RegisterGueInstantiation("Thrower", () => new ThrowingRuntime());

        GumProjectSave gumProject = new();
        StandardElementSave container = CreateStandard("Container");
        gumProject.StandardElements.Add(container);
        gumProject.Components.Add(thrower);
        ScreenSave brokenScreen = CreateScreenWithInstance("BrokenScreen", "Bad", "Thrower");
        gumProject.Screens.Add(brokenScreen);

        ScreenSave goodScreen = CreateScreenWithInstance("GoodScreen", "Outer", "Container");
        goodScreen.Instances.Add(new InstanceSave
        {
            Name = "Box",
            BaseType = "Container",
            ParentContainer = goodScreen
        });
        List<VariableSave> variables = goodScreen.DefaultState.Variables;
        variables.Add(new VariableSave { Name = "Outer.Width", Type = "float", Value = 200f });
        variables.Add(new VariableSave { Name = "Box.Parent", Type = "string", Value = "Outer" });
        variables.Add(new VariableSave { Name = "Box.WidthUnits", Type = "DimensionUnitType", Value = DimensionUnitType.PercentageOfParent });
        variables.Add(new VariableSave { Name = "Box.Width", Type = "float", Value = 50f });
        gumProject.Screens.Add(goodScreen);
        ObjectFinder.Self.GumProjectSave = gumProject;

        Should.Throw<Exception>(() => brokenScreen.ToGraphicalUiElement());

        GraphicalUiElement built = goodScreen.ToGraphicalUiElement();

        built.GetGraphicalUiElementByName("Box")!.AbsoluteWidth.ShouldBe(100);
    }
}
