using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Dialogs;
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

        // Phase 80 work in progress; each entry leaves this list as its view lands.
        ["ExposeColorDialogViewModel"] = "phase 80, not ported yet",
        ["DisplayReferencesDialog"] = "phase 80, not ported yet",
        ["ThemingDialogViewModel"] = "phase 80, not ported yet",
        ["LoadRecentViewModel"] = "phase 80, not ported yet",
        ["ImportBehaviorDialog"] = "phase 80, not ported yet",
        ["ImportComponentDialog"] = "phase 80, not ported yet",
        ["ImportScreenDialog"] = "phase 80, not ported yet",
        ["AddAnimationDialogViewModel"] = "phase 80, not ported yet",
        ["AddStateKeyframeDialog"] = "phase 80, not ported yet",
        ["SubAnimationSelectionDialogViewModel"] = "phase 80, not ported yet",
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
    public void RegisteredViews_ConstructWithoutAViewModel()
    {
        foreach (Type viewModelType in Registry.RegisteredViewModelTypes)
        {
            Control? view = Registry.CreateViewWithoutContext(viewModelType);

            view.ShouldNotBeNull(viewModelType.Name);
        }
    }
}
