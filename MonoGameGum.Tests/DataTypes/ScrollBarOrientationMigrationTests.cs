using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// A ScrollBar authored with OrientationCategoryState = Horizontal before ScrollBarBehavior had
/// an Orientation property keeps its orientation: load gives it the matching Orientation value.
/// </summary>
public class ScrollBarOrientationMigrationTests
{
    [Fact]
    public void Migrate_InstanceWithHorizontalState_AddsHorizontalOrientation()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: true);
        StateSave screenState = AddScreenWithInstance(project, "ScrollBarInstance", "Controls/ScrollBar");
        screenState.Variables.Add(StateVariable("ScrollBarInstance.OrientationCategoryState", "Horizontal"));

        bool didChange = project.MigrateScrollBarOrientationStateToProperty();

        didChange.ShouldBeTrue();
        VariableSave orientation = screenState.Variables.Single(item => item.Name == "ScrollBarInstance.Orientation");
        orientation.Value.ShouldBe(Orientation.Horizontal);
        orientation.Type.ShouldBe("Orientation");
        orientation.SetsValue.ShouldBeTrue();
    }

    [Fact]
    public void Migrate_InstanceOfComponentDerivedFromScrollBar_AddsHorizontalOrientation()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: true);
        project.Components.Add(new ComponentSave { Name = "Controls/MyScrollBar", BaseType = "Controls/ScrollBar" });
        StateSave screenState = AddScreenWithInstance(project, "Bar", "Controls/MyScrollBar");
        screenState.Variables.Add(StateVariable("Bar.OrientationCategoryState", "Horizontal"));

        project.MigrateScrollBarOrientationStateToProperty();

        screenState.Variables.ShouldContain(item => item.Name == "Bar.Orientation");
    }

    [Fact]
    public void Migrate_ScrollBarComponentDefaultingToHorizontal_AddsElementLevelOrientation()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: true);
        ComponentSave horizontalBar = new ComponentSave { Name = "Controls/HorizontalBar", BaseType = "Controls/ScrollBar" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = horizontalBar };
        defaultState.Variables.Add(StateVariable("OrientationCategoryState", "Horizontal"));
        horizontalBar.States.Add(defaultState);
        project.Components.Add(horizontalBar);

        project.MigrateScrollBarOrientationStateToProperty();

        defaultState.Variables.Single(item => item.Name == "Orientation").Value.ShouldBe(Orientation.Horizontal);
    }

    [Fact]
    public void Migrate_BehaviorWithoutOrientationProperty_ChangesNothing()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: false);
        StateSave screenState = AddScreenWithInstance(project, "ScrollBarInstance", "Controls/ScrollBar");
        screenState.Variables.Add(StateVariable("ScrollBarInstance.OrientationCategoryState", "Horizontal"));

        bool didChange = project.MigrateScrollBarOrientationStateToProperty();

        didChange.ShouldBeFalse();
        screenState.Variables.ShouldNotContain(item => item.Name == "ScrollBarInstance.Orientation");
    }

    [Fact]
    public void Migrate_VerticalState_ChangesNothing()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: true);
        StateSave screenState = AddScreenWithInstance(project, "ScrollBarInstance", "Controls/ScrollBar");
        screenState.Variables.Add(StateVariable("ScrollBarInstance.OrientationCategoryState", "Vertical"));

        project.MigrateScrollBarOrientationStateToProperty().ShouldBeFalse();
    }

    [Fact]
    public void Migrate_AlreadyAuthoredOrientation_IsKept()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: true);
        StateSave screenState = AddScreenWithInstance(project, "ScrollBarInstance", "Controls/ScrollBar");
        screenState.Variables.Add(StateVariable("ScrollBarInstance.OrientationCategoryState", "Horizontal"));
        screenState.Variables.Add(new VariableSave { Name = "ScrollBarInstance.Orientation", Type = "Orientation", Value = Orientation.Vertical, SetsValue = true });

        project.MigrateScrollBarOrientationStateToProperty().ShouldBeFalse();

        screenState.Variables.Single(item => item.Name == "ScrollBarInstance.Orientation").Value.ShouldBe(Orientation.Vertical);
    }

    [Fact]
    public void Migrate_InstanceOfAnotherComponent_ChangesNothing()
    {
        GumProjectSave project = CreateProject(behaviorHasOrientation: true);
        project.Components.Add(new ComponentSave { Name = "Controls/Other", BaseType = "Container" });
        StateSave screenState = AddScreenWithInstance(project, "OtherInstance", "Controls/Other");
        screenState.Variables.Add(StateVariable("OtherInstance.OrientationCategoryState", "Horizontal"));

        project.MigrateScrollBarOrientationStateToProperty().ShouldBeFalse();
    }

    private static VariableSave StateVariable(string name, string value) =>
        new VariableSave { Name = name, Type = "OrientationCategory", Value = value, SetsValue = true };

    private static GumProjectSave CreateProject(bool behaviorHasOrientation)
    {
        GumProjectSave project = new GumProjectSave();
        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ScrollBarBehaviorName };
        if (behaviorHasOrientation)
        {
            behavior.FormsProperties.Add(new VariableSave { Name = "Orientation", Type = "Orientation", Value = "Vertical" });
        }
        project.Behaviors.Add(behavior);
        ComponentSave scrollBar = new ComponentSave { Name = "Controls/ScrollBar", BaseType = "Container" };
        scrollBar.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ScrollBarBehaviorName });
        project.Components.Add(scrollBar);
        return project;
    }

    private static StateSave AddScreenWithInstance(GumProjectSave project, string instanceName, string baseType)
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = baseType, ParentContainer = screen });
        project.Screens.Add(screen);
        return state;
    }
}
