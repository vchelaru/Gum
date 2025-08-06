using Gum.Forms.Controls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V2.Forms;
public class MenuItemTests
{
    [Fact]
    public void Constructor_ShouldCreateV2Visual()
    {
        MenuItem menuItem = new ();
        menuItem.Visual.ShouldNotBeNull();
        (menuItem.Visual is Gum.Forms.DefaultVisuals.MenuItemVisual).ShouldBeTrue();
    }

    [Fact]
    public void ShowSubItem_ShouldAcceptVisualTemplate_OfScrollViewerVisual()
    {
        MenuItem menuItem = new();
        for(int i = 0; i < 10; i++)
        {
            menuItem.Items.Add(i);
        }

        menuItem.ScrollViewerVisualTemplate = new Gum.Forms.VisualTemplate(() =>
        {
            var scrollViewer = new ScrollViewer();
            return scrollViewer.Visual;
        });

        // no exception should be thrown
        menuItem.IsSelected = true;
    }
}
