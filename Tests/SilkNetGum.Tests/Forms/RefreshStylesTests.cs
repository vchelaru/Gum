using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Shouldly;
using System.Collections.Generic;

namespace SilkNetGum.Tests.Forms;

// GumService.RefreshStyles on Skia must save and restore Forms runtime properties around the state
// re-apply, the same as MonoGame/raylib (issue #5229). Without the Forms delegates, the TextBox
// component's default state wipes typed text.
public class RefreshStylesTests : BaseTestClass
{
    [Fact]
    public void RefreshStyles_PreservesTypedTextBoxTextAndCaret()
    {
        TextBox textBox = new();
        textBox.Visual.Name = "TextBoxInstance";

        // A project TextBox component's default state sets TextInstance.Text to "".
        ComponentSave textBoxComponent = new() { Name = "Controls/TextBox" };
        StateSave textBoxDefault = new() { Name = "Default" };
        textBoxDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.Text",
            Value = "",
            SetsValue = true
        });
        textBoxComponent.States.Add(textBoxDefault);
        textBox.Visual.AddStates(new List<StateSave> { textBoxDefault });
        textBox.Visual.ElementSave = textBoxComponent;
        GumService.Default.Root.AddChild(textBox.Visual);

        textBox.HandleCharEntered('H');
        textBox.HandleCharEntered('i');
        textBox.Text.ShouldBe("Hi");

        GumService.Default.RefreshStyles();

        textBox.Text.ShouldBe("Hi");
        textBox.CaretIndex.ShouldBe(2);
    }
}
