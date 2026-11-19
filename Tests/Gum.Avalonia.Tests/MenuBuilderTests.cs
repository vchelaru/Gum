using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The main menu rendered from the shared <see cref="MenuModel"/>.</summary>
public class MenuBuilderTests
{
    [AvaloniaFact]
    public void LeafClick_InvokesTheModel_OnlyAfterTheClickHasFinishedRouting()
    {
        // Many menu actions show a dialog synchronously (a nested dispatcher loop). Invoked inside
        // the click, the dialog opened while the menu was still open, and the menu's light-dismiss
        // swallowed the first click into the dialog. The action must run after the menu has closed.
        int invoked = 0;
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        file.Items.Add(new MenuItemModel("Leaf", () => invoked++));
        model.TopLevelItems.Add(file);
        Menu menu = AvaloniaMenuBuilder.Build(model);
        Window window = new Window { Content = menu, Width = 300, Height = 200 };
        window.Show();
        MenuItem top = (MenuItem)menu.Items[0]!;
        MenuItem leaf = (MenuItem)top.Items[0]!;

        leaf.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        invoked.ShouldBe(0);
        Dispatcher.UIThread.RunJobs();
        invoked.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void Build_ShowsTheModelsGesture_WithThePlatformCommandModifier()
    {
        MenuModel model = new MenuModel();
        MenuItemModel edit = new MenuItemModel("Edit");
        edit.Items.Add(new MenuItemModel("Undo") { Gesture = KeyCombination.Ctrl(GumKey.Z) });
        model.TopLevelItems.Add(edit);

        Menu menu = AvaloniaMenuBuilder.Build(model, KeyModifiers.Control);

        MenuItem undo = (MenuItem)((MenuItem)menu.Items[0]!).Items[0]!;
        KeyGesture gesture = undo.InputGesture.ShouldNotBeNull();
        gesture.Key.ShouldBe(Key.Z);
        gesture.KeyModifiers.ShouldBe(KeyModifiers.Control);
    }

    [AvaloniaFact]
    public void Build_DrawsAStepperRow_WhoseButtonsRunTheActions_WithoutClosingTheMenu()
    {
        // A Chrome-style row: "Zoom  -  100%  +". Clicking a button must leave the menu open so it can be clicked again.
        List<string> ran = new List<string>();
        MenuStepper zoom = new MenuStepper(
            "Zoom", "Zoom In", "Zoom Out", "Actual Size",
            increase: () => ran.Add("in"), decrease: () => ran.Add("out"), reset: () => ran.Add("reset"));
        zoom.ValueText = "100%";
        MenuModel model = new MenuModel();
        MenuItemModel view = new MenuItemModel("View");
        view.Items.Add(new MenuItemModel("Zoom") { Stepper = zoom });
        model.TopLevelItems.Add(view);
        Menu menu = AvaloniaMenuBuilder.Build(model);
        Window window = new Window { Content = menu, Width = 400, Height = 300 };
        window.Show();
        MenuItem viewItem = (MenuItem)menu.Items[0]!;
        MenuStepperRow row = ((MenuItem)viewItem.Items[0]!).Header.ShouldBeOfType<MenuStepperRow>();
        viewItem.Open();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        Click(row.PlusButton);
        Click(row.MinusButton);
        Click(row.PlusButton);
        Click(row.ValueButton);
        Click(row.Children[0]); // the label is not a button: a click there must not close the menu either

        ran.ShouldBe(new[] { "in", "out", "in", "reset" });
        viewItem.IsSubMenuOpen.ShouldBeTrue("the menu stays open while the buttons are used");
        zoom.ValueText = "108%";
        row.ValueButton.Content.ShouldBe("108%");
        window.Close();
    }

    [AvaloniaFact]
    public void Build_FollowsEveryKindOfCollectionChange_InOrder()
    {
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        MenuItemModel a = new MenuItemModel("A");
        MenuItemModel b = new MenuItemModel("B");
        MenuItemModel c = new MenuItemModel("C");
        file.Items.Add(a);
        file.Items.Add(b);
        model.TopLevelItems.Add(file);
        Menu menu = AvaloniaMenuBuilder.Build(model);
        MenuItem fileItem = (MenuItem)menu.Items[0]!;

        file.Items.Insert(1, c);
        Headers(fileItem.Items).ShouldBe(new[] { "A", "C", "B" });
        file.Items.Insert(0, MenuItemModel.Separator());
        Headers(fileItem.Items).ShouldBe(new[] { "-", "A", "C", "B" });
        file.Items.RemoveAt(0);
        file.Items.Move(0, 2);
        Headers(fileItem.Items).ShouldBe(new[] { "C", "B", "A" });
        file.Items[1] = new MenuItemModel("D");
        Headers(fileItem.Items).ShouldBe(new[] { "C", "D", "A" });
        file.Items.Remove(c);
        Headers(fileItem.Items).ShouldBe(new[] { "D", "A" });
        file.Items.Clear();
        Headers(fileItem.Items).ShouldBeEmpty();
        file.Items.Add(b);
        Headers(fileItem.Items).ShouldBe(new[] { "B" });
        model.TopLevelItems.Insert(0, new MenuItemModel("Edit"));
        Headers(menu.Items).ShouldBe(new[] { "Edit", "File" });
    }

    [AvaloniaFact]
    public void CollectionChange_KeepsTheControlsOfItemsThatDidNotChange()
    {
        // Plugins add menu entries one at a time during startup; rebuilding every item on each one
        // was most of the cost of building the menu.
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        MenuItemModel a = new MenuItemModel("A");
        MenuItemModel b = new MenuItemModel("B");
        b.Items.Add(new MenuItemModel("B1"));
        file.Items.Add(a);
        file.Items.Add(b);
        model.TopLevelItems.Add(file);
        Menu menu = AvaloniaMenuBuilder.Build(model);
        MenuItem fileItem = (MenuItem)menu.Items[0]!;
        MenuItem aItem = (MenuItem)fileItem.Items[0]!;
        MenuItem bItem = (MenuItem)fileItem.Items[1]!;
        MenuItem b1Item = (MenuItem)bItem.Items[0]!;

        file.Items.Insert(0, new MenuItemModel("Z"));
        model.TopLevelItems.Add(new MenuItemModel("Help"));
        file.Items.Move(1, 2);

        menu.Items[0].ShouldBeSameAs(fileItem);
        fileItem.Items[2].ShouldBeSameAs(aItem);
        fileItem.Items[1].ShouldBeSameAs(bItem);
        bItem.Items[0].ShouldBeSameAs(b1Item);
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
        Menu menu = AvaloniaMenuBuilder.Build(model);
        MenuItem fileItem = (MenuItem)menu.Items[0]!;
        MenuItem removedItem = (MenuItem)fileItem.Items[0]!;
        MenuItem replacedItem = (MenuItem)fileItem.Items[1]!;
        MenuItem clearedItem = (MenuItem)fileItem.Items[2]!;

        file.Items.Remove(removed);
        file.Items[0] = new MenuItemModel("Replacement");
        file.Items.Clear();
        foreach (MenuItemModel item in new[] { removed, replaced, cleared })
        {
            item.Header = "Changed";
            item.Items.Add(new MenuItemModel("Child"));
        }

        foreach ((MenuItem item, string header) in new[] { (removedItem, "Removed"), (replacedItem, "Replaced"), (clearedItem, "Cleared") })
        {
            item.Header.ShouldBe(header);
            item.Items.Count.ShouldBe(0);
        }
    }

    [AvaloniaFact]
    public void RemovedStepperRow_NoLongerFollowsItsStepper()
    {
        MenuStepper zoom = new MenuStepper("Zoom", "Zoom In", "Zoom Out", "Actual Size", () => { }, () => { }, () => { });
        zoom.ValueText = "100%";
        MenuModel model = new MenuModel();
        MenuItemModel view = new MenuItemModel("View");
        view.Items.Add(new MenuItemModel("Zoom") { Stepper = zoom });
        model.TopLevelItems.Add(view);
        Menu menu = AvaloniaMenuBuilder.Build(model);
        MenuStepperRow row = (MenuStepperRow)((MenuItem)((MenuItem)menu.Items[0]!).Items[0]!).Header!;

        view.Items.Clear();
        zoom.ValueText = "108%";

        row.ValueButton.Content.ShouldBe("100%");
    }

    private static string[] Headers(ItemCollection items) =>
        items.Select(item => item is Separator ? "-" : (string)((MenuItem)item!).Header!).ToArray();

    private static void Click(Control control)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(control)!;
        Point point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), topLevel)!.Value;
        topLevel.MouseDown(point, MouseButton.Left);
        topLevel.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
