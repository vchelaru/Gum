using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
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
        ImportFromGumxViewModel viewModel = new ImportFromGumxLogic(
            Services.GetRequiredService<IProjectState>(),
            Services.GetRequiredService<IImportLogic>(),
            Services.GetRequiredService<IFileCommands>(),
            Services.GetRequiredService<IDialogService>(),
            Services.GetRequiredService<IDispatcher>()).CreateImportViewModel();
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
