using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Gum.GueDeriving;
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

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

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
    public void Selection_Y_ShouldApplyAuthoredOffsetWithoutDrifting()
    {
        // selectionInstance IS _selectionInstances[0], so writing selection.Y at i==0 would modify
        // the value read as the authored offset on the next call, drifting the selection down by
        // the offset per update.
        TextBox withOffset = CreateMultiLineTextBox(authoredSelectionY: 2);
        TextBox withoutOffset = CreateMultiLineTextBox(authoredSelectionY: 0);
        GraphicalUiElement selection = withOffset.Visual.GetGraphicalUiElementByName("SelectionInstance")!;

        float firstY = selection.Y;
        withOffset.SelectionStart = 1;
        float secondY = selection.Y;
        withOffset.SelectionStart = 0;
        float thirdY = selection.Y;

        firstY.ShouldBe(withoutOffset.Visual.GetGraphicalUiElementByName("SelectionInstance")!.Y + 2, 0.01f);
        secondY.ShouldBe(firstY, "Selection Y drifted between first and second update");
        thirdY.ShouldBe(firstY, "Selection Y drifted between first and third update");
    }

    static TextBox CreateMultiLineTextBox(float authoredSelectionY)
    {
        TextBoxVisual visual = new(tryCreateFormsObject: false);
        visual.SelectionInstance.Y = authoredSelectionY;
        TextBox textBox = new(visual);
        textBox.Text = "line one\nline two\nline three";
        textBox.AcceptsReturn = true;
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;
        return textBox;
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
