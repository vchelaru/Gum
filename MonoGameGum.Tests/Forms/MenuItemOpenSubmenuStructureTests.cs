using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// A MenuItem submenu subscribes to its sub-items once per open (#6693), a Menu queues one next-push
/// action while one is pending (#6694), and an open submenu follows Items being added, removed or
/// cleared without closing or losing the state of the rows that stay (#6695).
/// </summary>
public class MenuItemOpenSubmenuStructureTests : BaseTestClass
{
    #region Helpers

    private class Bar
    {
        public ContainerRuntime Container = null!;
        public Menu Menu = null!;
        public MenuItem File = null!;
        public MenuItem Recent = null!;
        public MenuItem NewItem = null!;
        public MenuItem OpenItem = null!;
    }

    // File (New, Open, Recent (One, Two)).
    private static Bar CreateBar()
    {
        Bar bar = new();
        bar.Container = new ContainerRuntime();
        bar.Container.X = 20;
        bar.Container.Y = 20;
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
        bar.OpenItem = new MenuItem { Header = "Open" };
        bar.File.Items.Add(bar.OpenItem);
        bar.Recent = new MenuItem { Header = "Recent" };
        bar.File.Items.Add(bar.Recent);
        bar.Recent.Items!.Add(new MenuItem { Header = "One" });
        bar.Recent.Items.Add(new MenuItem { Header = "Two" });

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

    private static GraphicalUiElement LastPopup() => FrameworkElement.PopupRoot.Children![^1];

    private static int PopupRootCount() => FrameworkElement.PopupRoot.Children!.Count;

    // The headers of the MenuItem rows in a popup, top to bottom (rows of nested popups are not in it).
    private static List<string> HeadersIn(GraphicalUiElement popup)
    {
        List<string> headers = new();
        Collect(popup, headers);
        return headers;

        static void Collect(GraphicalUiElement parent, List<string> into)
        {
            foreach (GraphicalUiElement child in parent.Children!)
            {
                if (child is InteractiveGue gue && gue.FormsControlAsObject is MenuItem item)
                {
                    into.Add((string)item.Header!);
                }
                else
                {
                    Collect(child, into);
                }
            }
        }
    }

    private static int SubscriberCount(object target, string eventName)
    {
        FieldInfo? field = target.GetType().GetField(eventName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.ShouldNotBeNull();
        Delegate? handlers = (Delegate?)field.GetValue(target);
        return handlers?.GetInvocationList().Length ?? 0;
    }

    // One primary push with the cursor over the center of the visual (or the bottom right corner of the
    // canvas for null), then frames with the button up.
    private static void PushOn(GraphicalUiElement? visualOver, double pushTime)
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
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
    }

    #endregion

    #region Sub-item Selected subscription (#6693)

    [Fact]
    public void OpenClose_RepeatedCycles_DoNotAccumulateSelectedSubscriptionsOnSubItems()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.File.IsSelected = false;
        int afterFirstCycle = SubscriberCount(bar.NewItem, "Selected");

        for (int i = 0; i < 5; i++)
        {
            bar.File.IsSelected = true;
            bar.File.IsSelected = false;
        }

        SubscriberCount(bar.NewItem, "Selected").ShouldBe(afterFirstCycle);
    }

    [Fact]
    public void OpenSubmenu_SubItemSelected_DeselectsItsSiblings()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        bar.NewItem.IsSelected = true;

        bar.Recent.IsSelected.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void OpenSubmenuAfterSeveralCycles_SubItemSelected_DeselectsItsSiblings()
    {
        Bar bar = CreateBar();
        for (int i = 0; i < 3; i++)
        {
            bar.File.IsSelected = true;
            bar.File.IsSelected = false;
        }
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        bar.NewItem.IsSelected = true;

        bar.Recent.IsSelected.ShouldBeFalse();
    }

    #endregion

    #region Pending next-push action (#6694)

