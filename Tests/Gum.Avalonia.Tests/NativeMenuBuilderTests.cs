using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The macOS menu-bar menu rendered from the shared <see cref="MenuModel"/>.</summary>
public class NativeMenuBuilderTests
{
    [AvaloniaFact]
    public void Build_MapsTheModel_AndFollowsItsChanges()
    {
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        MenuItemModel save = new MenuItemModel("Save") { IsEnabled = false };
        MenuItemModel grid = new MenuItemModel("Show Grid") { IsCheckable = true, IsChecked = true };
        file.Items.Add(save);
        file.Items.Add(MenuItemModel.Separator());
        file.Items.Add(grid);
        model.TopLevelItems.Add(file);

        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);

        NativeMenuItem fileItem = (NativeMenuItem)menu.Items[0];
        fileItem.Header.ShouldBe("File");
        NativeMenu fileMenu = fileItem.Menu.ShouldNotBeNull();
        NativeMenuItem saveItem = (NativeMenuItem)fileMenu.Items[0];
        saveItem.IsEnabled.ShouldBeFalse();
        saveItem.ToggleType.ShouldBe(MenuItemToggleType.None);
        fileMenu.Items[1].ShouldBeOfType<NativeMenuItemSeparator>();
        NativeMenuItem gridItem = (NativeMenuItem)fileMenu.Items[2];
        gridItem.ToggleType.ShouldBe(MenuItemToggleType.CheckBox);
        gridItem.IsChecked.ShouldBeTrue();

        save.Header = "Save Project";
        save.IsEnabled = true;
        grid.IsChecked = false;
        saveItem.Header.ShouldBe("Save Project");
        saveItem.IsEnabled.ShouldBeTrue();
        gridItem.IsChecked.ShouldBeFalse();

