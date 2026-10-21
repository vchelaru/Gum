using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins what the runtime does with a Forms property (IsChecked, ToolTip, LargeChange, ...) authored in
/// a category state of a component or screen: <see cref="GraphicalUiElement.ApplyState(StateSave)"/> hands
/// a name nothing on the visual claims to the wrapped Forms control by reflection, so the value reaches
/// the control. Generated code must do the same (#6334).
/// </summary>
public class FormsPropertyInStateTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static StateSave AddCategoryState(ElementSave element, string name, params (string Variable, string Type, object Value)[] values)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave state = new StateSave { Name = name, ParentContainer = element };
        foreach ((string variable, string type, object value) in values)
        {
            state.Variables.Add(new VariableSave { Name = variable, Type = type, Value = value, SetsValue = true });
        }
        category.States.Add(state);
        element.Categories.Add(category);
        return state;
    }

    [Fact]
    public void ApplyState_ComponentStateAuthoringIsChecked_SetsTheControl()
    {
        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        StateSave state = AddCategoryState(component, "On", ("IsChecked", "bool", true));
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        CheckBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.ElementSave = component;
        CheckBox checkBox = new(visual);
        checkBox.IsChecked.ShouldBe(false);

        visual.ApplyState(state);

        checkBox.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void ApplyState_ComponentStateAuthoringToolTip_SetsTheControl()
    {
        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        StateSave state = AddCategoryState(component, "On", ("ToolTip", "string", "in state"));
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        CheckBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.ElementSave = component;
        CheckBox checkBox = new(visual);

        visual.ApplyState(state);

        checkBox.ToolTip.ShouldBe("in state");
    }

    [Fact]
    public void ApplyState_ComponentStateAuthoringLargeChange_SetsTheScrollViewer()
    {
        ComponentSave component = new ComponentSave { Name = "Controls/ScrollViewer", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        StateSave state = AddCategoryState(component, "Big", ("LargeChange", "double", 33.0));
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        ScrollViewerVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.ElementSave = component;
        ScrollViewer scrollViewer = new(visual);

        visual.ApplyState(state);

        scrollViewer.LargeChange.ShouldBe(33);
    }

    [Fact]
    public void ApplyState_ScreenStateAuthoringInstanceIsChecked_SetsTheInstanceControl()
    {
        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        StateSave state = AddCategoryState(screen, "On", ("Chk.IsChecked", "bool", true));
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        ContainerRuntime screenVisual = new ContainerRuntime();
        screenVisual.ElementSave = screen;
        CheckBoxVisual chkVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        chkVisual.Name = "Chk";
        chkVisual.ElementSave = component;
        screenVisual.Children.Add(chkVisual);
        CheckBox checkBox = new(chkVisual);

        screenVisual.ApplyState(state);

        checkBox.IsChecked.ShouldBe(true);
    }
}
