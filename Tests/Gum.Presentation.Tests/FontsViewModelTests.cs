using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.FontsPlugin;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Fonts tab's view model: the ranges table (the project-wide default row first, then one row per
/// font) and the font generation settings, filled from and written back to the project.
/// </summary>
public class FontsViewModelTests
{
    [Fact]
    public void SetFrom_ShowsTheProjectWideRangeFirst_ThenOneRowPerFont_InFileOrder()
    {
        GumProjectSave project = new GumProjectSave();
        project.FontRanges = "32-126";
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Noto Sans SC", Ranges = "19968-40959" });
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Material Icons", Ranges = "57344-63743" });
        FontsViewModel viewModel = new FontsViewModel();

        viewModel.SetFrom(project);

        viewModel.Entries.Select(entry => (entry.FontFamily, entry.Ranges, entry.IsDefault)).ShouldBe(new[]
        {
            ("", "32-126", true),
            ("Noto Sans SC", "19968-40959", false),
            ("Material Icons", "57344-63743", false),
        });
    }

    [Fact]
    public void SetFrom_FillsTheGenerationSettings()
    {
        GumProjectSave project = new GumProjectSave
        {
            FontSpacingHorizontal = 2,
            FontSpacingVertical = 3,
            AutoSizeFontOutputs = true,
            UseFontCharacterFile = true,
        };
        FontsViewModel viewModel = new FontsViewModel();

        viewModel.SetFrom(project);

        (viewModel.FontSpacingHorizontal, viewModel.FontSpacingVertical, viewModel.AutoSizeFontOutputs, viewModel.UseFontCharacterFile)
            .ShouldBe((2, 3, true, true));
    }

    [Fact]
    public void ApplyToModelObjects_WritesTheRowsBack_DefaultFirst()
    {
        GumProjectSave project = new GumProjectSave();
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(project);
        viewModel.Entries[0].Ranges = "32-126";
        viewModel.AddEntry();
        viewModel.Entries[1].FontFamily = "Material Icons";
        viewModel.Entries[1].Ranges = "57344-63743";
        viewModel.FontSpacingHorizontal = 4;

        viewModel.ApplyToModelObjects();

        project.FontRangeEntries.Select(entry => (entry.FontFamily, entry.Ranges)).ShouldBe(new (string?, string)[]
        {
            (null, "32-126"),
            ("Material Icons", "57344-63743"),
        });
        project.FontSpacingHorizontal.ShouldBe(4);
    }

    [Fact]
    public void ApplyToModelObjects_SkipsARowWithNoFont_AndAFontAnEarlierRowAlreadyNames()
    {
        GumProjectSave project = new GumProjectSave();
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(project);
        viewModel.AddEntry();
        viewModel.AddEntry();
        viewModel.AddEntry();
        viewModel.Entries[1].FontFamily = "Arial";
        viewModel.Entries[1].Ranges = "65-90";
        viewModel.Entries[2].FontFamily = "arial";
        viewModel.Entries[2].Ranges = "97-122";
        viewModel.Entries[3].Ranges = "48-57";

        viewModel.ApplyToModelObjects();

        project.FontRangeEntries.Select(entry => entry.FontFamily).ShouldBe(new string?[] { null, "Arial" });
        viewModel.Entries[2].ErrorText.ShouldNotBeEmpty();
        viewModel.Entries[3].ErrorText.ShouldNotBeEmpty();
        viewModel.Entries[1].ErrorText.ShouldBeEmpty();
    }

    [Fact]
    public void AddEntry_AppendsAnEmptyFontRow_AndRaisesEntriesChanged()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        List<string?> changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.AddEntry();

        FontRangeEntryViewModel added = viewModel.Entries.Last();
        (added.IsDefault, added.FontFamily, added.Ranges).ShouldBe((false, "", BmfcSave.DefaultRanges));
        changed.ShouldContain(nameof(FontsViewModel.Entries));
    }

    [Fact]
    public void RemoveEntry_RemovesAFontRow_ButNeverTheDefaultRow()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        viewModel.AddEntry();
        FontRangeEntryViewModel fontRow = viewModel.Entries[1];

        viewModel.RemoveEntry(viewModel.Entries[0]);
        viewModel.Entries.Count.ShouldBe(2);

        viewModel.RemoveEntry(fontRow);
        viewModel.Entries.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("FontFamily")]
    [InlineData("Ranges")]
    public void EditingARow_RaisesEntriesChanged(string property)
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        viewModel.AddEntry();
        List<string?> changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        if (property == "FontFamily")
        {
            viewModel.Entries[1].FontFamily = "Arial";
        }
        else
        {
            viewModel.Entries[1].Ranges = "65-90";
        }

        changed.ShouldContain(nameof(FontsViewModel.Entries));
    }

    [Fact]
    public void EditingARow_WhileTheViewModelIsFilledFromTheProject_RaisesNothing()
    {
        FontsViewModel viewModel = new FontsViewModel();
        GumProjectSave project = new GumProjectSave();
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Arial", Ranges = "65-90" });
        List<string?> changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SetFrom(project);

        changed.ShouldNotContain(nameof(FontsViewModel.Entries));
    }

    [Theory]
    [InlineData("50-40", true)]
    [InlineData("32-126, 160-255", true)]
    [InlineData("32-126,160-255", false)]
    public void ARowWithARangeTheGeneratorsCannotRead_HasAnError(string ranges, bool expectedError)
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());

        viewModel.Entries[0].Ranges = ranges;

        viewModel.Entries[0].HasError.ShouldBe(expectedError);
        (viewModel.Entries[0].ErrorText != "").ShouldBe(expectedError);
    }

    [Fact]
    public void IsFontRangesReadOnly_FollowsUseFontCharacterFile_AndLocksOnlyTheDefaultRow()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        viewModel.AddEntry();
        List<string?> changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.UseFontCharacterFile = true;

        changed.ShouldContain(nameof(FontsViewModel.IsFontRangesReadOnly));
        viewModel.Entries[0].IsRangesReadOnly.ShouldBeTrue();
        viewModel.Entries[1].IsRangesReadOnly.ShouldBeFalse();
    }

    [Fact]
    public void UpdateFontUsage_OffersTheUsedFonts_AndWarnsAboutAFamilyNoTextUses()
    {
        GumProjectSave project = new GumProjectSave();
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Arial", Ranges = "65-90" });
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Roboto", Ranges = "65-90" });
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(project);

        viewModel.UpdateFontUsage(new[] { "Arial", "Material Icons" }, unmatchedFamilies: new[] { "Roboto" });

        viewModel.AvailableFonts.ShouldBe(new[] { "Arial", "Material Icons" });
        viewModel.Entries[1].HasWarning.ShouldBeFalse();
        viewModel.Entries[2].HasWarning.ShouldBeTrue();
        viewModel.Entries[2].WarningText.ShouldContain("Roboto");
    }

    [Fact]
    public void Reloaded_IsRaisedByNotifyReloaded()
    {
        FontsViewModel viewModel = new FontsViewModel();
        int raised = 0;
        viewModel.Reloaded += () => raised++;

        viewModel.NotifyReloaded();

        raised.ShouldBe(1);
    }
}
