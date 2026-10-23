using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Hovering a sibling item while a submenu is open switches the open submenu (#6692).
/// </summary>
public class MenuItemHoverSwitchTests : BaseTestClass
{
    #region Helpers

    private class Bar
    {
        public ContainerRuntime Container = null!;
        public Menu Menu = null!;
        public MenuItem File = null!;
        public MenuItem Edit = null!;
        public MenuItem Recent = null!;
        public MenuItem NewItem = null!;
        public MenuItem Cut = null!;
        public MenuItem Standalone = null!;
    }

    // File (New, Open, Recent (One, Two)), Edit (Cut, Copy), Standalone (no sub-items).
    private static Bar CreateBar(float x = 0, float y = 0)
    {
        Bar bar = new();
        bar.Container = new ContainerRuntime();
        bar.Container.X = x;
        bar.Container.Y = y;
        bar.Container.Width = 400;
        bar.Container.WidthUnits = DimensionUnitType.Absolute;
        bar.Container.Height = 40;
        bar.Container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(bar.Container);

        bar.Menu = new Menu();
        bar.Container.Children!.Add(bar.Menu.Visual);

        bar.File = new MenuItem { Header = "File" };
        bar.Menu.Items!.Add(bar.File);
        bar.NewItem = new MenuItem { Header = "New" };
        bar.File.Items!.Add(bar.NewItem);
        bar.File.Items.Add(new MenuItem { Header = "Open" });
        bar.Recent = new MenuItem { Header = "Recent" };
        bar.File.Items.Add(bar.Recent);
        bar.Recent.Items!.Add(new MenuItem { Header = "One" });
        bar.Recent.Items.Add(new MenuItem { Header = "Two" });

        bar.Edit = new MenuItem { Header = "Edit" };
        bar.Menu.Items.Add(bar.Edit);
        bar.Cut = new MenuItem { Header = "Cut" };
        bar.Edit.Items!.Add(bar.Cut);
        bar.Edit.Items.Add(new MenuItem { Header = "Copy" });

        bar.Standalone = new MenuItem { Header = "Standalone" };
        bar.Menu.Items.Add(bar.Standalone);

        bar.Container.UpdateLayout();
        return bar;
    }

    private static int PopupRootCount() => FrameworkElement.PopupRoot.Children!.Count;

    // Simulates one primary push with the cursor over the center of the visual (or the bottom right
    // corner of the canvas for null), then frames with the button up. PrimaryPush is true for a single
    // frame, as with the real cursor, and the visual under the cursor comes from the real hit test. Each
    // push gets its own push time, so the menu can tell a new push from the one that opened it.
    private static void PushOn(GraphicalUiElement? visualOver, double pushTime, int heldFrames = 1)
    {
        float x = GraphicalUiElement.CanvasWidth - 1;
        float y = GraphicalUiElement.CanvasHeight - 1;
        if (visualOver != null)
        {
            x = visualOver.GetAbsoluteX() + visualOver.AbsoluteWidth / 2;
            y = visualOver.GetAbsoluteY() + visualOver.AbsoluteHeight / 2;
        }

        bool isPush = true;
        bool isDown = true;
        bool isClick = false;
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.Setup(c => c.PrimaryPush).Returns(() => isPush);
        cursor.Setup(c => c.PrimaryDown).Returns(() => isDown);
        cursor.Setup(c => c.PrimaryClick).Returns(() => isClick);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.LastPrimaryPushTime).Returns(pushTime);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        isPush = false;
        isDown = false;
        isClick = true;
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        isClick = false;
        for (int i = 0; i < heldFrames; i++)
        {
            Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
    }

    private static void OpenWithPush(MenuItem item, double pushTime = 1)
    {
        PushOn(item.Visual, pushTime);
        item.IsPopupVisible.ShouldBeTrue("precondition: pushing the item opens its submenu");
    }

    #endregion

    #region Hover

    [Fact]
    public void Hover_NextTopLevelItemWhileOpen_SwitchesTheSubmenu()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);

        bar.Edit.IsHighlighted = true;

        bar.File.IsSelected.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Edit.IsSelected.ShouldBeTrue();
        bar.Edit.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void Hover_ItemWithoutSubItemsWhileOpen_ClosesThePreviousSubmenu()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);

        bar.Standalone.IsHighlighted = true;

        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Hover_SiblingInSubmenu_SwitchesTheNestedSubmenu()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);
        PushOn(bar.Recent.Visual, pushTime: 2);
        bar.Recent.IsPopupVisible.ShouldBeTrue("precondition");

        bar.NewItem.IsHighlighted = true;

        bar.Recent.IsSelected.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.NewItem.IsSelected.ShouldBeTrue();
        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    #endregion
}
