using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Dialogs;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Every dialog the headless core can open has an Avalonia view, and every registered view builds.
/// A dialog view model reachable from a menu or command lives in Gum.Presentation (plugins open
/// theirs through <see cref="IDialogService"/> too), so scanning that assembly covers them all.
/// </summary>
public class DialogViewRegistryTests
{
    /// <summary>
    /// Dialogs whose Avalonia view another phase of the migration owns, with the reason. Keep this
    /// list short and named: a dialog missing from both the registry and this list fails the build.
    /// </summary>
    private static readonly Dictionary<string, string> OwnedElsewhere = new()
    {
        ["AddVariableViewModel"] = "Variables tab (VariableGrid) dialog, phase 70",
        ["AddFormsViewModel"] = "GumFormsPlugin, a property-grid consumer owned by phase 70",
        ["ImportFromGumxViewModel"] = "ImportFromGumxPlugin, a property-grid consumer owned by phase 70",
        ["StandardDiffDetailsViewModel"] = "ImportFromGumxPlugin, a property-grid consumer owned by phase 70",
    };

    private static DialogViewRegistry Registry => TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>();

    private static IEnumerable<Type> PresentationDialogViewModels =>
        typeof(DialogViewModel).Assembly.GetTypes()
            .Where(type => typeof(DialogViewModel).IsAssignableFrom(type) && !type.IsAbstract);

    [Fact]
    public void EveryDialogViewModel_HasAnAvaloniaView_OrANamedOwner()
    {
        string[] missing = PresentationDialogViewModels
            .Where(type => !Registry.HasView(type) && !OwnedElsewhere.ContainsKey(type.Name))
            .Select(type => type.FullName!)
            .ToArray();

        missing.ShouldBeEmpty("These dialogs would open as a 'No Avalonia view is registered' placeholder: " + string.Join(", ", missing));
    }

    [Fact]
    public void OwnedElsewhere_ListsOnlyDialogsWithoutAView()
    {
        string[] stale = PresentationDialogViewModels
            .Where(type => Registry.HasView(type) && OwnedElsewhere.ContainsKey(type.Name))
            .Select(type => type.Name)
            .ToArray();

        stale.ShouldBeEmpty("These dialogs have a view now; remove them from OwnedElsewhere: " + string.Join(", ", stale));
    }

    [AvaloniaFact]
    public void ImportDialog_ListSelectionFillsSelectedFiles()
    {
        // The real import dialogs read the loaded project in their constructors; the view only needs the base.
        TestImportDialog viewModel = new TestImportDialog(TestAppBuilder.Services.GetRequiredService<IDialogService>());
        viewModel.UnfilteredFiles.Add("Components/Button.gucx");
        viewModel.UnfilteredFiles.Add("Components/Label.gucx");
        Control view = Registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        ListBox files = view.GetLogicalDescendants().OfType<ListBox>().Single();

        files.SelectAll();

        viewModel.SelectedFiles.ShouldBe(new[] { "Components/Button.gucx", "Components/Label.gucx" }, ignoreOrder: true);
        viewModel.AffirmativeCommand.CanExecute(null).ShouldBeTrue();
        window.Title.ShouldBe(viewModel.Title);
        window.Close();
    }

    [AvaloniaFact]
    public void NewProjectDialog_ShowsItsOptionsInATitledWindow()
    {
        NewProjectDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<NewProjectDialogViewModel>();

        Control view = Registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();

        view.ShouldBeOfType<NewProjectDialogView>();
        window.Title.ShouldBe("New Project");
        CheckBox[] checkBoxes = view.GetLogicalDescendants().OfType<CheckBox>().ToArray();
        checkBoxes.Select(box => box.Content).ShouldBe(new object[] { "Include Forms controls", "Include DemoScreenGum" });
        checkBoxes[0].IsChecked.ShouldBe(true);

        checkBoxes[0].IsChecked = false;

        viewModel.IsIncludeFormsControls.ShouldBeFalse();
        checkBoxes[1].IsEffectivelyEnabled.ShouldBeFalse();
        window.Close();
    }

    [AvaloniaFact]
    public void ThemingDialog_OpeningDoesNotMarkColorsExplicit()
    {
        ThemingDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<ThemingDialogViewModel>();
        bool[] before = ExplicitFlags(viewModel);
        Control view = Registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);

        window.Show();

        ExplicitFlags(viewModel).ShouldBe(before);
        view.GetLogicalDescendants().OfType<ColorPicker>().Count().ShouldBe(6);
        // Closing without Cancel: nothing changed, and Cancel re-applies the theme, which the shared
        // test container's canvas plugins cannot react to without a device.
        window.Close();
    }

    [AvaloniaFact]
    public void RegisteredViews_ConstructWithoutAViewModel()
    {
        foreach (Type viewModelType in Registry.RegisteredViewModelTypes)
        {
            Control? view = Registry.CreateViewWithoutContext(viewModelType);

            view.ShouldNotBeNull(viewModelType.Name);
        }
    }

    private sealed class TestImportDialog : ImportBaseDialogViewModel
    {
        public TestImportDialog(IDialogService dialogService) : base(dialogService) { }

        public override string Title => "Import Test Files";

        public override string BrowseFileFilter => "All Files (*.*)|*.*";
    }

    private static bool[] ExplicitFlags(ThemingDialogViewModel viewModel) => new[]
    {
        viewModel.HasExplicitAccentColor,
        viewModel.HasExplicitCheckerAColor,
        viewModel.HasExplicitCheckerBColor,
        viewModel.HasExplicitOutlineColor,
        viewModel.HasExplicitGuideLineColor,
        viewModel.HasExplicitGuideTextColor,
    };
}
