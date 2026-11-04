using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Managers;
using Gum.Messages;
using Gum.Services;
using Gum.SettingsWindow;
using Moq;
using Shouldly;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.SettingsWindow;

/// <summary>
/// The rows of the Application > General page: each reads and writes the same setting the page always did.
/// </summary>
public class GeneralRowsTests
{
    private readonly Mock<IProjectManager> _projectManager = new Mock<IProjectManager>();
    private readonly Mock<IFileSystemRevealService> _reveal = new Mock<IFileSystemRevealService>();
    private readonly WeakReferenceMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<StandardsPaletteSettingChangedMessage> _messages = new List<StandardsPaletteSettingChangedMessage>();

    public GeneralRowsTests()
    {
        _messenger.Register<StandardsPaletteSettingChangedMessage>(this, (_, message) => _messages.Add(message));
    }

    private GeneralRows CreateRows() =>
        new GeneralRows(new GeneralSettingsViewModel(_projectManager.Object, _messenger, _reveal.Object));

    private static InstanceMember Row(GeneralRows rows, string name) =>
        rows.BuildCategories().SelectMany(section => section.Members).Single(member => member.Name == name);

    [Fact]
    public void EveryRow_HasALabel_AHint_AndTheSharedRowShape()
    {
        InstanceMember[] members = CreateRows().BuildCategories().SelectMany(section => section.Members).ToArray();

        members.Select(member => member.DisplayName).ShouldBe(new[] { "Auto save", "Standards palette", "Settings folder" });
        foreach (InstanceMember member in members)
        {
            member.DetailText.ShouldNotBeNullOrWhiteSpace();
            member.IsSpaciousRow.ShouldBeTrue();
            member.FirstGridLength.ShouldBe(SettingsRows.LabelColumnWidth);
        }
    }

    [Fact]
    public void SearchTerms_IncludeEveryLabelAndHint()
    {
        string[] terms = CreateRows().SearchTerms().ToArray();

        terms.ShouldContain("Auto save");
        terms.ShouldContain("Settings folder");
        terms.ShouldContain("Save the project whenever something changes.");
        terms.Length.ShouldBe(6);
    }

    [Fact]
    public void AutoSaveRow_ReadsTheSetting_AndWritesThroughAndSaves()
    {
        _projectManager.SetupGet(x => x.AutoSave).Returns(true);
        InstanceMember row = Row(CreateRows(), "AutoSave");

        row.Value.ShouldBe(true);
        row.Value = false;

        _projectManager.VerifySet(x => x.AutoSave = false, Times.Once);
        _projectManager.Verify(x => x.SaveGeneralSettings(), Times.Once);
    }

    [Fact]
    public void StandardsPaletteRow_WritesThroughSavesAndNotifies()
    {
        _projectManager.SetupGet(x => x.EffectiveUseStandardsPalette).Returns(false);
        InstanceMember row = Row(CreateRows(), "UseStandardsPalette");
        _projectManager.SetupGet(x => x.EffectiveUseStandardsPalette).Returns(true);

        row.Value.ShouldBe(false);
        row.Value = true;

        _projectManager.VerifySet(x => x.UseStandardsPalette = true, Times.Once);
        _projectManager.Verify(x => x.SaveGeneralSettings(), Times.Once);
        _messages.Count.ShouldBe(1);
        _messages[0].UseStandardsPalette.ShouldBeTrue();
    }

    [Fact]
    public void SettingsFolderRow_ShowsTheFolderReadOnly_AndItsLinkOpensIt()
    {
        InstanceMember row = Row(CreateRows(), "SettingsFolder");

        row.IsReadOnly.ShouldBeTrue();
        row.PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.ReadOnlyValueWithChip));
        row.Value.ShouldBe(FileManager.UserApplicationDataForThisApplication);
        row.DetailAction.ShouldNotBeNull().Text.ShouldBe("Open folder");

        row.DetailAction!.Navigate();

        _reveal.Verify(x => x.OpenFolder(FileManager.UserApplicationDataForThisApplication), Times.Once);
    }

    [Fact]
    public void ReadOnlyRow_IgnoresWrites_AndShowsItsChip()
    {
        string value = "detected";
        InstanceMember row = SettingsRows.ReadOnly("Detected", "Detected value", "Where it came from.", () => value, chip: "project");

        row.Value = "typed";

        row.Value.ShouldBe("detected");
        row.ChipText.ShouldBe("project");
    }
}
