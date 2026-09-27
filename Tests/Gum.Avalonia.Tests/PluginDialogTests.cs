using Gum.Managers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Dispatcher = Avalonia.Threading.Dispatcher;
using Avalonia.VisualTree;
using Gum.Avalonia.Controls;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Plugins.PluginDialogs;
using Gum.Commands;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.ViewModels;
using ImportFromGumxPlugin;
using ImportFromGumxPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Avalonia views for the Gum Forms and Import from .gumx plugins' dialogs: the registry maps
/// each shared view model to its view, and the views bind to the view models the plugins create.
/// </summary>
public class PluginDialogTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private static DialogViewRegistry Registry => Services.GetRequiredService<DialogViewRegistry>();

    [AvaloniaFact]
    public void AddFormsView_BindsTheThemePickerAndTheDemoOption()
    {
        ThemeSelectionViewModel themeSelection = ActivatorUtilities.CreateInstance<ThemeSelectionViewModel>(Services);
        AddFormsViewModel viewModel = ActivatorUtilities.CreateInstance<AddFormsViewModel>(Services, themeSelection);

        Control view = Registry.CreateView(viewModel);
        Window window = new Window { Content = view };
        window.Show();

        view.ShouldBeOfType<AddFormsView>();
        view.GetVisualDescendants().OfType<ThemeSelectionView>().Single().DataContext.ShouldBeSameAs(themeSelection);
        view.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked = true;
        viewModel.IsIncludeDemoScreenGum.ShouldBeTrue();
        viewModel.Title.ShouldBe("Add Forms");
        window.Close();
    }

    [AvaloniaFact]
    public void ImportFromGumxView_ClickingARowTogglesItThroughTheViewModel()
    {
        ImportFromGumxViewModel viewModel = CreateImportViewModel();
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        ImportTreeNodeViewModel leaf = new ImportTreeNodeViewModel("Button", "Button", ElementItemType.Component);
        folder.Children.Add(leaf);
        viewModel.RootNodes.Add(folder);
        viewModel.IsPreviewLoaded = true;

        ImportFromGumxView view = (ImportFromGumxView)Registry.CreateView(viewModel);
        Window window = new Window { Content = view };
        window.Show();
        window.UpdateLayout();

        CheckBox leafBox = view.Tree.GetVisualDescendants().OfType<CheckBox>().First(box => box.DataContext == leaf);
        leafBox.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        leaf.IsChecked.ShouldBe(true);
        leafBox.IsChecked.ShouldBe(true);
        view.Tree.GetVisualDescendants().OfType<CheckBox>().First(box => box.DataContext == folder).IsChecked.ShouldBe(true);
        viewModel.Title.ShouldBe("Import from .gumx");
        window.Close();
    }

    [AvaloniaFact]
    public void ImportFromGumxDialog_OnAShortWindow_ScrollsOnlyTheTree()
    {
        // As WPF's ScrollContent="False": the tree scrolls inside the dialog while the destination
        // box and the buttons stay on screen, rather than the whole view scrolling.
        ImportFromGumxViewModel viewModel = CreateImportViewModel();
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Components", "Components");
        for (int i = 0; i < 60; i++)
        {
            folder.Children.Add(new ImportTreeNodeViewModel($"Button{i}", $"Button{i}", ElementItemType.Component));
        }
        viewModel.RootNodes.Add(folder);
        viewModel.IsPreviewLoaded = true;

        ImportFromGumxView view = (ImportFromGumxView)Registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view) { MaxHeight = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        window.Bounds.Height.ShouldBeLessThanOrEqualTo(400);
        ScrollViewer treeScroller = view.Tree.GetVisualDescendants().OfType<ScrollViewer>().First();
        window.GetVisualDescendants().OfType<ScrollViewer>()
            .Where(scroller => scroller.Extent.Height > scroller.Viewport.Height)
            .ShouldBe(new[] { treeScroller });
        ShouldBeInsideWindow(view.GetVisualDescendants().OfType<TextBox>().Last(), window);
        ShouldBeInsideWindow(window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.AffirmativeButtonName), window);
        window.Close();
    }

    [AvaloniaFact]
    public void ImportFromGumxDialog_OnATallScreen_OpensAtItsFixedSize()
    {
        ImportFromGumxViewModel viewModel = CreateImportViewModel();

        ImportFromGumxView view = (ImportFromGumxView)Registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view) { MaxHeight = 2000 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        // The WPF view's size before any preview loads, so loading one does not grow the window.
        view.Bounds.Width.ShouldBe(600);
        view.Bounds.Height.ShouldBe(560);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void ImportFromGumxView_DetailsLink_UsesTheThemeLinkColor(string variantName)
    {
        // The button theme's white text vanished on the light tree (#5351); the link takes the
        // palette's link color in each theme instead.
        global::Avalonia.Styling.ThemeVariant variant = variantName == "Light"
            ? global::Avalonia.Styling.ThemeVariant.Light
            : global::Avalonia.Styling.ThemeVariant.Dark;
        ImportFromGumxViewModel viewModel = CreateImportViewModel();
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel("Standards", "Standards");
        ImportTreeNodeViewModel text = new ImportTreeNodeViewModel("Text", "Text", ElementItemType.Standard)
        {
            StandardDiffRows = new[] { new StandardDiffRowViewModel("Variable", "FontSize differs") },
        };
        folder.Children.Add(text);
        viewModel.RootNodes.Add(folder);
        viewModel.IsPreviewLoaded = true;

        ImportFromGumxView view = (ImportFromGumxView)Registry.CreateView(viewModel);
        Window window = new Window { Content = view, RequestedThemeVariant = variant };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        try
        {
            global::Avalonia.Controls.TextBlock details = view.Tree.GetVisualDescendants()
                .OfType<global::Avalonia.Controls.TextBlock>().Single(block => block.Text == "Details..." && block.DataContext == text);
            window.TryFindResource("Frb.Brushes.Link", variant, out object? link).ShouldBeTrue();

            ((global::Avalonia.Media.ISolidColorBrush)details.Foreground!).Color
                .ShouldBe(((global::Avalonia.Media.ISolidColorBrush)link!).Color);
        }
        finally
        {
            window.Close();
        }
    }

    private static ImportFromGumxViewModel CreateImportViewModel() => new ImportFromGumxLogic(
        Services.GetRequiredService<IProjectState>(),
        Services.GetRequiredService<IImportLogic>(),
        Services.GetRequiredService<IFileCommands>(),
        Services.GetRequiredService<IDialogService>(),
        Services.GetRequiredService<IDispatcher>(),
        Services.GetRequiredService<IOutputManager>()).CreateImportViewModel();

    private static void ShouldBeInsideWindow(Control control, Window window)
    {
        Point origin = control.TranslatePoint(new Point(0, 0), window)!.Value;
        control.Bounds.Height.ShouldBeGreaterThan(0);
        origin.Y.ShouldBeGreaterThanOrEqualTo(0);
        (origin.Y + control.Bounds.Height).ShouldBeLessThanOrEqualTo(window.Bounds.Height);
    }

    [AvaloniaFact]
    public void StandardDiffDetailsView_ListsEachDifference()
    {
        StandardDiffDetailsViewModel viewModel = new StandardDiffDetailsViewModel("Text", new[]
        {
            new StandardDiffRowViewModel("Variable", "FontSize differs"),
            new StandardDiffRowViewModel("Category", "ColorCategory is missing"),
        });

        Control view = Registry.CreateView(viewModel);
        Window window = new Window { Content = view };
        window.Show();
        window.UpdateLayout();

        view.ShouldBeOfType<StandardDiffDetailsView>();
        List<string?> texts = view.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToList();
        texts.ShouldContain("FontSize differs");
        texts.ShouldContain("Category");
        viewModel.Title.ShouldStartWith("Text");
        window.Close();
    }
}
