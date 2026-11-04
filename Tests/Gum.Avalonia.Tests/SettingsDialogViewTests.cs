using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Settings;
using Gum.Avalonia.Tests.Harness;
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
        window.GetVisualDescendants().OfType<CheckBox>().Select(box => box.Content).ShouldContain("Save automatically");
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
    public void AppearancePage_ShowsEachColorAsARow_WithAResetOnlyWhileItIsCustom()
    {
        Gum.Dialogs.IThemingService theming = TestAppBuilder.Services.GetRequiredService<Gum.Dialogs.IThemingService>();
        System.Drawing.Color? original = theming.GuideLine;
        theming.GuideLine = null;
        try
        {
            SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
            viewModel.SelectPage(SettingsPageIds.Appearance);
            DialogWindow window = Open(viewModel);

            window.GetVisualDescendants().OfType<Gum.Avalonia.Plugins.VariableGrid.ColorDisplay>().Count().ShouldBe(6);
            List<Button> Resets() => window.GetVisualDescendants().OfType<Button>().Where(button => button.Content as string == "↺").ToList();
            Resets().ShouldAllBe(button => !button.IsVisible, "nothing is custom, so nothing offers a reset");

            theming.GuideLine = System.Drawing.Color.FromArgb(255, 1, 2, 3);
            viewModel.SelectPage(SettingsPageIds.General);
            viewModel.SelectPage(SettingsPageIds.Appearance);
            Layout(window);
            Resets().Count(button => button.IsVisible).ShouldBe(1);

            Resets().Single(button => button.IsVisible).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Layout(window);

            theming.GuideLine.ShouldBeNull("reset puts the color back to its default");
            Resets().ShouldAllBe(button => !button.IsVisible);
            window.Close();
        }
        finally
        {
            theming.GuideLine = original;
        }
    }

    [AvaloniaFact]
    public void AppearancePage_ResetAll_IsDisabledAtDefaults_AndResetsEveryChangedColorAtOnce()
    {
        Gum.Dialogs.IThemingService theming = TestAppBuilder.Services.GetRequiredService<Gum.Dialogs.IThemingService>();
        System.Drawing.Color? originalGuide = theming.GuideLine;
        System.Drawing.Color? originalAccent = theming.Accent;
        theming.GuideLine = null;
        theming.Accent = null;
        try
        {
            SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
            viewModel.SelectPage(SettingsPageIds.Appearance);
            DialogWindow window = Open(viewModel);
            Button ResetAll() => window.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "Reset All");
            List<Button> Resets() => window.GetVisualDescendants().OfType<Button>().Where(button => button.Content as string == "↺").ToList();
            ResetAll().IsEnabled.ShouldBeFalse("no color differs from its default");

            theming.GuideLine = System.Drawing.Color.FromArgb(255, 1, 2, 3);
            theming.Accent = System.Drawing.Color.FromArgb(255, 4, 5, 6);
            viewModel.SelectPage(SettingsPageIds.General);
            viewModel.SelectPage(SettingsPageIds.Appearance);
            Layout(window);
            ResetAll().IsEnabled.ShouldBeTrue();
            Resets().Count(button => button.IsVisible).ShouldBe(2);

            // It sits below the last row, inside the page's width, and its label is not clipped.
            Button resetAll = ResetAll();
            global::Avalonia.Point buttonTop = resetAll.TranslatePoint(new global::Avalonia.Point(0, 0), window)!.Value;
            double lastRowBottom = window.GetVisualDescendants().OfType<Gum.Avalonia.Plugins.VariableGrid.ColorDisplay>()
                .Max(display => display.TranslatePoint(new global::Avalonia.Point(0, display.Bounds.Height), window)!.Value.Y);
            buttonTop.Y.ShouldBeGreaterThan(lastRowBottom, "Reset All must not overlap the rows");
            resetAll.Bounds.Width.ShouldBeGreaterThanOrEqualTo(resetAll.DesiredSize.Width);
            (buttonTop.X + resetAll.Bounds.Width).ShouldBeLessThan(window.Bounds.Width);

            ResetAll().RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Layout(window);

            theming.GuideLine.ShouldBeNull();
            theming.Accent.ShouldBeNull();
            Resets().ShouldAllBe(button => !button.IsVisible);
            ResetAll().IsEnabled.ShouldBeFalse();
            window.Close();
        }
        finally
        {
            theming.GuideLine = originalGuide;
            theming.Accent = originalAccent;
        }
    }

    [AvaloniaFact]
    public void AppearancePage_KeepsItsChoicesNarrow_AndLeftAlignedWithTheirLabels()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Appearance);
        DialogWindow window = Open(viewModel);

        ComboBox themeMode = window.GetVisualDescendants().OfType<ComboBox>().Single();
        List<Gum.Avalonia.Plugins.VariableGrid.ColorDisplay> colorRows =
            window.GetVisualDescendants().OfType<Gum.Avalonia.Plugins.VariableGrid.ColorDisplay>().ToList();
        TextBlock accentLabel = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Accent color");

        themeMode.Bounds.Width.ShouldBeLessThan(260, "the theme list is a short choice, not a full-width bar");
        colorRows.ShouldNotBeEmpty();
        double themeLeft = themeMode.TranslatePoint(new Point(0, 0), window)!.Value.X;
        foreach (Gum.Avalonia.Plugins.VariableGrid.ColorDisplay row in colorRows)
        {
            double swatchLeft = row.Swatch.TranslatePoint(new Point(0, 0), window)!.Value.X;
            row.Swatch.Bounds.Width.ShouldBeLessThan(260, "a color is a compact swatch, not a bar across the page");
            swatchLeft.ShouldBe(themeLeft, 8, "every choice starts at the same left edge, beside its label");
        }
        double labelRight = accentLabel.TranslatePoint(new Point(accentLabel.Bounds.Width, 0), window)!.Value.X;
        (themeLeft - labelRight).ShouldBeLessThan(40, "no big gap between a label and its choice");
        window.Close();
    }

    [AvaloniaFact]
    public void AppearancePage_ColorHexField_FitsSixHexCharactersAtEveryZoom()
    {
        Gum.Services.IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<Gum.Services.IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Appearance);
        DialogWindow window = Open(viewModel);
        using IDisposable follower = Gum.Avalonia.Shell.AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            foreach (double zoom in new[] { 9d, 12d, 16d, 24d })
            {
                scale.BaseFontSize = zoom;
                ScreenshotWindow.Pump(window);
                foreach (Gum.Avalonia.Plugins.VariableGrid.ColorDisplay row in window.GetVisualDescendants().OfType<Gum.Avalonia.Plugins.VariableGrid.ColorDisplay>())
                {
                    TextBox hex = row.HexTextBox;
                    global::Avalonia.Controls.Presenters.TextPresenter presenter = hex.GetVisualDescendants().OfType<global::Avalonia.Controls.Presenters.TextPresenter>().First();
                    double widest = "0123456789ABCDEF".Select(digit => new global::Avalonia.Media.FormattedText(
                            new string(digit, 6), System.Globalization.CultureInfo.InvariantCulture,
                            global::Avalonia.Media.FlowDirection.LeftToRight,
                            new global::Avalonia.Media.Typeface(hex.FontFamily, hex.FontStyle, hex.FontWeight),
                            hex.FontSize, null).Width).Max();
                    presenter.Bounds.Width.ShouldBeGreaterThanOrEqualTo(widest + 1,
                        $"six hex characters fit the field at zoom {zoom}");
                }
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AppearancePage_ResetButton_HasATooltip()
    {
        Gum.Dialogs.IThemingService theming = TestAppBuilder.Services.GetRequiredService<Gum.Dialogs.IThemingService>();
        System.Drawing.Color? original = theming.GuideLine;
        theming.GuideLine = System.Drawing.Color.FromArgb(255, 1, 2, 3);
        try
        {
            SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
            viewModel.SelectPage(SettingsPageIds.Appearance);
            DialogWindow window = Open(viewModel);

            Button reset = window.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "↺" && button.IsVisible);

            ToolTip.GetTip(reset).ShouldBe("Reset to Default", "an icon-only button says what it does");
            window.Close();
        }
        finally
        {
            theming.GuideLine = original;
        }
    }

    [AvaloniaFact]
    public void Search_OnTheAppearancePage_MarksTheMatchingRowsByLabelOrHint()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Appearance);
        DialogWindow window = Open(viewModel);
        SettingsRowsPageView page = window.GetVisualDescendants().OfType<SettingsRowsPageView>().First();

        viewModel.SearchText = "checkerboard";
        Layout(window);

        page.Grid.Categories.SelectMany(category => category.Members).Where(row => row.IsSearchMatch)
            .Select(row => row.DisplayName).ShouldBe(new[] { "Editor background", "Checker color" });
        viewModel.SearchText = "";
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
        TextBlock lastRow = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Project-wide usings");
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
        window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain("Undo");
        window.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain(TestAppBuilder.Services.GetRequiredService<Gum.Managers.IKeyCombinationFormatter>().Format(TestAppBuilder.Services.GetRequiredService<Gum.Managers.IHotkeyManager>().Undo!));
        window.Close();
    }

    [AvaloniaFact]
    public void PluginsPage_IsRows_ScrollingWithThePage_AndOffersOneTogglePerPlugin()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Plugins);
        DialogWindow window = Open(viewModel, maxHeight: 360);

        SettingsRowsPageView page = window.GetVisualDescendants().OfType<SettingsRowsPageView>().Single();
        TextBlock title = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Plugins" && text.FontSize >= 16);
        title.GetVisualAncestors().OfType<ScrollViewer>().ShouldNotBeEmpty("a rows page scrolls with its title, as the other rows pages do");
        window.GetVisualDescendants().OfType<TabControl>().ShouldBeEmpty();
        page.Grid.Categories.First().Members.ShouldNotBeEmpty();
        page.Grid.Categories.First().Members.ShouldAllBe(row => row.PreferredDisplayer == null && row.Name.StartsWith("Plugin:"));
        window.Close();
    }

    [AvaloniaFact]
    public void Search_OnThePluginsPage_OpensGumInternals_WhenARequiredPluginMatches()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Plugins);
        DialogWindow window = Open(viewModel);
        SettingsRowsPageView page = window.GetVisualDescendants().OfType<SettingsRowsPageView>().First();
        WpfDataUi.DataTypes.MemberCategory internals = page.Grid.Categories.Single(category => category.Name == "Gum internals");
        internals.IsExpanded.ShouldBeFalse("Gum internals starts collapsed");
        string name = internals.Members.First().DisplayName;

        viewModel.SearchText = name;
        Layout(window);

        internals.IsExpanded.ShouldBeTrue();
        internals.Members.Where(row => row.IsSearchMatch).Select(row => row.DisplayName).ShouldContain(name);
        viewModel.SearchText = "";
        window.Close();
    }

    [AvaloniaFact]
    public void Search_OnThePluginsPage_MarksThePluginsWhoseNameOrHintMatches()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Plugins);
        DialogWindow window = Open(viewModel);
        SettingsRowsPageView page = window.GetVisualDescendants().OfType<SettingsRowsPageView>().First();
        string name = page.Grid.Categories.First().Members.First().DisplayName;

        viewModel.SearchText = name;
        Layout(window);

        page.Grid.Categories.SelectMany(category => category.Members).Where(row => row.IsSearchMatch)
            .Select(row => row.DisplayName).ShouldContain(name);
        viewModel.SearchText = "";
        Layout(window);
        page.Grid.Categories.SelectMany(category => category.Members).ShouldAllBe(row => !row.IsSearchMatch);
        window.Close();
    }

    [AvaloniaFact]
    public void SearchBox_HasAnXThatClearsTheSearch_ShownOnlyWhileThereIsText()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        DialogWindow window = Open(viewModel);
        Button clear = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == SettingsDialogView.ClearSearchButtonName);
        clear.IsEffectivelyVisible.ShouldBeFalse("nothing to clear yet");

        viewModel.SearchText = "plug";
        Layout(window);
        clear.IsEffectivelyVisible.ShouldBeTrue();
        Click(window, clear);

        viewModel.SearchText.ShouldBe("");
        clear.IsEffectivelyVisible.ShouldBeFalse();
        window.GetVisualDescendants().OfType<TextBox>().First().Text.ShouldBeNullOrEmpty();
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


    [AvaloniaFact]
    public void Search_OnTheGeneralPage_MarksTheMatchingRowsByLabelOrHint()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.General);
        DialogWindow window = Open(viewModel);
        SettingsRowsPageView page = window.GetVisualDescendants().OfType<SettingsRowsPageView>().First();

        viewModel.SearchText = "project panel";
        Layout(window);

        page.Grid.Categories.SelectMany(category => category.Members).Where(row => row.IsSearchMatch)
            .Select(row => row.DisplayName).ShouldBe(new[] { "Standards palette" });
        viewModel.SearchText = "";
        window.Close();
    }

    [AvaloniaFact]
    public void Search_OnARowsPage_MarksTheMatchingRows_AndHidesNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.Canvas);
        DialogWindow window = Open(viewModel);
        SettingsRowsPageView page = window.GetVisualDescendants().OfType<SettingsRowsPageView>().First();
        int rowCount = page.Grid.Categories.Sum(category => category.Members.Count);

        viewModel.SearchText = "canvas width";
        Layout(window);

        viewModel.SelectedPage!.Id.ShouldBe(SettingsPageIds.Canvas, "the page stays selected while it has a match");
        page.Grid.Categories.SelectMany(category => category.Members).Count().ShouldBe(rowCount);
        page.Grid.Categories.SelectMany(category => category.Members).Where(row => row.IsSearchMatch)
            .Select(row => row.DisplayName).ShouldBe(new[] { "Canvas width" });

        viewModel.SearchText = "";
        Layout(window);
        page.Grid.Categories.SelectMany(category => category.Members).ShouldAllBe(row => !row.IsSearchMatch);
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
