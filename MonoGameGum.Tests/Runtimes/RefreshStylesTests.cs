using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class RefreshStylesTests : BaseTestClass
{
    [Fact]
    public void RefreshStyles_ShouldReapplyDefaultStateValues()
    {
        // Arrange
        ColoredRectangleRuntime rectangle = new();
        float originalWidth = 100;
        float updatedWidth = 200;

        StateSave defaultState = new();
        defaultState.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = originalWidth,
            SetsValue = true
        });

        rectangle.AddStates(new System.Collections.Generic.List<StateSave> { defaultState });
        defaultState.Name = "Default";
        rectangle.Tag = null;
        rectangle.ElementSave = new ScreenSave();
        rectangle.ElementSave.States.Add(defaultState);
        rectangle.ElementSave.Name = "TestScreen";

        // Apply initial state
        rectangle.ApplyState(defaultState);
        rectangle.Width.ShouldBe(originalWidth);

        // Act — modify the variable value, then refresh
        defaultState.Variables[0].Value = updatedWidth;
        rectangle.RefreshStyles();

        // Assert
        rectangle.Width.ShouldBe(updatedWidth);
    }

    [Fact]
    public void RefreshStyles_ShouldRecurseIntoChildren()
    {
        // Arrange
        ContainerRuntime parent = new();
        ColoredRectangleRuntime child = new();
        child.Parent = parent;

        float originalWidth = 50;
        float updatedWidth = 150;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "ParentScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        StateSave childDefault = new();
        childDefault.Name = "Default";
        childDefault.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = originalWidth,
            SetsValue = true
        });
        ComponentSave childElement = new();
        childElement.Name = "ChildComponent";
        childElement.States.Add(childDefault);
        child.AddStates(new System.Collections.Generic.List<StateSave> { childDefault });
        child.ElementSave = childElement;

        // Apply initial state on child
        child.ApplyState(childDefault);
        child.Width.ShouldBe(originalWidth);

        // Act — modify child's variable, refresh from parent
        childDefault.Variables[0].Value = updatedWidth;
        parent.RefreshStyles();

        // Assert — child should have picked up the new value
        child.Width.ShouldBe(updatedWidth);
    }

    [Fact]
    public void RefreshStyles_ShouldReapplyParentInstanceQualifiedVariables()
    {
        // Arrange
        ContainerRuntime parent = new();
        ColoredRectangleRuntime child = new();
        child.Name = "ChildRect";
        child.Parent = parent;

        float originalWidth = 100;
        float overriddenWidth = 300;
        float updatedOverrideWidth = 400;

        // Child's own default state
        StateSave childDefault = new();
        childDefault.Name = "Default";
        childDefault.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = originalWidth,
            SetsValue = true
        });
        ComponentSave childElement = new();
        childElement.Name = "ChildComp";
        childElement.States.Add(childDefault);
        child.AddStates(new System.Collections.Generic.List<StateSave> { childDefault });
        child.ElementSave = childElement;

        // Parent's default state with instance-qualified variable
        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        parentDefault.Variables.Add(new VariableSave
        {
            Name = "ChildRect.Width",
            Value = overriddenWidth,
            SetsValue = true
        });
        ScreenSave parentElement = new();
        parentElement.Name = "ParentScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        // Apply initial states
        child.ApplyState(childDefault);
        parent.ApplyState(parentDefault);
        child.Width.ShouldBe(overriddenWidth);

        // Act — modify parent's override, refresh from parent
        parentDefault.Variables[0].Value = updatedOverrideWidth;
        parent.RefreshStyles();

        // Assert — child should have the updated override
        child.Width.ShouldBe(updatedOverrideWidth);
    }

    [Fact]
    public void RefreshStyles_ShouldReapplyFormsControlCategoricalState()
    {
        // Arrange
        Button button = new();
        InteractiveGue visual = button.Visual;

        // The button is in "Enabled" state by default
        // Find the Enabled state's background color variable
        StateSaveCategory buttonCategory = visual.Categories["ButtonCategory"];
        StateSave enabledState = buttonCategory.States
            .First(s => s.Name == FrameworkElement.EnabledStateName);

        // Get the current background color variable
        VariableSave backgroundColorVar = enabledState.Variables
            .First(v => v.Name == "ButtonBackground.Color");

        // Record the original color applied to the background child
        ColoredRectangleRuntime background = (ColoredRectangleRuntime)visual.GetGraphicalUiElementByName("ButtonBackground")!;

        // Change the variable to a new color
        Color newColor = Color.Red;
        backgroundColorVar.Value = newColor;

        // Act
        visual.RefreshStyles();

        // Assert — the background should now show the new color
        background.Color.ShouldBe(newColor);
    }

    [Fact]
    public void RefreshStyles_ShouldRecurseThroughFormsControlChildren()
    {
        // Arrange — a container with a button inside
        ContainerRuntime parent = new();
        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        Button button = new();
        button.Visual.Parent = parent;

        InteractiveGue visual = button.Visual;
        StateSaveCategory buttonCategory = visual.Categories["ButtonCategory"];
        StateSave enabledState = buttonCategory.States
            .First(s => s.Name == FrameworkElement.EnabledStateName);

        VariableSave backgroundColorVar = enabledState.Variables
            .First(v => v.Name == "ButtonBackground.Color");

        ColoredRectangleRuntime background = (ColoredRectangleRuntime)visual.GetGraphicalUiElementByName("ButtonBackground")!;

        Color newColor = Color.Green;
        backgroundColorVar.Value = newColor;

        // Act — refresh from the parent, not the button directly
        parent.RefreshStyles();

        // Assert — the button's background should have the new color
        background.Color.ShouldBe(newColor);
    }
}
