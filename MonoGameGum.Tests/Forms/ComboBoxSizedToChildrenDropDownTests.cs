using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A code-only ComboBox whose V3 drop-down is made sized to its items with
/// <see cref="ListBoxVisual.MakeHeightSizedToChildren"/> (#8412).
/// </summary>
public class ComboBoxSizedToChildrenDropDownTests : BaseTestClass
{
    #region Helpers

    private static void ResizeCanvas(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    private static void Tick() => Gum.GumService.Default.Root.AnimateSelf(0.016);

    private static ComboBox CreateComboBox(float y, int rows)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.Y = y;
        ((ListBoxVisual)comboBox.ListBox.Visual).MakeHeightSizedToChildren();
        for (int i = 0; i < rows; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        return comboBox;
    }

    private static float RowsHeight(ListBox listBox) =>
        listBox.ListBoxItems[^1].Visual.GetAbsoluteBottom() - listBox.ListBoxItems[0].Visual.GetAbsoluteTop();

    // The drop-down shows every row with no scrolling: the rows fit inside the clip area, and the
    // drop-down is no taller than its rows plus the visual's few pixels of padding.
    private static void ShouldFitRows(ComboBox comboBox)
    {
        ListBox listBox = comboBox.ListBox;
        GraphicalUiElement clip = (GraphicalUiElement)listBox.InnerPanel.Parent!;
        listBox.ListBoxItems[0].Visual.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 0.01f);
        listBox.ListBoxItems[^1].Visual.GetAbsoluteBottom().ShouldBeLessThanOrEqualTo(clip.AbsoluteBottom + 0.01f);
        listBox.Visual.AbsoluteHeight.ShouldBeLessThan(RowsHeight(listBox) + 16);
    }

    #endregion

    [Fact]
    public void Open_ShouldSizeDropDownToItsItems()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, rows: 3);

        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(comboBox.Visual.AbsoluteBottom, 0.01f);
        ShouldFitRows(comboBox);
    }

    [Fact]
    public void ItemsAddedOrRemovedWhileOpen_ShouldResizeDropDown()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, rows: 3);
        comboBox.IsDropDownOpen = true;
        float heightWithThreeRows = comboBox.ListBox.Visual.AbsoluteHeight;

        comboBox.Items!.Add("Item 3");
        comboBox.Items.Add("Item 4");
        Tick();

        comboBox.ListBox.Visual.AbsoluteHeight.ShouldBeGreaterThan(heightWithThreeRows);
        ShouldFitRows(comboBox);

        comboBox.Items.RemoveAt(4);
        comboBox.Items.RemoveAt(3);
        Tick();

        comboBox.ListBox.Visual.AbsoluteHeight.ShouldBe(heightWithThreeRows, 0.01f);
        ShouldFitRows(comboBox);
    }

    [Fact]
    public void Open_ShouldCapDropDownAtMaxHeightAndScroll()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, rows: 30);
        ListBoxVisual listBoxVisual = (ListBoxVisual)comboBox.ListBox.Visual;
        listBoxVisual.ClipContainerInstance.MaxHeight = 100;

        comboBox.IsDropDownOpen = true;

        listBoxVisual.ClipContainerInstance.AbsoluteHeight.ShouldBe(100, 0.01f);
        listBoxVisual.AbsoluteHeight.ShouldBeLessThan(120);
        comboBox.ListBox.VerticalScrollBarValue = float.MaxValue;
        GraphicalUiElement lastRow = comboBox.ListBox.ListBoxItems[29].Visual;
        lastRow.GetAbsoluteBottom().ShouldBeLessThanOrEqualTo(listBoxVisual.ClipContainerInstance.AbsoluteBottom + 0.01f);
        lastRow.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(listBoxVisual.ClipContainerInstance.AbsoluteTop - 0.01f);
    }

    [Fact]
    public void Open_ShouldOpenAbove_WhenItDoesNotFitBelow()
    {
        ResizeCanvas(800, 300);
        ComboBox comboBox = CreateComboBox(y: 250, rows: 5);

        comboBox.IsDropDownOpen = true;

        // The drop-down is measured at its rows' height before the above/below choice (#8435).
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteBottom.ShouldBe(comboBox.Visual.AbsoluteTop, 0.01f);
        ShouldFitRows(comboBox);
    }

    [Fact]
    public void MakeHeightFixedSize_ShouldOpenAtFixedHeight()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, rows: 3);
        ListBoxVisual listBoxVisual = (ListBoxVisual)comboBox.ListBox.Visual;

        listBoxVisual.MakeHeightFixedSize();
        comboBox.IsDropDownOpen = true;

        listBoxVisual.AbsoluteHeight.ShouldBe(256, 0.01f);
    }
}
