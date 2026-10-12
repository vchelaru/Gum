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

    private static void Click(Control control)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(control)!;
        Point point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), topLevel)!.Value;
        topLevel.MouseDown(point, MouseButton.Left);
        topLevel.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
