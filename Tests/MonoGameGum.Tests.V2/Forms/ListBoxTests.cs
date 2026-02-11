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
public class ListBoxTests
{
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ListBox listBox = new();
        InteractiveGue visual = listBox.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (var child in children)
        {
            if (child.Name != "ThumbContainer")
            {

                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }
    }

    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        var listBox = new Gum.Forms.Controls.ListBox();
        listBox.Visual.ShouldNotBeNull();
        (listBox.Visual is Gum.Forms.DefaultVisuals.ListBoxVisual).ShouldBeTrue();
    }


    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ListBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void ListBoxVisual_ShouldCreateListBoxForms()
    {
        var visual = new Gum.Forms.DefaultVisuals.ListBoxVisual();
        visual.FormsControl.ShouldNotBeNull();
    }
}