        file.Items.Add(new MenuItemModel("Exit"));
        model.TopLevelItems.Add(new MenuItemModel("Help"));
        ((NativeMenuItem)fileMenu.Items[3]).Header.ShouldBe("Exit");
        ((NativeMenuItem)menu.Items[1]).Header.ShouldBe("Help");
    }

    [AvaloniaFact]
    public void LeafClick_InvokesTheModel_OnlyAfterTheClickHasFinishedRouting()
    {
        int invoked = 0;
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        file.Items.Add(new MenuItemModel("Leaf", () => invoked++));
        model.TopLevelItems.Add(file);
        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);
        NativeMenuItem leaf = (NativeMenuItem)((NativeMenuItem)menu.Items[0]).Menu!.Items[0];

        ((INativeMenuItemExporterEventsImplBridge)leaf).RaiseClicked();

        invoked.ShouldBe(0);
        Dispatcher.UIThread.RunJobs();
        invoked.ShouldBe(1);
    }

    [AvaloniaFact]
    public void Build_ExpandsAStepperRow_IntoPlainItems_ThatRunItsActions()
    {
        // The macOS menu bar cannot draw a row with buttons, so a stepper becomes three ordinary items.
        List<string> ran = new List<string>();
        MenuStepper zoom = new MenuStepper(
            "Zoom", "Zoom In", "Zoom Out", "Actual Size",
            increase: () => ran.Add("in"), decrease: () => ran.Add("out"), reset: () => ran.Add("reset"));
        MenuModel model = new MenuModel();
        MenuItemModel view = new MenuItemModel("View");
        view.Items.Add(new MenuItemModel("Theming"));
        view.Items.Add(new MenuItemModel("Zoom") { Stepper = zoom });
        view.Items.Add(new MenuItemModel("Focus on Selection"));
        model.TopLevelItems.Add(view);

        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model, KeyModifiers.Meta, invokeAfterClick: action => action());

        IList<NativeMenuItemBase> items = ((NativeMenuItem)menu.Items[0]).Menu!.Items;
        items.OfType<NativeMenuItem>().Select(item => item.Header).ToArray()
            .ShouldBe(new[] { "Theming", "Zoom In", "Zoom Out", "Actual Size", "Focus on Selection" });
        foreach (string header in new[] { "Zoom In", "Zoom Out", "Actual Size" })
        {
            ((INativeMenuItemExporterEventsImplBridge)items.OfType<NativeMenuItem>().Single(item => item.Header == header)).RaiseClicked();
        }
        ran.ShouldBe(new[] { "in", "out", "reset" });
    }

    [AvaloniaFact]
    public void LeafClick_RunsTheModel_ThroughTheSuppliedScheduler()
    {
        // The shell passes a scheduler that waits for AppKit's menu tracking to end (#4982).
        int invoked = 0;
        List<Action> scheduled = new List<Action>();
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        file.Items.Add(new MenuItemModel("Leaf", () => invoked++));
        model.TopLevelItems.Add(file);
        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model, KeyModifiers.Meta, invokeAfterClick: scheduled.Add);
        NativeMenuItem leaf = (NativeMenuItem)((NativeMenuItem)menu.Items[0]).Menu!.Items[0];

        ((INativeMenuItemExporterEventsImplBridge)leaf).RaiseClicked();
        Dispatcher.UIThread.RunJobs();

        invoked.ShouldBe(0);
        scheduled.ShouldHaveSingleItem().Invoke();
        invoked.ShouldBe(1);
    }

    [AvaloniaFact]
    public void BuildAppMenu_RunsAbout_ThroughTheSuppliedScheduler()
    {
        int invoked = 0;
        List<Action> scheduled = new List<Action>();

        NativeMenu appMenu = AvaloniaNativeMenuBuilder.BuildAppMenu(() => invoked++, scheduled.Add);
        ((INativeMenuItemExporterEventsImplBridge)appMenu.Items[0]).RaiseClicked();
        Dispatcher.UIThread.RunJobs();

        invoked.ShouldBe(0);
        scheduled.ShouldHaveSingleItem().Invoke();
        invoked.ShouldBe(1);
    }

    [AvaloniaFact]
    public void Build_BindsTheModelsGesture_WithThePlatformCommandModifier()
    {
        // On macOS AppKit matches a menu item's key equivalent before the window's keyDown, so the
        // native item is the one binding for Cmd+Z and the hotkey manager never sees the key.
        MenuModel model = new MenuModel();
        MenuItemModel edit = new MenuItemModel("Edit");
        edit.Items.Add(new MenuItemModel("Undo") { Gesture = KeyCombination.Ctrl(GumKey.Z) });
        edit.Items.Add(new MenuItemModel("Add"));
        model.TopLevelItems.Add(edit);

        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model, KeyModifiers.Meta);

        NativeMenu editMenu = ((NativeMenuItem)menu.Items[0]).Menu.ShouldNotBeNull();
        KeyGesture gesture = ((NativeMenuItem)editMenu.Items[0]).Gesture.ShouldNotBeNull();
        gesture.Key.ShouldBe(Key.Z);
        gesture.KeyModifiers.ShouldBe(KeyModifiers.Meta);
        ((NativeMenuItem)editMenu.Items[1]).Gesture.ShouldBeNull();
    }

    [AvaloniaFact]
    public void Build_BindsGestures_OnlyWhileTheOwnerWindowIsActive()
    {
        // A key equivalent fires app-wide, so while a dialog is the key window it would take Cmd+Z
        // from the dialog's text box. Dropping the gestures while the owner is inactive lets the key
        // reach the dialog like on Windows, where the in-window menu displays the shortcut only.
        MenuModel model = new MenuModel();
        MenuItemModel edit = new MenuItemModel("Edit");
        edit.Items.Add(new MenuItemModel("Undo") { Gesture = KeyCombination.Ctrl(GumKey.Z) });
        model.TopLevelItems.Add(edit);
        OwnerActivity isOwnerActive = new OwnerActivity();

        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model, KeyModifiers.Meta, isOwnerActive);
        NativeMenu editMenu = ((NativeMenuItem)menu.Items[0]).Menu.ShouldNotBeNull();
        NativeMenuItem undo = (NativeMenuItem)editMenu.Items[0];

        isOwnerActive.Set(true);
        undo.Gesture.ShouldNotBeNull().Key.ShouldBe(Key.Z);

        isOwnerActive.Set(false);
        undo.Gesture.ShouldBeNull();
        // A collection change rebuilds the submenu's items; the new ones start unbound too.
        edit.Items.Add(new MenuItemModel("Redo") { Gesture = KeyCombination.Ctrl(GumKey.Y) });
        undo = (NativeMenuItem)editMenu.Items[0];
        NativeMenuItem redo = (NativeMenuItem)editMenu.Items[1];
        undo.Gesture.ShouldBeNull();
        redo.Gesture.ShouldBeNull();

        isOwnerActive.Set(true);
        undo.Gesture.ShouldNotBeNull().Key.ShouldBe(Key.Z);
        redo.Gesture.ShouldNotBeNull().Key.ShouldBe(Key.Y);
    }

    private sealed class OwnerActivity : IObservable<bool>
    {
        private readonly List<IObserver<bool>> _observers = new List<IObserver<bool>>();

        public void Set(bool isActive) => _observers.ForEach(observer => observer.OnNext(isActive));

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            _observers.Add(observer);
            return new Unsubscriber(() => _observers.Remove(observer));
        }

        private sealed class Unsubscriber(Action unsubscribe) : IDisposable
        {
            public void Dispose() => unsubscribe();
        }
    }

    [AvaloniaFact]
    public void BuildAppMenu_HasAboutGum_ThatRunsTheAboutAction()
    {
        int invoked = 0;

        NativeMenu appMenu = AvaloniaNativeMenuBuilder.BuildAppMenu(() => invoked++);

        NativeMenuItem about = appMenu.Items.ShouldHaveSingleItem().ShouldBeOfType<NativeMenuItem>();
        about.Header.ShouldBe("About Gum");
        ((INativeMenuItemExporterEventsImplBridge)about).RaiseClicked();
        Dispatcher.UIThread.RunJobs();
        invoked.ShouldBe(1);
    }

    [AvaloniaFact]
    public void Build_FollowsEveryKindOfCollectionChange_InOrder_WithAStepperAsThreeItems()
    {
        MenuStepper zoom = new MenuStepper("Zoom", "Zoom In", "Zoom Out", "Actual Size", () => { }, () => { }, () => { });
        MenuModel model = new MenuModel();
        MenuItemModel view = new MenuItemModel("View");
        MenuItemModel a = new MenuItemModel("A");
        MenuItemModel stepper = new MenuItemModel("Zoom") { Stepper = zoom };
        MenuItemModel c = new MenuItemModel("C");
        view.Items.Add(a);
        view.Items.Add(stepper);
        model.TopLevelItems.Add(view);
        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);
        NativeMenu viewMenu = ((NativeMenuItem)menu.Items[0]).Menu!;

        view.Items.Add(c);
        Headers(viewMenu).ShouldBe(new[] { "A", "Zoom In", "Zoom Out", "Actual Size", "C" });
        view.Items.Move(1, 2);
        Headers(viewMenu).ShouldBe(new[] { "A", "C", "Zoom In", "Zoom Out", "Actual Size" });
        view.Items.Insert(1, MenuItemModel.Separator());
        Headers(viewMenu).ShouldBe(new[] { "A", "-", "C", "Zoom In", "Zoom Out", "Actual Size" });
        view.Items[3] = new MenuItemModel("D");
        Headers(viewMenu).ShouldBe(new[] { "A", "-", "C", "D" });
        view.Items.RemoveAt(0);
        Headers(viewMenu).ShouldBe(new[] { "-", "C", "D" });
        view.Items.Clear();
        Headers(viewMenu).ShouldBeEmpty();
        view.Items.Add(stepper);
        Headers(viewMenu).ShouldBe(new[] { "Zoom In", "Zoom Out", "Actual Size" });

        MenuItemModel leaf = new MenuItemModel("Leaf");
        model.TopLevelItems.Insert(0, leaf);
        leaf.Items.Add(new MenuItemModel("Child"));
        Headers(menu).ShouldBe(new[] { "Leaf", "View" });
        Headers(((NativeMenuItem)menu.Items[0]).Menu!).ShouldBe(new[] { "Child" });
    }

    [AvaloniaFact]
    public void CollectionChange_KeepsTheItemsThatDidNotChange()
    {
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        MenuItemModel a = new MenuItemModel("A");
        MenuItemModel b = new MenuItemModel("B");
        b.Items.Add(new MenuItemModel("B1"));
        file.Items.Add(a);
        file.Items.Add(b);
        model.TopLevelItems.Add(file);
        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);
        NativeMenuItem fileItem = (NativeMenuItem)menu.Items[0];
        NativeMenuItem aItem = (NativeMenuItem)fileItem.Menu!.Items[0];
        NativeMenuItem bItem = (NativeMenuItem)fileItem.Menu.Items[1];
        NativeMenuItem b1Item = (NativeMenuItem)bItem.Menu!.Items[0];

        file.Items.Insert(0, new MenuItemModel("Z"));
        model.TopLevelItems.Add(new MenuItemModel("Help"));
        file.Items.Move(1, 2);

        menu.Items[0].ShouldBeSameAs(fileItem);
        fileItem.Menu.Items[2].ShouldBeSameAs(aItem);
        fileItem.Menu.Items[1].ShouldBeSameAs(bItem);
        bItem.Menu.Items[0].ShouldBeSameAs(b1Item);
    }

    [AvaloniaFact]
    public void RemovedItems_NoLongerFollowTheirModels()
    {
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        MenuItemModel removed = new MenuItemModel("Removed");
        MenuItemModel replaced = new MenuItemModel("Replaced");
        MenuItemModel cleared = new MenuItemModel("Cleared");
        file.Items.Add(removed);
        file.Items.Add(replaced);
        file.Items.Add(cleared);
        model.TopLevelItems.Add(file);
        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);
        IList<NativeMenuItemBase> fileItems = ((NativeMenuItem)menu.Items[0]).Menu!.Items;
        NativeMenuItem removedItem = (NativeMenuItem)fileItems[0];
        NativeMenuItem replacedItem = (NativeMenuItem)fileItems[1];
        NativeMenuItem clearedItem = (NativeMenuItem)fileItems[2];

        file.Items.Remove(removed);
        file.Items[0] = new MenuItemModel("Replacement");
        file.Items.Clear();
        foreach (MenuItemModel item in new[] { removed, replaced, cleared })
        {
            item.Header = "Changed";
            item.Items.Add(new MenuItemModel("Child"));
        }

        foreach ((NativeMenuItem item, string header) in new[] { (removedItem, "Removed"), (replacedItem, "Replaced"), (clearedItem, "Cleared") })
        {
            item.Header.ShouldBe(header);
            item.Menu.ShouldBeNull();
        }
    }

    private static string[] Headers(NativeMenu menu) =>
        menu.Items.Select(item => item is NativeMenuItemSeparator ? "-" : ((NativeMenuItem)item).Header!).ToArray();
}
