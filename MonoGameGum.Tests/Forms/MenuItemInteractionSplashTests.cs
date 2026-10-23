using System;
using System.Reflection;
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
/// Splash-damage sweep over the MenuItem/Menu popup (#6685, #6691): how the menu opens, switches and
/// closes under cursor pushes, hovering and programmatic selection, and the events it raises.
/// </summary>
public class MenuItemInteractionSplashTests : BaseTestClass
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

    private static GraphicalUiElement PopupOf(MenuItem owner)
    {
        GraphicalUiElement visual = ((MenuItem)owner.Items![0]!).Visual;
        while (visual.Parent != null && visual.Parent != FrameworkElement.PopupRoot)
        {
            visual = (GraphicalUiElement)visual.Parent;
        }
        return visual;
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

    private static int SubscriberCount(object target, string eventName)
    {
        FieldInfo? field = target.GetType().GetField(eventName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.ShouldNotBeNull();
        Delegate? handlers = (Delegate?)field.GetValue(target);
        return handlers?.GetInvocationList().Length ?? 0;
    }

    #endregion

    #region Push

    [Fact]
    public void Push_HeldForSeveralFrames_RaisesClickedOnce()
    {
        Bar bar = CreateBar();
        int clicked = 0;
        bar.File.Clicked += (_, _) => clicked++;

        PushOn(bar.File.Visual, pushTime: 1, heldFrames: 4);

        clicked.ShouldBe(1);
    }

    [Fact]
    public void Push_OnDisabledTopLevelItem_DoesNotOpenItsSubmenu()
    {
        Bar bar = CreateBar();
        bar.File.IsEnabled = false;

        PushOn(bar.File.Visual, pushTime: 1);

        bar.File.IsSelected.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void Push_OnDisabledSubItem_DoesNotRaiseClicked()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);
        bar.NewItem.IsEnabled = false;
        int clicked = 0;
        bar.NewItem.Clicked += (_, _) => clicked++;

        PushOn(bar.NewItem.Visual, pushTime: 2);

        clicked.ShouldBe(0);
    }

    [Fact]
    public void Push_OnItemWithoutSubItems_RaisesClickedAndShowsNoPopup()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        int clicked = 0;
        bar.Standalone.Clicked += (_, _) => clicked++;

        PushOn(bar.Standalone.Visual, pushTime: 1);

        clicked.ShouldBe(1);
        bar.Standalone.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OnLeafSubItem_RaisesClickedAndClosesEveryLevel()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);
        int clicked = 0;
        bar.NewItem.Clicked += (_, _) => clicked++;

        PushOn(bar.NewItem.Visual, pushTime: 2);

        clicked.ShouldBe(1);
        bar.File.IsSelected.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OnNestedLeaf_ClosesEveryLevel()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);
        PushOn(bar.Recent.Visual, pushTime: 2);
        bar.Recent.IsPopupVisible.ShouldBeTrue("precondition");
        MenuItem one = (MenuItem)bar.Recent.Items![0]!;

        PushOn(one.Visual, pushTime: 3);

        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OnOpenTopLevelItemAgain_ClosesTheMenu()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);

        PushOn(bar.File.Visual, pushTime: 2);

        bar.File.IsSelected.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OnSubItemWithChildren_KeepsTheMenuOpenAndOpensItsSubmenu()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);

        PushOn(bar.Recent.Visual, pushTime: 2);

        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.Recent.IsSelected.ShouldBeTrue();
        bar.Recent.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void Push_OnTheMenuBarBackground_ClosesTheMenu()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);

        // The bar is 400 wide; past the last item is still on the Menu's visual.
        PushOn(bar.Menu.Visual, pushTime: 2);

        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OnTopLevelItem_OpensItsSubmenuAndSelectsIt()
    {
        Bar bar = CreateBar();

        PushOn(bar.File.Visual, pushTime: 1);

        bar.File.IsSelected.ShouldBeTrue();
        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void Push_OnTopLevelItemAfterMenuWasClosed_OpensItAgain()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);
        PushOn(null, pushTime: 2);
        bar.File.IsPopupVisible.ShouldBeFalse("precondition");

        PushOn(bar.File.Visual, pushTime: 3);

        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void Push_OutsideTheMenu_ClosesTheMenuAndDeselectsItems()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);

        PushOn(null, pushTime: 2);

        bar.File.IsSelected.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OutsideTheMenu_ClosesNestedSubmenusToo()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);
        PushOn(bar.Recent.Visual, pushTime: 2);
        bar.Recent.IsPopupVisible.ShouldBeTrue("precondition");

        PushOn(null, pushTime: 3);

        bar.Recent.IsSelected.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Push_OutsideTheMenu_ThenHoveringAnItem_DoesNotOpenIt()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);
        PushOn(null, pushTime: 2);

        bar.Edit.IsHighlighted = true;

        bar.Edit.IsSelected.ShouldBeFalse();
        bar.Edit.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void Push_OnAnotherMenusItem_ClosesTheFirstMenu()
    {
        Bar first = CreateBar();
        Bar second = CreateBar(y: 200);
        OpenWithPush(first.File);

        PushOn(second.File.Visual, pushTime: 2);

        first.File.IsPopupVisible.ShouldBeFalse();
        second.File.IsPopupVisible.ShouldBeTrue();
    }

    #endregion

    #region Hover intent

    [Fact]
    public void Hover_SwitchingBackAndForth_LeavesOnePopupOpen()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenWithPush(bar.File);

        for (int i = 0; i < 4; i++)
        {
            bar.Edit.IsHighlighted = true;
            bar.Edit.IsHighlighted = false;
            bar.File.IsHighlighted = true;
            bar.File.IsHighlighted = false;
        }

        PopupRootCount().ShouldBe(baseline + 1);
        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.Edit.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void Hover_BeforeTheMenuWasOpened_DoesNotOpenAnything()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();

        bar.File.IsHighlighted = true;

        bar.File.IsSelected.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    #endregion

    #region Selection and events

    [Fact]
    public void IsSelected_SetTrueTwice_RaisesSelectedOnce()
    {
        Bar bar = CreateBar();
        int selected = 0;
        bar.File.Selected += (_, _) => selected++;

        bar.File.IsSelected = true;
        bar.File.IsSelected = true;

        selected.ShouldBe(1);
    }

    [Fact]
    public void IsSelected_SelectingASibling_DeselectsThePreviousItemBeforeSelectedIsRaised()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bool fileWasSelectedWhenEditRaised = true;
        bar.Edit.Selected += (_, _) => fileWasSelectedWhenEditRaised = bar.File.IsSelected;

        bar.Edit.IsSelected = true;

        fileWasSelectedWhenEditRaised.ShouldBeFalse();
    }

    [Fact]
    public void IsSelected_SetFalse_DoesNotRaiseSelected()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        int selected = 0;
        bar.File.Selected += (_, _) => selected++;

        bar.File.IsSelected = false;

        selected.ShouldBe(0);
    }

    [Fact]
    public void IsSelected_SelectedAgainAfterDeselect_RaisesSelectedAgain()
    {
        Bar bar = CreateBar();
        int selected = 0;
        bar.File.Selected += (_, _) => selected++;

        bar.File.IsSelected = true;
        bar.File.IsSelected = false;
        bar.File.IsSelected = true;

        selected.ShouldBe(2);
    }

    [Fact]
    public void IsSelected_SelectingASibling_DeselectsThePreviousTopLevelItem()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;

        bar.Edit.IsSelected = true;

        bar.File.IsSelected.ShouldBeFalse();
        bar.Edit.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void IsSelected_SelectingASiblingInASubmenu_DeselectsThePreviousOne()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        bar.NewItem.IsSelected = true;

        bar.Recent.IsSelected.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.NewItem.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void IsSelected_AtMostOneTopLevelItemSelectedAfterAnySequence()
    {
        Bar bar = CreateBar();
        MenuItem[] items = { bar.File, bar.Edit, bar.Standalone };

        foreach (MenuItem item in new[] { bar.File, bar.Edit, bar.File, bar.Standalone, bar.Edit })
        {
            item.IsSelected = true;
            int selectedCount = 0;
            foreach (MenuItem other in items)
            {
                if (other.IsSelected)
                {
                    selectedCount++;
                }
            }
            selectedCount.ShouldBe(1);
        }
    }

    [Fact]
    public void IsSelected_ItemWithoutSubItems_CreatesNoPopup()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();

        bar.Standalone.IsSelected = true;

        bar.Standalone.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void IsSelected_SetFalseOnAClosedItem_DoesNothing()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();

        Should.NotThrow(() => bar.File.IsSelected = false);

        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void IsSelected_Clicked_IsNotRaisedBySelectingInCode()
    {
        Bar bar = CreateBar();
        int clicked = 0;
        bar.File.Clicked += (_, _) => clicked++;

        bar.File.IsSelected = true;

        clicked.ShouldBe(0);
    }

    [Fact]
    public void IsPopupVisible_IsFalseAfterEveryCloseMechanism()
    {
        Bar bar = CreateBar();

        bar.File.IsSelected = true;
        bar.File.IsSelected = false;
        bar.File.IsPopupVisible.ShouldBeFalse();

        bar.File.IsSelected = true;
        bar.File.HidePopupRecursively();
        bar.File.IsPopupVisible.ShouldBeFalse();

        bar.File.IsSelected = true;
        bar.Edit.IsSelected = true;
        bar.File.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void HidePopupRecursively_WhileNestedOpen_ClosesEveryLevelAndDeselectsChildren()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        bar.File.HidePopupRecursively();

        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsSelected.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    #endregion

    #region Repeated cycles

    [Fact]
    public void OpenClose_RepeatedCycles_LeaveNoPopupBehind()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();

        for (int i = 0; i < 10; i++)
        {
            bar.File.IsSelected = true;
            bar.Recent.IsSelected = true;
            bar.File.IsSelected = false;
        }

        PopupRootCount().ShouldBe(baseline);
    }


    #endregion

    #region Idle

    [Fact]
    public void Idle_AfterTheMenuIsOpenedAndClosedByPush_TheBarGoesIdleAgain()
    {
        Bar bar = CreateBar();
        OpenWithPush(bar.File);
        PushOn(null, pushTime: 2);
        bar.File.IsPopupVisible.ShouldBeFalse("precondition");

        for (int i = 0; i < 4; i++)
        {
            Gum.GumService.Default.Root.AnimateSelf(0.016);
        }

        bar.Container.IsIdle.ShouldBeTrue();
    }

    #endregion
}
