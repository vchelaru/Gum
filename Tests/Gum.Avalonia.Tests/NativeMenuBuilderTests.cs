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
        saveItem.ToggleType.ShouldBe(NativeMenuItemToggleType.None);
        fileMenu.Items[1].ShouldBeOfType<NativeMenuItemSeparator>();
        NativeMenuItem gridItem = (NativeMenuItem)fileMenu.Items[2];
        gridItem.ToggleType.ShouldBe(NativeMenuItemToggleType.CheckBox);
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
}
