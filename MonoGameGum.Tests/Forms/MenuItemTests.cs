using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class MenuItemTests : BaseTestClass
{
    [Fact]
    public void SubmenuIndicatorInstanceVisible_ShouldBeFalse_ByDefault()
    {
        Menu menu = new();

        MenuItem menuItem = new();
        menu.Items.Add(menuItem);
        var submenuIndicator = menuItem.GetVisual("SubmenuIndicatorInstance")!;

        submenuIndicator.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SubmenuIndicatorInstanceVisible_ShouldBeFalse_ForTopLevelItems()
    {
        Menu menu = new();

        MenuItem topItem = new();
        menu.Items.Add(topItem);

        for (int i = 0; i < 5; i++)
        {
            topItem.Items.Add(i);
        }

        topItem.GetVisual("SubmenuIndicatorInstance")!.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SubmenuIndicatorInstanceVisible_ShouldBeTrue_WhenAddedAsAChild()
    {
        Menu menu = new();

        MenuItem parentItem = new();
        menu.Items.Add(parentItem);

        MenuItem childMenuItem = new();
        parentItem.Items.Add(childMenuItem);

        for(int i = 0; i < 5 ; i++)
        {
            childMenuItem.Items.Add(i);
        }

        parentItem.IsSelected = true;

        childMenuItem.ParentMenuItem.ShouldNotBeNull();
        childMenuItem.GetVisual("SubmenuIndicatorInstance")!.Visible.ShouldBeTrue();
    }
}
