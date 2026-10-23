using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ListBox.SelectedObject does not scroll the selected row into view, unlike SelectedIndex
/// (found by the #6643 splash-damage sweep).
/// </summary>
public class ListBoxSelectedObjectScrollTests : BaseTestClass
{
    [Fact]
    public void ListBox_SelectedObjectSetWhenLaidOut_ShouldScrollSelectedItemIntoView()
    {
        ListBox listBox = new();
        listBox.Visual.Height = 200;
        listBox.AddToRoot();
        for (int i = 0; i < 50; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        listBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0, "the test needs scrollable content");

        listBox.SelectedObject = "Item 40";

        IsRowInView(listBox, 40).ShouldBeTrue();
    }

    [Fact]
    public void ComboBox_SelectedObjectSetWhileOpen_ShouldShowSelectedItem()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        for (int i = 0; i < 30; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.IsDropDownOpen = true;
        comboBox.ListBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0, "the drop-down must have more rows than it shows");

        comboBox.SelectedObject = "Item 25";

        IsRowInView(comboBox.ListBox, 25).ShouldBeTrue();
    }

    [Fact]
    public void ListBox_SelectedObjectSetInHiddenParentThenParentShown_ShouldShowSelectedItem()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);
        listBox.SelectedObject = "Item 40";
        listBox.VerticalScrollBarValue.ShouldBe(0, "a hidden list box defers the scroll instead of jumping to the bottom");

        parent.Visible = true;
        parent.UpdateLayout();

        IsRowInView(listBox, 40).ShouldBeTrue();
    }

    [Fact]
    public void ListBox_SelectedObjectSetToNullInHiddenParentThenParentShown_ShouldNotScroll()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);
        listBox.SelectedObject = "Item 49";
        listBox.SelectedObject = null;

        parent.Visible = true;
        parent.UpdateLayout();

        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    private static (ContainerRuntime Parent, ListBox ListBox) CreateListBoxInHiddenParent(int itemCount)
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        parent.Visible = false;
        ListBox listBox = new();
        parent.Children.Add(listBox.Visual);
        listBox.Visual.Height = 200;
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        return (parent, listBox);
    }

    private static bool IsRowInView(ListBox listBox, int index)
    {
        GraphicalUiElement row = listBox.ListBoxItems[index].Visual;
        GraphicalUiElement clip = listBox.ClipContainer;
        float clipTop = clip.GetAbsoluteY();
        float clipBottom = clipTop + clip.AbsoluteHeight;
        float rowTop = row.GetAbsoluteY();
        float rowBottom = rowTop + row.AbsoluteHeight;
        return clip.AbsoluteHeight > 0 && rowTop >= clipTop - 0.5f && rowBottom <= clipBottom + 0.5f;
    }
}