    [Fact]
    public void OpenClose_RepeatedCycles_DoNotAccumulatePendingPushActions()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.File.IsSelected = false;
        int afterFirstCycle = InteractiveGue.PendingNextPushActionCount;

        for (int i = 0; i < 5; i++)
        {
            bar.File.IsSelected = true;
            bar.File.IsSelected = false;
        }

        InteractiveGue.PendingNextPushActionCount.ShouldBeLessThanOrEqualTo(afterFirstCycle);
    }

    [Fact]
    public void OpenCloseCycles_ThenPushOutside_ClosesTheMenu()
    {
        Bar bar = CreateBar();
        for (int i = 0; i < 3; i++)
        {
            bar.File.IsSelected = true;
            bar.File.IsSelected = false;
        }
        PushOn(bar.File.Visual, pushTime: 1);
        bar.File.IsPopupVisible.ShouldBeTrue("precondition: pushing the item opens its submenu");

        PushOn(null, pushTime: 2);

        bar.File.IsSelected.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void ClosedByPush_ThenOpenedAgain_AnotherPushOutsideClosesItAgain()
    {
        Bar bar = CreateBar();
        PushOn(bar.File.Visual, pushTime: 1);
        PushOn(null, pushTime: 2);
        bar.File.IsPopupVisible.ShouldBeFalse("precondition: the first outside push closes the menu");
        PushOn(bar.File.Visual, pushTime: 3);
        bar.File.IsPopupVisible.ShouldBeTrue("precondition: pushing the item opens its submenu again");

        PushOn(null, pushTime: 4);

        bar.File.IsPopupVisible.ShouldBeFalse();
        InteractiveGue.PendingNextPushActionCount.ShouldBe(0);
    }

    #endregion

    #region Items changing while open (#6695)

    [Fact]
    public void AllItemsClearedWhileOpen_LeavesNoEmptyPopupOnScreen()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        bar.File.IsSelected = true;

        bar.File.Items!.Clear();

        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void AllItemsClearedWhileOpen_DeselectsTheItemSoItCanOpenAgain()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.File.Items!.Clear();

        bar.File.Items.Add(new MenuItem { Header = "Fresh" });
        bar.File.IsSelected = true;

        bar.File.IsSelected.ShouldBeTrue();
        HeadersIn(PopupOf(bar.File)).ShouldBe(new[] { "Fresh" });
    }

    [Fact]
    public void ItemAddedToSubmenuWhileOpen_AppearsInThePopup()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        MenuItem added = new() { Header = "Added" };

        bar.File.Items!.Add(added);

        HeadersIn(PopupOf(bar.File)).ShouldBe(new[] { "New", "Open", "Recent", "Added" });
    }

    [Fact]
    public void ItemInsertedInTheMiddleWhileOpen_AppearsAtThatPosition()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;

        bar.File.Items!.Insert(1, new MenuItem { Header = "Inserted" });

        HeadersIn(PopupOf(bar.File)).ShouldBe(new[] { "New", "Inserted", "Open", "Recent" });
    }

    [Fact]
    public void ItemAddedWhileOpen_GrowsThePopupAndKeepsItBelowTheOwner()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        float heightBefore = PopupOf(bar.File).AbsoluteHeight;

        bar.File.Items!.Add(new MenuItem { Header = "Added" });

        GraphicalUiElement popup = PopupOf(bar.File);
        popup.AbsoluteHeight.ShouldBeGreaterThan(heightBefore);
        popup.AbsoluteLeft.ShouldBe(bar.File.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(bar.File.Visual.GetAbsoluteBottom(), 0.5f);
    }

    [Fact]
    public void ItemAddedWhileOpenNearTheBottomEdge_PopupStillEndsAtTheCanvasBottom()
    {
        Bar bar = CreateBar();
        bar.File.ScrollViewerVisualTemplate = null;
        bar.Container.Anchor(Anchor.BottomLeft);
        bar.Container.UpdateLayout();
        bar.File.IsSelected = true;
        float heightBefore = PopupOf(bar.File).AbsoluteHeight;

        bar.File.Items!.Add(new MenuItem { Header = "Added" });

        GraphicalUiElement popup = PopupOf(bar.File);
        popup.AbsoluteHeight.ShouldBeGreaterThan(heightBefore);
        (popup.AbsoluteTop + popup.AbsoluteHeight).ShouldBe(GraphicalUiElement.CanvasHeight, 0.5f);
    }

    [Fact]
    public void ItemAddedWhileOpen_KeepsTheOtherRowsStateAndOpenSubmenu()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        bar.File.Items!.Add(new MenuItem { Header = "Added" });

        bar.File.IsSelected.ShouldBeTrue();
        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.Recent.IsSelected.ShouldBeTrue();
        bar.Recent.IsPopupVisible.ShouldBeTrue();
        PopupOf(bar.Recent).AbsoluteLeft.ShouldBe(bar.Recent.Visual.GetAbsoluteRight(), 0.5f);
    }

    [Fact]
    public void ItemAddedWhileOpen_JoinsTheSiblingsSoSelectingItDeselectsThem()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        MenuItem added = new() { Header = "Added" };
        bar.File.Items!.Add(added);

        added.IsSelected = true;

        bar.Recent.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ItemRemovedFromSubmenuWhileOpen_LeavesThePopup()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;

        bar.File.Items!.Remove(bar.OpenItem);

        HeadersIn(PopupOf(bar.File)).ShouldBe(new[] { "New", "Recent" });
        bar.OpenItem.Visual.Parent.ShouldBeNull();
    }

    [Fact]
    public void ItemRemovedFromSubmenuWhileOpen_StopsListeningToItsSelected()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        int whileOpen = SubscriberCount(bar.OpenItem, "Selected");

        bar.File.Items!.Remove(bar.OpenItem);

        SubscriberCount(bar.OpenItem, "Selected").ShouldBe(whileOpen - 1);
    }

    [Fact]
    public void ItemRemovedFromSubmenuWhileOpen_ShrinksThePopupAndKeepsOtherRowsOpen()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        float heightBefore = PopupOf(bar.File).AbsoluteHeight;

        bar.File.Items!.Remove(bar.OpenItem);

        PopupOf(bar.File).AbsoluteHeight.ShouldBeLessThan(heightBefore);
        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.Recent.IsPopupVisible.ShouldBeTrue();
        PopupOf(bar.Recent).AbsoluteTop.ShouldBe(bar.Recent.Visual.AbsoluteTop, 0.5f);
    }

    [Fact]
    public void NestedItemRemovedFromSubmenuWhileItsOwnSubmenuIsOpen_ClosesThatSubmenu()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        bar.File.Items!.Remove(bar.Recent);

        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsSelected.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline + 1);
    }

    [Fact]
    public void ItemReplacedWhileOpen_ShowsTheNewOne()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;

        bar.File.Items![1] = new MenuItem { Header = "Replacement" };

        HeadersIn(PopupOf(bar.File)).ShouldBe(new[] { "New", "Replacement", "Recent" });
    }

    [Fact]
    public void DataItemsChangedWhileOpen_FollowTheObservableCollection()
    {
        Bar bar = CreateBar();
        ObservableCollection<string> names = new() { "Alpha", "Beta" };
        bar.File.Items = names;
        bar.File.IsSelected = true;

        names.Add("Gamma");
        HeadersIn(LastPopup()).ShouldBe(new[] { "Alpha", "Beta", "Gamma" });

        names.Remove("Alpha");
        HeadersIn(LastPopup()).ShouldBe(new[] { "Beta", "Gamma" });

        names.Clear();
        bar.File.IsPopupVisible.ShouldBeFalse();
    }

    [Fact]
    public void ItemAddedWhileClosed_DoesNotOpenThePopup()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();

        bar.File.Items!.Add(new MenuItem { Header = "Added" });

        bar.File.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    #endregion
}
