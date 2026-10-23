using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class ListBoxSelectedItemsNoScrollTests : BaseTestClass
{
    // Decision (#6683): editing SelectedItems directly never scrolls, while hidden or visible.
    // Code that wants a scroll calls ScrollIndexIntoView itself.
    [Fact]
    public void ListBox_SelectedItemsAddWhenLaidOut_ShouldSelectRowWithoutScrolling()
    {
        ListBox listBox = CreateLaidOutMultiSelectListBox(50);

        listBox.SelectedItems.Add("Item 40");

        listBox.ListBoxItems[40].IsSelected.ShouldBeTrue();
        listBox.VerticalScrollBarValue.ShouldBe(0);
        IsRowInView(listBox, 40).ShouldBeFalse();
    }

    [Fact]
    public void ListBox_SelectedItemsAddWhileParentHiddenThenParentShown_ShouldNotScroll()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);

        listBox.SelectedItems.Add("Item 40");
        parent.Visible = true;
        parent.UpdateLayout();

        listBox.ListBoxItems[40].IsSelected.ShouldBeTrue();
        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_SelectedItemsClearAndRemove_ShouldNotChangeScroll()
    {
        ListBox listBox = CreateLaidOutMultiSelectListBox(50);
        listBox.SelectedItems.Add("Item 5");
        listBox.SelectedItems.Add("Item 40");
        listBox.ScrollIndexIntoView(20);
        double scrollBefore = listBox.VerticalScrollBarValue;
        scrollBefore.ShouldBeGreaterThan(0, "the test needs a non-zero scroll");

        listBox.SelectedItems.Remove("Item 40");
        listBox.VerticalScrollBarValue.ShouldBe(scrollBefore);

        listBox.SelectedItems.Clear();
        listBox.VerticalScrollBarValue.ShouldBe(scrollBefore);
        listBox.ListBoxItems[5].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ListBox_ScrollIndexIntoViewAfterSelectedItemsAdd_ShouldScrollRowIntoView()
    {
        ListBox listBox = CreateLaidOutMultiSelectListBox(50);
        listBox.SelectedItems.Add("Item 40");

        listBox.ScrollIndexIntoView(40);

        IsRowInView(listBox, 40).ShouldBeTrue();
    }

    private static ListBox CreateLaidOutMultiSelectListBox(int itemCount)
    {
        ListBox listBox = new();
        listBox.SelectionMode = SelectionMode.Multiple;
        listBox.Visual.Height = 200;
        listBox.AddToRoot();
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        listBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0, "the test needs scrollable content");
        return listBox;
    }

    private static (ContainerRuntime Parent, ListBox ListBox) CreateListBoxInHiddenParent(int itemCount)
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        parent.Visible = false;
        ListBox listBox = new();
        listBox.SelectionMode = SelectionMode.Multiple;
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
