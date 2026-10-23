using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A row that leaves an open submenu while hovered does not keep its highlight (#6700).
/// </summary>
public class MenuItemOpenSubmenuHighlightTests : BaseTestClass
{
    // File (I0, I1, I2), open.
    private static MenuItem OpenFile()
    {
        MenuItem file = new MenuItem { Header = "File" };
        Menu menu = new Menu();
        Gum.GumService.Default.Root.Children!.Add(menu.Visual);
        menu.Items!.Add(file);
        for (int i = 0; i < 3; i++)
        {
            file.Items!.Add(new MenuItem { Header = "I" + i });
        }
        file.IsSelected = true;
        return file;
    }

    [Fact]
    public void ItemRemovedWhileHoveredThenAddedBack_IsNotHighlighted()
    {
        MenuItem file = OpenFile();
        MenuItem hovered = (MenuItem)file.Items![1]!;
        hovered.IsHighlighted = true;
        file.Items.Remove(hovered);

        file.Items.Add(hovered);

        hovered.IsHighlighted.ShouldBeFalse();
    }

    [Fact]
    public void ItemRemovedWhileHovered_ClearsItsHighlight()
    {
        MenuItem file = OpenFile();
        MenuItem hovered = (MenuItem)file.Items![1]!;
        hovered.IsHighlighted = true;

        file.Items.Remove(hovered);

        hovered.IsHighlighted.ShouldBeFalse();
    }

    [Fact]
    public void ItemsClearedWhileHovered_ClearsTheRowsHighlight()
    {
        MenuItem file = OpenFile();
        MenuItem hovered = (MenuItem)file.Items![1]!;
        hovered.IsHighlighted = true;

        file.Items.Clear();

        hovered.IsHighlighted.ShouldBeFalse();
    }
}
