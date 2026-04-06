using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class TextBoxVisualTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        TextBox textBox = new();
        InteractiveGue visual = textBox.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (var child in children)
        {
            child.HasEvents.ShouldBeFalse(
                $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
        }
    }

    [Fact]
    public void Constructor_ShouldCreateV3Visual()
    {
        TextBox textBox = new();
        textBox.Visual.ShouldNotBeNull();
        (textBox.Visual is Gum.Forms.DefaultVisuals.V3.TextBoxVisual).ShouldBeTrue();
    }

    [Fact]
    public void PlaceholderText_ShuldNotLineWrap_InSingleLineMode()
    {
        TextBox textBox = new();
        TextBoxVisual visual = (TextBoxVisual) textBox.Visual;
        visual.PlaceholderTextInstance.WidthUnits.ShouldNotBe(Gum.DataTypes.DimensionUnitType.RelativeToParent);
    }

    [Fact]
    public void PlaceholderText_ShouldLineWrap_InMultiLineMode()
    {
        TextBox textBox = new();
        TextBoxVisual visual = (TextBoxVisual)textBox.Visual;

        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        visual.PlaceholderTextInstance.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToParent);

        textBox.TextWrapping = Gum.Forms.TextWrapping.NoWrap;
        visual.PlaceholderTextInstance.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);


        textBox.TextWrapping = Gum.Forms.TextWrapping.NoWrap;
        textBox.AcceptsReturn = true;
        visual.PlaceholderTextInstance.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindCaretInstance()
    {
        TextBox textBox = new();

        GraphicalUiElement? caret = textBox.Visual.GetGraphicalUiElementByName("CaretInstance");

        caret.ShouldNotBeNull();
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindSelectionInstance()
    {
        TextBox textBox = new();

        GraphicalUiElement? selection = textBox.Visual.GetGraphicalUiElementByName("SelectionInstance");

        selection.ShouldNotBeNull();
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindTextInstance()
    {
        TextBox textBox = new();

        // Text set/get depends on textComponent being found by RefreshInternalVisualReferences
        textBox.Text = "hello";

        textBox.Text.ShouldBe("hello");
    }

    [Fact]
    public void SelectionLength_ShouldBeSettableAfterConstruction()
    {
        TextBox textBox = new();
        textBox.Text = "hello";

        // SelectionStart/Length depend on selectionInstance being found
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 3;

        textBox.SelectionLength.ShouldBe(3);
    }

    [Fact]
    public void Selection_Y_ShouldNotDriftOnRepeatedUpdates()
    {
        // Regression test: SelectionInstance.Y = 2f in the V3 visual. Because selectionInstance
        // IS _selectionInstances[0], writing to selection.Y at i==0 modifies the value that gets
        // read as the offset on the next call, causing the selection to drift down by 2px per update.
        TextBox textBox = new();
        textBox.Text = "line one\nline two\nline three";
        textBox.AcceptsReturn = true;

        GraphicalUiElement SelectionInstance = textBox.Visual.GetGraphicalUiElementByName("SelectionInstance");

        // Select some text to trigger UpdateToSelection
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;
        float firstY = SelectionInstance.Y;

        // Change selection to trigger UpdateToSelection again
        textBox.SelectionStart = 1;
        float secondY = SelectionInstance.Y;

        // Change again
        textBox.SelectionStart = 0;
        float thirdY = SelectionInstance.Y;

        // Y should remain stable, not drift
        secondY.ShouldBe(firstY, "Selection Y drifted between first and second update");
        thirdY.ShouldBe(firstY, "Selection Y drifted between first and third update");
    }

    [Fact]
    public void TextBox_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        TextBox sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
