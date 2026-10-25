using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Adding ListBox items one at a time (#6702) must not re-lay out every existing row, and must lay rows out the
/// same as before.
/// </summary>
public class ListBoxAppendLayoutTests : BaseTestClass
{
    [Fact]
    public void ItemsAdd_ShouldLayOutLinearly()
    {
        ListBox listBox = new();
        listBox.AddToRoot();

        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
        for (int i = 0; i < 100; i++)
        {
            listBox.Items!.Add("item " + i);
        }
        int calls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        // 46,181 layouts before #6702: every row and its hidden children were laid out again per add.
        calls.ShouldBeLessThan(100 * 30);
    }

    [Fact]
    public void ItemsAdd_ShouldStackRowsAndSizeHiddenChildren_WhenShown()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 20; i++)
        {
            listBox.Items!.Add("item " + i);
        }

        var rows = listBox.InnerPanel.Children.ToList();
        for (int i = 1; i < rows.Count; i++)
        {
            rows[i].AbsoluteTop.ShouldBe(rows[i - 1].AbsoluteTop + rows[i - 1].AbsoluteHeight + listBox.InnerPanel.StackSpacing);
        }

        // A hidden child skipped on every resume is still laid out when it is shown.
        var hidden = rows[0].Children.First(c => !c.Visible);
        hidden.Visible = true;
        hidden.AbsoluteWidth.ShouldBeGreaterThan(0);
        hidden.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(rows[0].AbsoluteLeft);
        hidden.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(rows[0].AbsoluteTop);
    }

    [Fact]
    public void ItemsAdd_ShouldSizeHoverBackground_WhenShownAfterAppends()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 30; i++)
        {
            listBox.Items!.Add("item " + i);
        }
        var row = listBox.InnerPanel.Children[5];
        var background = row.Children.First(c => c.Name == "Background");
        background.Visible.ShouldBeFalse();

        background.Visible = true;

        background.AbsoluteWidth.ShouldBe(row.AbsoluteWidth);
        background.AbsoluteHeight.ShouldBe(row.AbsoluteHeight);
        background.AbsoluteLeft.ShouldBe(row.AbsoluteLeft);
        background.AbsoluteTop.ShouldBe(row.AbsoluteTop);
    }
}
