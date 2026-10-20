using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
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

    #region Visual without TextInstance (#6195)

    [Fact]
    public void SetHeaderNoTranslate_ShouldNotThrow_WhenVisualHasNoTextInstance()
    {
        MenuItem sut = new(new InteractiveGue());

        Should.NotThrow(() => sut.SetHeaderNoTranslate("File"));
    }

    [Fact]
    public void UpdateToObject_ShouldNotThrow_WhenVisualHasNoTextInstance()
    {
        MenuItem sut = new(new InteractiveGue());

        Should.NotThrow(() => sut.UpdateToObject("File"));
    }

#if FULL_DIAGNOSTICS
    [Fact]
    public void Header_ShouldThrowDescriptiveError_WhenVisualHasNoTextInstance()
    {
        MenuItem sut = new(new InteractiveGue());

        Exception getterException = Should.Throw<Exception>(() => sut.Header);
        getterException.Message.ShouldContain("TextInstance");

        Exception setterException = Should.Throw<Exception>(() => sut.Header = "File");
        setterException.Message.ShouldContain("TextInstance");
    }
#else
    [Fact]
    public void Header_ShouldBeIgnored_WhenVisualHasNoTextInstance()
    {
        MenuItem sut = new(new InteractiveGue());

        Should.NotThrow(() => sut.Header = "File");
        sut.Header.ShouldBeNull();
        Should.NotThrow(() => sut.ToString());
    }
#endif

    #endregion

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
    public void SubmenuIndicatorIcon_ShouldUseArrow2Sprite()
    {
        MenuItem sut = new();
        MenuItemVisual visual = (MenuItemVisual)sut.Visual;

        SpriteRuntime expected = new();
        expected.ApplyState(Styling.ActiveStyle.Icons.Arrow2);

        visual.SubmenuIndicatorIcon.TextureLeft.ShouldBe(expected.TextureLeft);
        visual.SubmenuIndicatorIcon.TextureTop.ShouldBe(expected.TextureTop);
        visual.SubmenuIndicatorIcon.TextureWidth.ShouldBe(expected.TextureWidth);
        visual.SubmenuIndicatorIcon.TextureHeight.ShouldBe(expected.TextureHeight);
    }

    [Fact]
    public void SubmenuIndicatorIcon_ShouldNotEnlargeItemBeyondTextHeight()
    {
        // The arrow sprite is 32x32 on the sheet; if it sized the row, every item with a
        // submenu would be taller than the text it carries.
        MenuItem sut = new();
        MenuItemVisual visual = (MenuItemVisual)sut.Visual;

        visual.SubmenuIndicatorInstance.Height.ShouldBeLessThan(32);
        visual.SubmenuIndicatorInstance.HeightUnits.ShouldBe(DimensionUnitType.Absolute);
    }

    [Fact]
    public void SubmenuIndicatorColor_ShouldTintIcon()
    {
        MenuItem sut = new();
        MenuItemVisual visual = (MenuItemVisual)sut.Visual;

        visual.SubmenuIndicatorColor = Color.Red;

        visual.SubmenuIndicatorIcon.Color.ShouldBe(Color.Red);
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

    #region Visual-authored state flows to the control

    [Fact]
    public void Header_ShouldBeAuthoredText_WhenVisualAssignedToFreshMenuItem()
    {
        MenuItemVisual authoredVisual = new(tryCreateFormsObject: false);
        authoredVisual.TextInstance.Text = "File";

        MenuItem sut = new(authoredVisual);

        sut.Header.ShouldBe("File");
    }

    [Fact]
    public void Header_ShouldBeNewVisualText_WhenVisualReassigned()
    {
        MenuItem sut = new();
        sut.Header = "Old";
        MenuItemVisual authoredVisual = new(tryCreateFormsObject: false);
        authoredVisual.TextInstance.Text = "File";

        sut.Visual = authoredVisual;

        sut.Header.ShouldBe("File");
    }

    [Fact]
    public void Items_ShouldContainAuthoredSubItems_WhenVisualAssignedToFreshMenuItem()
    {
        ContainerRuntime authoredVisual = new();
        ContainerRuntime subItemContainer = new() { Name = "SubItemContainerInstance" };
        authoredVisual.AddChild(subItemContainer);
        MenuItem authoredSubItem = new();
        subItemContainer.Children.Add(authoredSubItem.Visual);

        MenuItem sut = new(authoredVisual);

        sut.Items!.Count.ShouldBe(1);
        sut.Items[0].ShouldBe(authoredSubItem);
    }

    [Fact]
    public void SubmenuIndicator_ShouldBeHidden_WhenVisualWithVisibleIndicatorAssignedToTopLevelMenuItem()
    {
        MenuItemVisual authoredVisual = new(tryCreateFormsObject: false);
        authoredVisual.SubmenuIndicatorInstance.Visible = true;

        MenuItem sut = new(authoredVisual);

        authoredVisual.SubmenuIndicatorInstance.Visible.ShouldBeFalse();
    }

    #endregion

    [Fact]
    public void UpdateToObject_ShouldNotWriteToOldVisual_WhenVisualSetToNull()
    {
        MenuItemVisual oldVisual = new(tryCreateFormsObject: false);
        oldVisual.TextInstance.Text = "File";
        MenuItem sut = new(oldVisual);

        sut.Visual = null!;
        sut.UpdateToObject("Edit");

        oldVisual.TextInstance.Text.ShouldBe("File");
    }
}
