using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Settings window: a page list with Application and Project headers, the selected page's view
/// beside it, changes applying at once, and only a Close button.
/// </summary>
public class SettingsDialogViewTests
{
    private static DialogWindow Open(SettingsDialogViewModel viewModel)
    {
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        Layout(window);
        return window;
    }

    [AvaloniaFact]
    public void SettingsWindow_ShowsTheGeneralPage_WithOnlyACloseButton()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();

        DialogWindow window = Open(viewModel);

        ListBox pages = window.GetVisualDescendants().OfType<ListBox>().First();
        pages.Items.OfType<SettingsNavigationEntry>().Select(entry => entry.Title).ShouldContain("General");
        window.GetVisualDescendants().OfType<CheckBox>().Select(box => box.Content).ShouldContain("Auto save");
        Button close = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.AffirmativeButtonName);
        close.Content.ShouldBe("Close");
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.NegativeButtonName).IsVisible.ShouldBeFalse();
        window.Close();
    }

    [AvaloniaFact]
    public void SettingsWindow_ClickingAPage_ShowsItsView()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        DialogWindow window = Open(viewModel);

        ListBoxItem appearance = window.GetVisualDescendants().OfType<ListBoxItem>()
            .Single(item => (item.DataContext as SettingsNavigationEntry)?.Title == "Appearance");
        Click(window, appearance);

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.Appearance);
        window.GetVisualDescendants().OfType<ComboBox>().ShouldNotBeEmpty("the theme mode choice is on the page");
        window.GetVisualDescendants().OfType<CheckBox>().Select(box => box.Content).ShouldNotContain("Auto save");
        window.Close();
    }

    [AvaloniaFact]
    public void SettingsWindow_ClickingASectionHeader_KeepsThePageSelected()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        DialogWindow window = Open(viewModel);

        ListBoxItem header = window.GetVisualDescendants().OfType<ListBoxItem>()
            .First(item => (item.DataContext as SettingsNavigationEntry)?.IsHeader == true);
        Click(window, header);

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.General);
        window.Close();
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static Point CenterOf(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void Click(Window window, Control control)
    {
        Point point = CenterOf(control, window);
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Layout(window);
    }
}
