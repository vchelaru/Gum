using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ItemsControlTests : BaseTestClass
{
    [Fact]
    public void ItemsControl_Items_ShouldAddLabels_WhenAdding()
    {
        ItemsControl itemsControl = new ();
        itemsControl.Items.Add(1);
        itemsControl.InnerPanel.Children.Count.ShouldBe(1);
        (itemsControl.InnerPanel.Children[0] is InteractiveGue).ShouldBeTrue();
        (itemsControl.InnerPanel.Children[0] as InteractiveGue).FormsControlAsObject
            .ShouldBeOfType<Label>();
    }

    [Fact]
    public void ItemsControl_Items_ShouldRespectVisualTemplate()
    {
        ItemsControl itemsControl = new ();
        itemsControl.VisualTemplate = new Gum.Forms.VisualTemplate(() =>
        {
            ColoredRectangleRuntime toReturn = new ();
            toReturn.Color = Microsoft.Xna.Framework.Color.Orange;
            return toReturn;
        });

        itemsControl.Items.Add(1);
        itemsControl.InnerPanel.Children!.Count.ShouldBe(1);
        IRenderableIpso child = itemsControl.InnerPanel.Children[0];
        ColoredRectangleRuntime? coloredRectangle = child as ColoredRectangleRuntime;
        coloredRectangle.ShouldNotBeNull();
    }


    [Fact]
    public void ItemsControl_Items_ShouldRespectFrameworkElementTemplate()
    {
        var itemsControl = new ItemsControl();
        itemsControl.FrameworkElementTemplate = 
            new Gum.Forms.FrameworkElementTemplate(typeof(Button));

        itemsControl.Items.Add(1);
        itemsControl.InnerPanel.Children.Count.ShouldBe(1);
        IRenderableIpso child = itemsControl.InnerPanel.Children[0];
        (child is InteractiveGue).ShouldBeTrue();
        Button? button = ((InteractiveGue)child).FormsControlAsObject as Button;
        button.ShouldNotBeNull();
    }

    [Fact]
    public void ItemsControl_FrameworkElementTemplate_ShouldRecreateAllItems()
    {
        var itemsControl = new ItemsControl();

        itemsControl.Items.Add(1);

        itemsControl.FrameworkElementTemplate =
            new Gum.Forms.FrameworkElementTemplate(typeof(Button));

        itemsControl.InnerPanel.Children.Count.ShouldBe(1);
        
        Button? button = ((InteractiveGue)itemsControl.InnerPanel.Children[0])
            .FormsControlAsObject as Button;
        button.ShouldNotBeNull();
    }

    [Fact]
    public void ItemsControl_Orientation_ShouldSetInnerPanelChildrenLayout()
    {
        var itemsControl = new ItemsControl();
        var innerPanel = itemsControl.GetVisual("InnerPanelInstance");

        itemsControl.Orientation = Orientation.Horizontal;
        innerPanel.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);

        itemsControl.Orientation = Orientation.Vertical;
        innerPanel.ChildrenLayout.ShouldBe(ChildrenLayout.TopToBottomStack);
    }
}
