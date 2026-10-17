using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Tests.EndToEnd;
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
    private static DialogWindow Open(SettingsDialogViewModel viewModel, double maxHeight = double.PositiveInfinity)
    {
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view) { MaxHeight = maxHeight };
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
    public void AppearancePage_KeepsItsChoicesNarrow_AndLeftAlignedWithTheirLabels()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Appearance);
        DialogWindow window = Open(viewModel);

        ComboBox themeMode = window.GetVisualDescendants().OfType<ComboBox>().Single();
        List<ColorPicker> pickers = window.GetVisualDescendants().OfType<ColorPicker>().ToList();
        TextBlock accentLabel = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Accent Color");

        themeMode.Bounds.Width.ShouldBeLessThan(260, "the theme list is a short choice, not a full-width bar");
        pickers.ShouldNotBeEmpty();
        double themeLeft = themeMode.TranslatePoint(new Point(0, 0), window)!.Value.X;
        foreach (ColorPicker picker in pickers)
        {
            double pickerLeft = picker.TranslatePoint(new Point(0, 0), window)!.Value.X;
            pickerLeft.ShouldBe(themeLeft, 1, "every choice starts at the same left edge, beside its label");
        }
        double labelRight = accentLabel.TranslatePoint(new Point(accentLabel.Bounds.Width, 0), window)!.Value.X;
        (themeLeft - labelRight).ShouldBeLessThan(40, "no big gap between a label and its choice");
        window.Close();
    }

    [AvaloniaFact]
    public void ATallPage_ScrollsAsOne_WithTheTitleAndRows_WhileTheCloseButtonStaysPut()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.CodeGeneration);
        DialogWindow window = Open(viewModel, maxHeight: 360);

        TextBlock title = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Code generation" && text.FontSize >= 16);
        ScrollViewer pageScroller = title.GetVisualAncestors().OfType<ScrollViewer>().First();
        Button close = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.AffirmativeButtonName);
        ScrollViewer gridScroller = window.GetVisualDescendants().OfType<DataUiGrid>().Single().GetVisualDescendants().OfType<ScrollViewer>().First();

        pageScroller.Extent.Height.ShouldBeGreaterThan(pageScroller.Viewport.Height, "the page is taller than the window");
        close.GetVisualAncestors().ShouldNotContain(pageScroller, "Close is not part of the scrolling page");
        gridScroller.Extent.Height.ShouldBeLessThanOrEqualTo(gridScroller.Viewport.Height + 1, "the grid grows with the page instead of scrolling itself");

        pageScroller.ScrollToEnd();
        Layout(window);
        TextBlock lastRow = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Syntax Version");
        double lastRowBottom = lastRow.TranslatePoint(new Point(0, lastRow.Bounds.Height), pageScroller)!.Value.Y;
        lastRowBottom.ShouldBeLessThanOrEqualTo(pageScroller.Bounds.Height, "scrolling to the end reaches the last row");
        window.Close();
    }

    [AvaloniaFact]
    public void KeyboardShortcutsPage_IsPlainRows_ScrollingWithThePage()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.KeyboardShortcuts);
        DialogWindow window = Open(viewModel, maxHeight: 360);

        window.GetVisualDescendants().OfType<ListBox>().Count().ShouldBe(1, "only the page list is a list box; the shortcuts are not a second scroller");
        TextBlock title = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Keyboard shortcuts" && text.FontSize >= 16);
        ScrollViewer pageScroller = title.GetVisualAncestors().OfType<ScrollViewer>().First();
        pageScroller.Extent.Height.ShouldBeGreaterThan(pageScroller.Viewport.Height);
        window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain(text => text != null && text.StartsWith("Undo: "));
        window.Close();
    }

    [AvaloniaFact]
    public void PluginsPage_ScrollsItself_SoItsTitleStaysPut()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Plugins);
        DialogWindow window = Open(viewModel, maxHeight: 360);

        TextBlock title = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Plugins" && text.FontSize >= 16);

        title.GetVisualAncestors().OfType<ScrollViewer>().ShouldBeEmpty("a page with its own scrolling gets the bounded height instead");
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
