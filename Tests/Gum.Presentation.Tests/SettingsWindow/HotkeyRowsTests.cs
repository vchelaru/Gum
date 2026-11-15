using System.Collections.Generic;
using System.Linq;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.SettingsWindow;
using Moq;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.SettingsWindow;

/// <summary>
/// The rows of the Application > Keyboard shortcuts page: one compact line per command (its name and a key
/// chip), grouped by area, and every command and shortcut the old plain list showed.
/// </summary>
public class HotkeyRowsTests
{
    private static HotkeyViewModel CreateViewModel()
    {
        Mock<IHotkeyManager> hotkeyManager = new();
        hotkeyManager.Setup(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));
        hotkeyManager.Setup(x => x.Undo).Returns(KeyCombination.Ctrl(GumKey.Z));
        hotkeyManager.Setup(x => x.Duplicate).Returns(KeyCombination.Ctrl(GumKey.D));
        hotkeyManager.Setup(x => x.NudgeUp).Returns(KeyCombination.Pressed(GumKey.Up));
        hotkeyManager.Setup(x => x.NudgeUp5).Returns(KeyCombination.Shift(GumKey.Up));
        hotkeyManager.Setup(x => x.ZoomCameraIn).Returns(KeyCombination.Ctrl(GumKey.Oemplus));
        return new HotkeyViewModel(hotkeyManager.Object, new KeyCombinationFormatter(KeyDisplayStyle.Windows));
    }

    private static InstanceMember[] Members(HotkeyRows rows) =>
        rows.BuildCategories().SelectMany(section => section.Members).ToArray();

    [Fact]
    public void EveryShortcut_IsOneRow_WithTheCommandAsLabel_AndTheKeysAsAChip()
    {
        HotkeyViewModel viewModel = CreateViewModel();

        InstanceMember[] members = Members(new HotkeyRows(viewModel));

        members.Length.ShouldBe(viewModel.Items.Count);
        InstanceMember undo = members.Single(member => member.DisplayName == "Undo");
        undo.ChipText.ShouldBe("Ctrl+Z");
        undo.PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.KeyChip));
        undo.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void Rows_AreOneLine_WithNoHint_AndTheSharedRowShape()
    {
        InstanceMember[] members = Members(new HotkeyRows(CreateViewModel()));

        foreach (InstanceMember member in members)
        {
            member.DetailText.ShouldBeNull();
            member.IsSpaciousRow.ShouldBeTrue();
            member.FirstGridLength.ShouldBe(SettingsRows.LabelColumnWidth);
        }
    }

    [Fact]
    public void Shortcuts_AreGroupedByArea_InTheOrderOfTheOldList()
    {
        MemberCategory[] sections = new HotkeyRows(CreateViewModel()).BuildCategories().ToArray();

        sections.Select(section => section.Name).ShouldBe(new[] { "Editing", "Nudge", "Camera" });
        sections.Single(section => section.Name == "Editing").Members.Select(member => member.DisplayName)
            .ShouldBe(new[] { "Copy", "Duplicate", "Undo" });
        sections.Single(section => section.Name == "Nudge").Members.Select(member => member.DisplayName)
            .ShouldBe(new[] { "Nudge Up", "Nudge Up 5" });
        sections.ShouldAllBe(section => section.IsExpanded);
    }

    [Fact]
    public void ASection_WithNoBoundShortcut_IsLeftOut()
    {
        HotkeyViewModel viewModel = CreateViewModel();

        new HotkeyRows(viewModel).BuildCategories().ShouldAllBe(section => section.Members.Count > 0);
    }

    [Fact]
    public void Search_MarksTheRowByCommandName_OrByShortcutText()
    {
        HotkeyRows rows = new HotkeyRows(CreateViewModel());
        List<MemberCategory> sections = rows.BuildCategories();

        SettingsRows.MarkMatches(sections, "undo").ShouldBe(1);
        sections.SelectMany(s => s.Members).Single(m => m.IsSearchMatch).DisplayName.ShouldBe("Undo");

        SettingsRows.MarkMatches(sections, "ctrl+d").ShouldBe(1);
        sections.SelectMany(s => s.Members).Single(m => m.IsSearchMatch).DisplayName.ShouldBe("Duplicate");
    }

    [Fact]
    public void SearchTerms_IncludeEveryCommandNameAndShortcut()
    {
        string[] terms = new HotkeyRows(CreateViewModel()).SearchTerms().ToArray();

        terms.ShouldContain("Copy");
        terms.ShouldContain("Ctrl+C");
    }

    [Fact]
    public void Items_KeepTheirDisplayText_AndExposeNameAndShortcut()
    {
        HotkeyItemViewModel copy = CreateViewModel().Items.Single(item => item.Name == "Copy");

        copy.Display.ShouldBe("Copy: Ctrl+C");
        copy.Shortcut.ShouldBe("Ctrl+C");
    }
}
