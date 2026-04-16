using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Shouldly;
using System.Linq;
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

    [Fact]
    public void RefreshStyles_ShouldPreserveSliderValue()
    {
        // Arrange
        Slider slider = new();
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 75;

        GraphicalUiElement thumbVisual = slider.Visual
            .GetGraphicalUiElementByName("ThumbInstance")!;
        float thumbXBeforeRefresh = thumbVisual.X;

        // Sanity check — thumb should not be at the default position
        thumbXBeforeRefresh.ShouldBeGreaterThan(0);

        // Act
        slider.Visual.RefreshStyles();

        // Assert — Value and thumb position should be preserved
        slider.Value.ShouldBe(75);
        thumbVisual.X.ShouldBe(thumbXBeforeRefresh);
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveScrollBarValue()
    {
        // Arrange
        ScrollBar scrollBar = new();
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 200;
        scrollBar.Value = 100;

        GraphicalUiElement thumbVisual = scrollBar.Visual
            .GetGraphicalUiElementByName("ThumbInstance")!;
        float thumbYBeforeRefresh = thumbVisual.Y;

        // Act
        scrollBar.Visual.RefreshStyles();

        // Assert — Value and thumb position should be preserved
        scrollBar.Value.ShouldBe(100);
        thumbVisual.Y.ShouldBe(thumbYBeforeRefresh);
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveTextBoxTextAndCaret()
    {
        // Arrange
        TextBox textBox = new();
        textBox.HandleCharEntered('H');
        textBox.HandleCharEntered('i');

        textBox.Text.ShouldBe("Hi");
        int caretBefore = textBox.CaretIndex;
        caretBefore.ShouldBe(2);

        // Act
        textBox.Visual.RefreshStyles();

        // Assert — text and caret should be preserved
        textBox.Text.ShouldBe("Hi");
        textBox.CaretIndex.ShouldBe(caretBefore);
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveTextBoxTextWhenComponentHasTextDefault()
    {
        // Arrange — simulates a Gum project where the TextBox component's own
        // default state has TextInstance.Text = "" (which is the standard setup).
        // The TextBox's ElementSave resets Text to empty on SetInitialState.
        ContainerRuntime parent = new();

        TextBox textBox = new();
        textBox.Visual.Name = "TextBoxInstance";
        textBox.Visual.Parent = parent;

        // The TextBox component has its own ElementSave with TextInstance.Text = ""
        GraphicalUiElement textInstance = textBox.Visual.GetGraphicalUiElementByName("TextInstance")!;
        ComponentSave textBoxComponent = new();
        textBoxComponent.Name = "Controls/TextBox";
        StateSave textBoxDefault = new();
        textBoxDefault.Name = "Default";
        textBoxDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Value = "",
            SetsValue = true
        });
        textBoxComponent.States.Add(textBoxDefault);
        textBox.Visual.AddStates(new System.Collections.Generic.List<StateSave> { textBoxDefault });
        textBox.Visual.ElementSave = textBoxComponent;

        // Parent screen
        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        // Type some text
        textBox.HandleCharEntered('H');
        textBox.HandleCharEntered('i');
        textBox.Text.ShouldBe("Hi");

        // Act — refresh from the parent
        parent.RefreshStyles();

        // Assert — text should be preserved
        textBox.Text.ShouldBe("Hi");
    }

    [Fact]
    public void RefreshStyles_ShouldNotCrashOnFocusedTextBoxWhenComponentResetsText()
    {
        // Arrange — same setup: TextBox component with TextInstance.Text = ""
        ContainerRuntime parent = new();

        TextBox textBox = new();
        textBox.Visual.Name = "TextBoxInstance";
        textBox.Visual.Parent = parent;
        textBox.IsFocused = true;

        ComponentSave textBoxComponent = new();
        textBoxComponent.Name = "Controls/TextBox";
        StateSave textBoxDefault = new();
        textBoxDefault.Name = "Default";
        textBoxDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Value = "",
            SetsValue = true
        });
        textBoxComponent.States.Add(textBoxDefault);
        textBox.Visual.AddStates(new System.Collections.Generic.List<StateSave> { textBoxDefault });
        textBox.Visual.ElementSave = textBoxComponent;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        textBox.HandleCharEntered('A');
        textBox.HandleCharEntered('B');
        textBox.HandleCharEntered('C');

        // Act
        parent.RefreshStyles();

        // Assert — should not crash, and typing should still work
        textBox.HandleCharEntered('D');
        textBox.Text.ShouldBe("ABCD");
    }

    [Fact]
    public void RefreshStyles_ShouldHidePlaceholderOnUnfocusedTextBoxWithText()
    {
        // Arrange — unfocused TextBox with text should not show placeholder after refresh
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.HandleCharEntered('H');
        textBox.HandleCharEntered('i');
        textBox.IsFocused = false;

        GraphicalUiElement placeholder = textBox.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance")!;
        placeholder.Visible.ShouldBe(false, "placeholder should be hidden when text exists");

        // Act
        textBox.Visual.RefreshStyles();

        // Assert
        textBox.Text.ShouldBe("Hi");
        placeholder.Visible.ShouldBe(false, "placeholder should still be hidden after refresh");
    }

    [Fact]
    public void RefreshStyles_ShouldHideSelectionOnUnfocusedTextBox()
    {
        // Arrange — unfocused TextBox should not show selection after refresh
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.HandleCharEntered('A');
        textBox.HandleCharEntered('B');
        textBox.IsFocused = false;

        GraphicalUiElement selection = textBox.Visual.GetGraphicalUiElementByName("SelectionInstance")!;

        // Act
        textBox.Visual.RefreshStyles();

        // Assert — selection should not be visible
        selection.Visible.ShouldBe(false, "selection should be hidden on unfocused textbox after refresh");
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveRadioButtonGroupState()
    {
        // Arrange — two radio buttons in the same group (both added to root)
        RadioButton radio1 = new();
        radio1.AddToRoot();
        RadioButton radio2 = new();
        radio2.AddToRoot();

        // Check the second one
        radio2.IsChecked = true;
        radio1.IsChecked.ShouldBe(false);
        radio2.IsChecked.ShouldBe(true);

        // Act — refresh from the root
        GumService.Default.RefreshStyles();

        // Assert — checked state should be preserved
        radio1.IsChecked.ShouldBe(false);
        radio2.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveScrollViewerOffset()
    {
        // Arrange — ScrollViewer with content taller than the viewer
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 100;
        scrollViewer.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        // The InnerPanelInstance's element (Container) has Y=0 in its default state.
        // This simulates the file-loaded scenario where SetInitialState resets Y to 0.
        StandardElementSave containerElement = new();
        containerElement.Name = "Container";
        StateSave containerDefault = new();
        containerDefault.Name = "Default";
        containerDefault.Variables.Add(new VariableSave
        {
            Name = "Y",
            Value = 0f,
            SetsValue = true
        });
        containerElement.States.Add(containerDefault);
        scrollViewer.InnerPanel.AddStates(new System.Collections.Generic.List<StateSave> { containerDefault });
        scrollViewer.InnerPanel.ElementSave = containerElement;

        // Add tall content to make it scrollable
        ContainerRuntime tallContent = new();
        tallContent.Width = 200;
        tallContent.Height = 500;
        tallContent.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        tallContent.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.InnerPanel.Children.Add(tallContent);

        // Force layout so scroll range is computed
        scrollViewer.Visual.UpdateLayout();

        // Scroll down
        double scrollValue = 50;
        scrollViewer.VerticalScrollBarValue = scrollValue;
        float innerPanelYBefore = scrollViewer.InnerPanel.Y;
        innerPanelYBefore.ShouldBe(-50f);

        // Act
        scrollViewer.Visual.RefreshStyles();

        // Assert — scroll offset should be preserved
        scrollViewer.VerticalScrollBarValue.ShouldBe(scrollValue);
        scrollViewer.InnerPanel.Y.ShouldBe(innerPanelYBefore,
            "InnerPanel.Y should be preserved after RefreshStyles");
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveComboBoxSelectedText()
    {
        // Arrange — ComboBox with a selection made at runtime
        ComboBox comboBox = new();
        comboBox.Items.Add("Option A");
        comboBox.Items.Add("Option B");
        comboBox.Items.Add("Option C");
        comboBox.SelectedIndex = 1;

        // The ComboBox component has TextInstance.Text = "" in its default state
        ComponentSave comboBoxComponent = new();
        comboBoxComponent.Name = "Controls/ComboBox";
        StateSave comboDefault = new();
        comboDefault.Name = "Default";
        comboDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Value = "",
            SetsValue = true
        });
        comboBoxComponent.States.Add(comboDefault);
        comboBox.Visual.AddStates(new System.Collections.Generic.List<StateSave> { comboDefault });
        comboBox.Visual.ElementSave = comboBoxComponent;

        comboBox.Text.ShouldBe("Option B");

        // Act
        comboBox.Visual.RefreshStyles();

        // Assert — selected text should be preserved
        comboBox.Text.ShouldBe("Option B");
        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveRadioButtonCheckedVisual()
    {
        // Arrange — simulates file-loaded RadioButton with Radio.Visible
        // controlled by RadioButtonCategory states (EnabledOn/EnabledOff)
        RadioButton radio1 = new();
        radio1.AddToRoot();
        RadioButton radio2 = new();
        radio2.AddToRoot();

        // The code-only DefaultRadioButtonRuntime uses "InnerCheck" for the
        // check indicator. Verify it's visible when checked.
        radio2.IsChecked = true;

        GraphicalUiElement innerCheck2 = radio2.Visual.GetGraphicalUiElementByName("InnerCheck")!;
        GraphicalUiElement innerCheck1 = radio1.Visual.GetGraphicalUiElementByName("InnerCheck")!;

        innerCheck2.Visible.ShouldBe(true, "radio2 inner check should be visible when checked");
        innerCheck1.Visible.ShouldBe(false, "radio1 inner check should be hidden when unchecked");

        // Act
        GumService.Default.RefreshStyles();

        // Assert — visual check state should be preserved
        radio2.IsChecked.ShouldBe(true);
        radio1.IsChecked.ShouldBe(false);
        innerCheck2.Visible.ShouldBe(true, "radio2 inner check should still be visible after refresh");
        innerCheck1.Visible.ShouldBe(false, "radio1 inner check should still be hidden after refresh");
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveLabelTextWhenComponentHasTextDefault()
    {
        // Simulates a Gum project where the Label component's own default state
        // sets TextInstance.Text, and the user has assigned a runtime value
        // (e.g. a localized string) via Label.Text in CustomInitialize.
        ContainerRuntime parent = new();

        Label label = new();
        label.Visual.Name = "LabelInstance";
        label.Visual.Parent = parent;

        ComponentSave labelComponent = new();
        labelComponent.Name = "Controls/Label";
        StateSave labelDefault = new();
        labelDefault.Name = "Default";
        labelDefault.Variables.Add(new VariableSave
        {
            Name = "Text",
            Value = "DesignTimeText",
            SetsValue = true
        });
        labelComponent.States.Add(labelDefault);
        label.Visual.AddStates(new System.Collections.Generic.List<StateSave> { labelDefault });
        label.Visual.ElementSave = labelComponent;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        label.Text = "RuntimeText";
        label.Text.ShouldBe("RuntimeText");

        parent.RefreshStyles();

        label.Text.ShouldBe("RuntimeText");
    }

    [Fact]
    public void RefreshStyles_ShouldPreserveButtonTextWhenComponentHasTextDefault()
    {
        ContainerRuntime parent = new();

        Button button = new();
        button.Visual.Name = "ButtonInstance";
        button.Visual.Parent = parent;

        ComponentSave buttonComponent = new();
        buttonComponent.Name = "Controls/Button";
        StateSave buttonDefault = new();
        buttonDefault.Name = "Default";
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Value = "DesignTimeText",
            SetsValue = true
        });
        buttonComponent.States.Add(buttonDefault);
        button.Visual.AddStates(new System.Collections.Generic.List<StateSave> { buttonDefault });
        button.Visual.ElementSave = buttonComponent;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        button.Text = "RuntimeText";
        button.Text.ShouldBe("RuntimeText");

        parent.RefreshStyles();

        button.Text.ShouldBe("RuntimeText");
    }

    [Fact]
    public void RefreshStyles_ShouldRaiseBeforeAndAfterRefreshStylesEventsInOrder()
    {
        // A FrameworkElement subscriber (e.g. user code in CustomInitialize)
        // must be able to save state before the default state is re-applied
        // and restore/override state after, without subclassing.
        ContainerRuntime parent = new();

        Button button = new();
        button.Visual.Name = "ButtonInstance";
        button.Visual.Parent = parent;

        ComponentSave buttonComponent = new();
        buttonComponent.Name = "Controls/Button";
        StateSave buttonDefault = new();
        buttonDefault.Name = "Default";
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Value = "DesignTimeText",
            SetsValue = true
        });
        buttonComponent.States.Add(buttonDefault);
        button.Visual.AddStates(new System.Collections.Generic.List<StateSave> { buttonDefault });
        button.Visual.ElementSave = buttonComponent;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        button.Text = "UserValue";

        var log = new System.Collections.Generic.List<string>();

        button.BeforeRefreshStyles += (_, _) => log.Add("before");
        button.AfterRefreshStyles += (_, _) =>
        {
            log.Add("after");
            button.Text = "FinalValue";
        };

        parent.RefreshStyles();

        log.ShouldBe(new[] { "before", "after" });
        button.Text.ShouldBe("FinalValue");
    }

    [Fact]
    public void RegisterRuntimeProperty_ShouldPreserveReflectedPropertyOnTarget()
    {
        // Simulates a user registering a visual property for preservation
        // via the string-based overload targeting a child visual.
        ContainerRuntime parent = new();

        Button button = new();
        button.Visual.Name = "ButtonInstance";
        button.Visual.Parent = parent;

        ComponentSave buttonComponent = new();
        buttonComponent.Name = "Controls/Button";
        StateSave buttonDefault = new();
        buttonDefault.Name = "Default";
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = 100f,
            SetsValue = true
        });
        buttonComponent.States.Add(buttonDefault);
        button.Visual.AddStates(new System.Collections.Generic.List<StateSave> { buttonDefault });
        button.Visual.ElementSave = buttonComponent;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        // User sets width at runtime
        button.Visual.Width = 250f;
        button.RegisterRuntimeProperty(button.Visual, nameof(button.Visual.Width));

        parent.RefreshStyles();

        button.Visual.Width.ShouldBe(250f);
    }

    [Fact]
    public void RegisterRuntimeProperty_ShouldPreserveSelfPropertyByName()
    {
        // Simulates a user registering a property on self via the shorthand.
        Button button = new();
        button.IsEnabled = false;
        button.RegisterRuntimeProperty(nameof(button.IsEnabled));

        button.Visual.RefreshStyles();

        button.IsEnabled.ShouldBe(false);
    }

    [Fact]
    public void RegisterRuntimeProperty_ShouldPreserveLambdaBasedProperty()
    {
        // Simulates a user registering a property via getter/setter lambdas.
        ContainerRuntime parent = new();

        Button button = new();
        button.Visual.Name = "ButtonInstance";
        button.Visual.Parent = parent;

        ComponentSave buttonComponent = new();
        buttonComponent.Name = "Controls/Button";
        StateSave buttonDefault = new();
        buttonDefault.Name = "Default";
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "Height",
            Value = 50f,
            SetsValue = true
        });
        buttonComponent.States.Add(buttonDefault);
        button.Visual.AddStates(new System.Collections.Generic.List<StateSave> { buttonDefault });
        button.Visual.ElementSave = buttonComponent;

        StateSave parentDefault = new();
        parentDefault.Name = "Default";
        ScreenSave parentElement = new();
        parentElement.Name = "TestScreen";
        parentElement.States.Add(parentDefault);
        parent.AddStates(new System.Collections.Generic.List<StateSave> { parentDefault });
        parent.ElementSave = parentElement;

        button.Visual.Height = 300f;
        button.RegisterRuntimeProperty(
            () => button.Visual.Height,
            v => button.Visual.Height = v);

        parent.RefreshStyles();

        button.Visual.Height.ShouldBe(300f);
    }
}
