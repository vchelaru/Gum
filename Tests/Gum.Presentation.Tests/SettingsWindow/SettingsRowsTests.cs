using System.Linq;
using Gum.SettingsWindow;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.SettingsWindow;

/// <summary>The row kinds and shared row shape every Settings page is composed from.</summary>
public class SettingsRowsTests
{
    [Fact]
    public void Toggle_ReadsAndWritesThroughItsDelegates()
    {
        bool stored = false;
        InstanceMember row = SettingsRows.Toggle("Key", "Label", "Hint", () => stored, value => stored = value, toggleText: "Do it");

        row.Value = true;

        stored.ShouldBeTrue();
        row.Value.ShouldBe(true);
        row.PropertyType.ShouldBe(typeof(bool));
        row.ToggleText.ShouldBe("Do it");
    }

    [Fact]
    public void Text_WritesAnEmptyStringForNull()
    {
        string? stored = "x";
        InstanceMember row = SettingsRows.Text("Key", "Label", "Hint", () => stored, value => stored = value);

        row.Value = null;

        stored.ShouldBe(string.Empty);
        row.PropertyType.ShouldBe(typeof(string));
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData("12", 12)]
    public void Number_AcceptsANumberOrItsText(object entered, int expected)
    {
        int stored = 0;
        InstanceMember row = SettingsRows.Number("Key", "Label", "Hint", () => stored, value => stored = value);

        row.Value = entered;

        stored.ShouldBe(expected);
    }

    [Fact]
    public void Number_IgnoresAnEntryThatIsNotANumber()
    {
        int stored = 5;
        InstanceMember row = SettingsRows.Number("Key", "Label", "Hint", () => stored, value => stored = value);

        row.Value = "abc";

        stored.ShouldBe(5);
    }

    [Fact]
    public void Choice_OffersTheOptions()
    {
        InstanceMember row = SettingsRows.Choice("Key", "Label", "Hint", typeof(string), new object[] { "A", "B" }, () => "A", _ => { });

        row.CustomOptions.ShouldBe(new object[] { "A", "B" });
        row.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void Path_UsesTheFileEditor_AndAFolderPickerOnlyForAFolder()
    {
        InstanceMember file = SettingsRows.Path("Key", "Label", "Hint", () => "", _ => { });
        InstanceMember folder = SettingsRows.Path("Key", "Label", "Hint", () => "", _ => { }, isFolder: true);

        file.PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.FileSelection));
        file.PropertiesToSetOnDisplayer.ContainsKey("IsFolderDialog").ShouldBeFalse();
        folder.PropertiesToSetOnDisplayer["IsFolderDialog"].ShouldBe(true);
    }

    [Fact]
    public void EveryRow_KeepsItsKeyAsNameAndShowsItsLabelAndHint()
    {
        InstanceMember row = SettingsRows.Text("Key", "The label", "The hint", () => "", _ => { });

        row.Name.ShouldBe("Key");
        row.DisplayName.ShouldBe("The label");
        row.DetailText.ShouldBe("The hint");
    }

    [Fact]
    public void Finish_GivesEveryRowTheLabelColumnAndTheSpaciousShape()
    {
        MemberCategory section = SettingsRows.Section("S", new[]
        {
            SettingsRows.Text("A", "A", "a", () => "", _ => { }),
            SettingsRows.Toggle("B", "B", "b", () => false, _ => { }),
        });

        SettingsRows.Finish(new[] { section });

        section.Members.ShouldAllBe(member => member.FirstGridLength == SettingsRows.LabelColumnWidth && member.IsSpaciousRow);
    }

    [Fact]
    public void Section_IsExpandedUnlessTold()
    {
        SettingsRows.Section("S", new InstanceMember[0]).IsExpanded.ShouldBeTrue();
        SettingsRows.Section("S", new InstanceMember[0], isExpanded: false).IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void SearchTerms_AreTheLabelAndHintOfEveryRow()
    {
        MemberCategory section = SettingsRows.Section("S", new[]
        {
            SettingsRows.Text("A", "Alpha", "first hint", () => "", _ => { }),
            SettingsRows.Text("B", "Beta", "second hint", () => "", _ => { }),
        });

        SettingsRows.SearchTerms(new[] { section }).ToArray().ShouldBe(new[] { "Alpha", "first hint", "Beta", "second hint" });
    }
}
