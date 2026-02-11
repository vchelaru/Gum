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
    public void TextBox_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        TextBox sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
