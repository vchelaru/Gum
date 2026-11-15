using System;
using System.Linq;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What happens to an open ComboBox drop-down when the ComboBox is
/// hidden, rooted elsewhere, or opened before it is in the tree. The popup hiding
/// when an ancestor is hidden is a separate known bug (#6681) and replacing the ListBox or Visual while open is #6684; neither is covered here.
/// </summary>
public class ComboBoxOpenLifecycleTests : BaseTestClass
{
    private static ComboBox CreateComboBox(params string[] items)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        foreach (string item in items)
        {
            comboBox.Items!.Add(item);
        }
        return comboBox;
    }

    private static void Tick(GraphicalUiElement? root = null) =>
        (root ?? Gum.GumService.Default.Root).AnimateSelf(0.016);

    private static (float X, float Y) OffsetFromComboBox(ComboBox comboBox) =>
        (comboBox.ListBox.Visual.AbsoluteX - comboBox.Visual.AbsoluteX,
         comboBox.ListBox.Visual.AbsoluteY - comboBox.Visual.AbsoluteY);

    private static void ShouldKeepOffset(ComboBox comboBox, (float X, float Y) offset)
    {
        (float X, float Y) now = OffsetFromComboBox(comboBox);
        now.X.ShouldBe(offset.X, 0.5f);
        now.Y.ShouldBe(offset.Y, 0.5f);
    }

    [Fact]
    public void IsDropDownOpen_SetFalseWhileComboBoxVisualIsHidden_RemovesThePopup()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana");
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        comboBox.IsDropDownOpen = true;
        comboBox.Visual.Visible = false;

        comboBox.IsDropDownOpen = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(popup);
    }

    [Fact]
    public void Follow_ComboBoxInModalRoot_PopupStaysInModalRootAndFollows()
    {
        ComboBox comboBox = new();
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        FrameworkElement.ModalRoot.Children!.Add(comboBox.Visual);
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.Parent.ShouldBeSameAs(FrameworkElement.ModalRoot);
        (float X, float Y) offset = OffsetFromComboBox(comboBox);

        comboBox.X += 70;
        Tick(FrameworkElement.ModalRoot);

        popup.Parent.ShouldBeSameAs(FrameworkElement.ModalRoot);
        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_TwoOpenComboBoxes_EachPopupFollowsItsOwnComboBox()
    {
        ComboBox first = CreateComboBox("Apple", "Banana");
        ComboBox second = CreateComboBox("Cherry", "Date");
        second.Y = 120;
        first.IsDropDownOpen = true;
        (float X, float Y) firstOffset = OffsetFromComboBox(first);
        first.X += 50;
        Tick();
        ShouldKeepOffset(first, firstOffset);

        // Opening the second drop-down takes focus, which closes the first (#6958).
        second.IsDropDownOpen = true;
        (float X, float Y) secondOffset = OffsetFromComboBox(second);
        second.Y += 30;
        Tick();

        ShouldKeepOffset(second, secondOffset);
    }

    [Fact]
    public void IsDropDownOpen_SetBeforeTheComboBoxIsInTheTree_FollowsOnceItIs()
    {
        ComboBox comboBox = new();
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");

        Should.NotThrow(() => comboBox.IsDropDownOpen = true);

        comboBox.AddToRoot();
        Tick();
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.X += 80;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_ComboBoxHiddenThenOpenedThenShownAtANewPosition_PopupFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana");
        comboBox.Visual.Visible = false;
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.X += 60;
        comboBox.Visual.Visible = true;

        Tick();

        ShouldKeepOffset(comboBox, offset);
    }
}
