using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Settings;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Managers;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests;

/// <summary>The Application > Keyboard shortcuts page: a dense list, one short line per command.</summary>
public class KeyboardShortcutsPageTests
{
    private static (DialogWindow Window, SettingsDialogViewModel ViewModel, SettingsRowsPageView Page) Open()
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        viewModel.SelectPage(SettingsPageIds.KeyboardShortcuts);
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        window.UpdateLayout();
        return (window, viewModel, window.GetVisualDescendants().OfType<SettingsRowsPageView>().Single());
    }

    [AvaloniaFact]
    public void EveryShortcutRow_IsOneShortLine_NoTallerThanItsTextPlusTheChipPadding()
    {
        (DialogWindow window, _, SettingsRowsPageView page) = Open();

        KeyChipDisplay[] rows = page.GetVisualDescendants().OfType<KeyChipDisplay>().ToArray();
        rows.Length.ShouldBeGreaterThan(20);
        foreach (KeyChipDisplay row in rows)
        {
            // Measured against the row's own text, so a wider or larger font cannot make this wrong.
            TextBlock label = row.GetVisualDescendants().OfType<TextBlock>().First();
            row.Bounds.Height.ShouldBeLessThanOrEqualTo(label.Bounds.Height + 16, row.InstanceMember!.DisplayName);
        }
        window.Close();
    }

    [AvaloniaFact]
    public void SearchingAShortcutsKeys_HighlightsItsRow()
    {
        (DialogWindow window, SettingsDialogViewModel viewModel, SettingsRowsPageView page) = Open();
        IHotkeyManager hotkeys = TestAppBuilder.Services.GetRequiredService<IHotkeyManager>();
        string keys = TestAppBuilder.Services.GetRequiredService<IKeyCombinationFormatter>().Format(hotkeys.Undo!);

        viewModel.SearchText = keys;
        window.UpdateLayout();

        page.Grid.Categories.SelectMany(category => category.Members).Where(row => row.IsSearchMatch)
            .Select(row => row.DisplayName).ShouldContain("Undo");
        viewModel.SearchText = "";
        window.Close();
    }

    [AvaloniaFact]
    public void SearchingACommandName_HighlightsItsRow_AndHidesNothing()
    {
        (DialogWindow window, SettingsDialogViewModel viewModel, SettingsRowsPageView page) = Open();
        int total = page.Grid.Categories.Sum(category => category.Members.Count);

        viewModel.SearchText = "duplicate";
        window.UpdateLayout();

        page.Grid.Categories.SelectMany(category => category.Members).Where(row => row.IsSearchMatch)
            .Select(row => row.DisplayName).ShouldBe(new[] { "Duplicate" });
        page.Grid.Categories.Sum(category => category.Members.Count).ShouldBe(total);
        viewModel.SearchText = "";
        window.Close();
    }
}
