using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
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
    public void IsSelected_SetFalse_ShouldHidePopup_WhenContainerManagersAttachedViaAttachManagersOnly()
    {
        // Mirrors a Menu drawn through GumBatch (immediate mode) rather than added to a managed
        // root - AttachManagersOnly is what such callers must use to get this to work (#4096
        // RenderTarget sample).
        ContainerRuntime container = new();
        container.AttachManagersOnly(SystemManagers.Default);

        Menu menu = new();
        container.Children.Add(menu.Visual);
        MenuItem fileItem = new();
        menu.Items!.Add(fileItem);
        fileItem.Items!.Add("SubItem");

        fileItem.IsSelected = true;
        fileItem.IsPopupVisible.ShouldBeTrue();

        fileItem.IsSelected = false;
        fileItem.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsSelected_SetFalse_ShouldThrow_WhenContainerHasNoManagers()
    {
        // Documents the constraint AttachManagersOnly exists to fix: HidePopupRecursively gates
        // on Visual.EffectiveManagers, which is null when the Menu's tree was never added to a
        // managed root (e.g. a container only ever drawn through GumBatch). This must throw
        // (FULL_DIAGNOSTICS) rather than silently leave the popup open, since that failure mode
        // produced no error signal at all when it shipped in a sample.
        ContainerRuntime container = new();
        Menu menu = new();
        container.Children.Add(menu.Visual);
        MenuItem fileItem = new();
        menu.Items!.Add(fileItem);
        fileItem.Items!.Add("SubItem");

        fileItem.IsSelected = true;
        fileItem.IsPopupVisible.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => fileItem.IsSelected = false);
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        MenuItem sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void SubmenuIndicatorInstanceVisible_ShouldBeFalse_ByDefault()
    {
        Menu menu = new();

        MenuItem menuItem = new();
        menu.Items!.Add(menuItem);
        var submenuIndicator = menuItem.GetVisual("SubmenuIndicatorInstance")!;

        submenuIndicator.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SubmenuIndicatorInstanceVisible_ShouldBeFalse_ForTopLevelItems()
    {
        Menu menu = new();

        MenuItem topItem = new();
        menu.Items!.Add(topItem);

        for (int i = 0; i < 5; i++)
        {
            topItem.Items!.Add(i);
        }

        topItem.GetVisual("SubmenuIndicatorInstance")!.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SubmenuIndicatorInstanceVisible_ShouldBeTrue_WhenAddedAsAChild()
    {
        Menu menu = new();

        MenuItem parentItem = new();
        menu.Items!.Add(parentItem);

        MenuItem childMenuItem = new();
        parentItem.Items!.Add(childMenuItem);

        for(int i = 0; i < 5 ; i++)
        {
            childMenuItem.Items!.Add(i);
        }

        parentItem.IsSelected = true;

        childMenuItem.ParentMenuItem.ShouldNotBeNull();
        childMenuItem.GetVisual("SubmenuIndicatorInstance")!.Visible.ShouldBeTrue();
    }

    [Fact]
    public void DoItemsHaveFocus_SetTrue_OnMenuItem_ShouldNotThrow()
    {
        // MenuItem inherits ScrollViewer.DoItemsHaveFocus (via ItemsControl), but MenuItemVisual --
        // unlike ScrollViewerVisual -- has no InnerPanelInstance, since menu items don't scroll.
        // Gamepad/keyboard nav sets this to true when A/Enter is pressed while a MenuItem has focus
        // (ScrollViewer.DoTopLevelFocusUpdate), which must not NRE on the null InnerPanel (#3668).
        MenuItem menuItem = new();
        menuItem.InnerPanel.ShouldBeNull();

        menuItem.IsFocused = true;

        Should.NotThrow(() => menuItem.DoItemsHaveFocus = true);
    }
}
