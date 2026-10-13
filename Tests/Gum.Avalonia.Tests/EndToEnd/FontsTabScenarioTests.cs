using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.FontsPlugin;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The Font Settings tab (Edit > Font Settings) driven with simulated input over a real project: the ranges table, the
/// font character file, and the generation settings each save into the project file.
/// </summary>
public class FontsTabScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    [AvaloniaFact]
    [Trait("Feature", "EDIT-013")]
    public void EditFontSettings_OpensTheTab_ShowingTheProjectsRanges()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using FontsTab fonts = new FontsTab(tree);

        fonts.Tab.IsVisible.ShouldBeTrue();
        fonts.ViewModel.Entries.Select(entry => (entry.FontFamily, entry.Ranges)).ShouldBe(new[] { ("", BmfcSave.DefaultRanges) });

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-013")]
    public void DefaultRange_Typed_IsSaved()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using FontsTab fonts = new FontsTab(tree);

        fonts.TypeAndEnter(fonts.RangesBox(fonts.ViewModel.Entries[0]), "32-126");
        tree.WaitUntil(() => SavedProject(tree).FontRanges == "32-126", AsyncWork, "the default ranges to save");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-013")]
    public void FontRange_Added_TypedAndRemoved_IsSavedThenDropped()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using FontsTab fonts = new FontsTab(tree);

        fonts.Input.Click(fonts.View.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Add Font Range")));
        fonts.Settle();
        FontRangeEntryViewModel row = fonts.ViewModel.Entries.Last();
        fonts.TypeAndEnter(fonts.FamilyBox(row), "Material Icons");
        fonts.TypeAndEnter(fonts.RangesBox(row), "57344-63743");
        tree.WaitUntil(() => SavedProject(tree).GetFontRanges("Material Icons") == "57344-63743", AsyncWork, "the font's ranges to save");

        fonts.Input.Click(fonts.RemoveButton(row));
        fonts.Settle();
        tree.WaitUntil(() => SavedProject(tree).FindFontSpecificRangeEntry("Material Icons") == null, AsyncWork, "the font's row to be dropped");
        fonts.ViewModel.Entries.Count.ShouldBe(1);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-013")]
    public void FontRange_ForAFontNoTextUses_ShowsAWarning_AndAnInvalidRangeAnError()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using FontsTab fonts = new FontsTab(tree);
        fonts.ViewModel.AddEntry();
        FontRangeEntryViewModel row = fonts.ViewModel.Entries.Last();
        fonts.Settle();

        fonts.TypeAndEnter(fonts.FamilyBox(row), "Ghost Sans");
        tree.WaitUntil(() => row.HasWarning, AsyncWork, "the unused font to be flagged");

        fonts.TypeAndEnter(fonts.RangesBox(row), "50-40");
        row.HasError.ShouldBeTrue();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-014")]
    public void FontCharacterFile_ReplacesTheDefaultRange_AndLocksOnlyThatRow()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using FontsTab fonts = new FontsTab(tree);
        fonts.ViewModel.AddEntry();
        FontRangeEntryViewModel fontRow = fonts.ViewModel.Entries.Last();
        FontRangeEntryViewModel defaultRow = fonts.ViewModel.Entries[0];

        // The project's character file, which saving the project writes with the default characters.
        string characterFile = Path.Combine(tree.Project.ProjectFolder, ".gumfcs");
        string defaultCharacters = File.ReadAllText(characterFile);
        File.WriteAllText(characterFile, "AB");
        try
        {
            fonts.Input.Click(fonts.CharacterFileCheckBox);
            fonts.Settle();
            tree.WaitUntil(() => SavedProject(tree).UseFontCharacterFile, AsyncWork, "the character file setting to save");

            defaultRow.Ranges.ShouldBe(BmfcSave.GenerateRangesFromFile(characterFile));
            fonts.RangesBox(defaultRow).IsReadOnly.ShouldBeTrue("the default ranges come from the file while it is used");
            fonts.RangesBox(fontRow).IsReadOnly.ShouldBeFalse("a font's own ranges still apply");

            fonts.Input.Click(fonts.CharacterFileCheckBox);
            fonts.Settle();
            tree.WaitUntil(() => !SavedProject(tree).UseFontCharacterFile, AsyncWork, "the character file setting to clear");
            defaultRow.Ranges.ShouldBe(BmfcSave.DefaultRanges);
            fonts.RangesBox(defaultRow).IsReadOnly.ShouldBeFalse();
        }
        finally
        {
            File.WriteAllText(characterFile, defaultCharacters);
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "PROP-015")]
    public void GenerationSettings_AreSavedIntoTheProject()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        using FontsTab fonts = new FontsTab(tree);

        fonts.TypeAndEnterInGrid("FontSpacingHorizontal", "2");
        fonts.TypeAndEnterInGrid("FontSpacingVertical", "3");
        fonts.ClickGridCheckBox("AutoSizeFontOutputs");
        if (OperatingSystem.IsWindows())
        {
            FontGeneratorType generator = fonts.ViewModel.FontGenerator == FontGeneratorType.KernSmith ? FontGeneratorType.BmFont : FontGeneratorType.KernSmith;
            fonts.PickGridCombo("FontGenerator", generator.ToString());
            tree.WaitUntil(() => SavedProject(tree).FontGenerator == generator, AsyncWork, "the font generator to save");
        }
        else
        {
            // bmfont.exe runs only on Windows, so elsewhere the row is read-only and KernSmith is used.
            fonts.GridRow("FontGenerator").GetVisualDescendants().OfType<ComboBox>().Single().IsEffectivelyEnabled.ShouldBeFalse();
        }
        tree.WaitUntil(() => SavedProject(tree).AutoSizeFontOutputs, AsyncWork, "the font outputs setting to save");

        GumProjectSave saved = SavedProject(tree);
        (saved.FontSpacingHorizontal, saved.FontSpacingVertical).ShouldBe((2, 3));

        tree.AssertOracles();
    }

    private static GumProjectSave SavedProject(ProjectTreeHarness tree) =>
        GumProjectSave.Load(tree.Project.ProjectFilePath, out _) ?? throw new InvalidOperationException("The project file did not load.");

    /// <summary>The head's Font Settings tab, opened with Edit > Font Settings and hosted in a window of its own.</summary>
    private sealed class FontsTab : IDisposable
    {
        private readonly ProjectTreeHarness _tree;

        public FontsTab(ProjectTreeHarness tree)
        {
            _tree = tree;
            tree.PickMainMenu("Edit", "Font Settings");
            Tab = ((AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>()).AllTabs.Single(candidate => candidate.Title == "Font Settings");
            Tab.IsVisible.ShouldBeTrue();
            View = (FontsView)Tab.Content;
            ViewModel = (FontsViewModel)View.DataContext!;
            Input = new HeadlessWindowDriver(View, width: 520, height: 900, framesFolderName: "GumFonts");
        }

        public AvaloniaPluginTab Tab { get; }

        public FontsView View { get; }

        public FontsViewModel ViewModel { get; }

        public HeadlessWindowDriver Input { get; }

        /// <summary>The ranges box of <paramref name="row"/>.</summary>
        public TextBox RangesBox(FontRangeEntryViewModel row)
        {
            Settle();
            return View.GetVisualDescendants().OfType<TextBox>()
                .Single(box => box.DataContext == row && !box.GetVisualAncestors().OfType<ComboBox>().Any());
        }

        /// <summary>The text box inside the editable font combo of <paramref name="row"/>.</summary>
        public TextBox FamilyBox(FontRangeEntryViewModel row)
        {
            Settle();
            return View.GetVisualDescendants().OfType<ComboBox>().Single(combo => combo.DataContext == row)
                .GetVisualDescendants().OfType<TextBox>().Single();
        }

        public CheckBox CharacterFileCheckBox =>
            View.GetVisualDescendants().OfType<CheckBox>().Single(box => Equals(box.Content, "Use Font Character File (.gumfcs)"));

        public Button RemoveButton(FontRangeEntryViewModel row)
        {
            Settle();
            return View.GetVisualDescendants().OfType<Button>().Single(button => button.DataContext == row && button.CommandParameter == row);
        }

        public void TypeAndEnter(TextBox box, string text)
        {
            Input.TypeAndEnter(box, text);
            Settle();
        }

        public SingleDataUiContainer GridRow(string name)
        {
            Settle();
            InstanceMember member = View.SettingsGrid.Categories.SelectMany(category => category.Members).FirstOrDefault(candidate => candidate.Name == name)
                ?? throw new InvalidOperationException($"The Font Settings grid has no {name} row.");
            SingleDataUiContainer row = View.SettingsGrid.LiveContainers.FirstOrDefault(candidate => candidate.Member == member)
                ?? throw new InvalidOperationException($"The {name} row has no live editor.");
            row.BringIntoView();
            Settle();
            return row;
        }

        public void TypeAndEnterInGrid(string name, string text) =>
            TypeAndEnter(((TextBoxDisplay)GridRow(name).Displayer!).TextBox, text);

        public void ClickGridCheckBox(string name)
        {
            Input.Click(((CheckBoxDisplay)GridRow(name).Displayer!).CheckBox);
            Settle();
        }

        /// <summary>Picks <paramref name="item"/> in a combo row, as a click in its open drop-down does.</summary>
        public void PickGridCombo(string name, string item)
        {
            ComboBox combo = GridRow(name).GetVisualDescendants().OfType<ComboBox>().Single();
            combo.IsDropDownOpen = true;
            Settle();
            combo.SelectedItem = combo.Items.Cast<object?>().First(candidate => candidate?.ToString() == item);
            combo.IsDropDownOpen = false;
            Settle();
        }

        public void Settle()
        {
            Input.Layout();
            _tree.ThrowIfCrashed();
        }

        public void Dispose() => Input.Dispose();
    }
}
