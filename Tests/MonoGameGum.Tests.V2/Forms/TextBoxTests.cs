using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class TextBoxTests
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
    public void Constructor_ShouldCreateV2Visual()
    {
        TextBox textBox = new ();
        textBox.Visual.ShouldNotBeNull();
        (textBox.Visual is Gum.Forms.DefaultVisuals.TextBoxVisual).ShouldBeTrue();
    }

    [Fact]
    public void SelectionLength_ShouldWork_OnMultiline()
    {
        TextBox textBox = new();
        textBox.TextWrapping = TextWrapping.Wrap;
        textBox.Text = "Hello, this is a multiline text box. It has really long text. This should line wrap";
        textBox.SelectionStart = 0;
        textBox.SelectionLength = textBox.Text.Length;
    }


    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        TextBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
