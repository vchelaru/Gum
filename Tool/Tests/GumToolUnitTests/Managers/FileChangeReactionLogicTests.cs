using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Managers;

public class FileChangeReactionLogicTests
{
    [Fact]
    public void FlagElementForDeletedFile_ShouldSetIsSourceFileMissing_WhenDeletedFileMapsToLoadedElement()
    {
        // Repro #3367: an element's source file is deleted on disk while the tool is running.
        // The element must be flagged (red "!" / GUM0004) but NOT reloaded/removed — the in-memory
        // copy stays authoritative.
        GumProjectSave project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "MyButton" };
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            FilePath projectDirectory = new FilePath(@"C:\proj\");
            FilePath deletedFile = new FilePath(@"C:\proj\Components\MyButton.gucx");

            ElementSave? flagged = FileChangeReactionLogic.FlagElementForDeletedFile(deletedFile, projectDirectory);

            flagged.ShouldBe(component);
            component.IsSourceFileMissing.ShouldBeTrue();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void FlagElementForDeletedFile_ShouldReturnNull_WhenDeletedFileMapsToNoLoadedElement()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "MyButton" };
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            FilePath projectDirectory = new FilePath(@"C:\proj\");
            FilePath deletedFile = new FilePath(@"C:\proj\Components\Unrelated.gucx");

            ElementSave? flagged = FileChangeReactionLogic.FlagElementForDeletedFile(deletedFile, projectDirectory);

            flagged.ShouldBeNull();
            component.IsSourceFileMissing.ShouldBeFalse();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void IsLocalizationFile_ReturnsFalse_WhenChangedFileIsUnrelated()
    {
        List<FilePath> baseFiles = new List<FilePath>
        {
            new FilePath(@"C:\project\Strings.resx"),
            new FilePath(@"C:\project\Buttons.resx"),
        };
        FilePath unrelated = new FilePath(@"C:\project\SomeScreen.gusx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(unrelated, baseFiles).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFile_ReturnsFalse_WhenLocalizationFilesListIsEmpty()
    {
        List<FilePath> baseFiles = new List<FilePath>();
        FilePath changed = new FilePath(@"C:\project\Strings.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(changed, baseFiles).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFile_ReturnsTrue_WhenChangedFileIsSatelliteOfAnyBaseResx()
    {
        List<FilePath> baseFiles = new List<FilePath>
        {
            new FilePath(@"C:\project\Strings.resx"),
            new FilePath(@"C:\project\Buttons.resx"),
            new FilePath(@"C:\project\Errors.resx"),
        };
        FilePath satelliteOfButtons = new FilePath(@"C:\project\Buttons.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satelliteOfButtons, baseFiles).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFile_ReturnsTrue_WhenChangedFileMatchesOneOfMultipleBasePaths()
    {
        List<FilePath> baseFiles = new List<FilePath>
        {
            new FilePath(@"C:\project\Strings.resx"),
            new FilePath(@"C:\project\Buttons.resx"),
        };
        FilePath changed = new FilePath(@"C:\project\Buttons.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(changed, baseFiles).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFile_ReturnsTrue_WhenSingleEntryList_MatchesPreviousBehavior()
    {
        List<FilePath> baseFiles = new List<FilePath>
        {
            new FilePath(@"C:\project\Strings.resx"),
        };
        FilePath satellite = new FilePath(@"C:\project\Strings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFiles).ShouldBeTrue();

        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(baseFile, baseFiles).ShouldBeTrue();

        FilePath unrelated = new FilePath(@"C:\project\Other.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(unrelated, baseFiles).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenCsvHasNoSatellites()
    {
        FilePath baseFile = new FilePath(@"C:\project\Localization.csv");
        FilePath sibling = new FilePath(@"C:\project\Localization.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(sibling, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenResxNameDoesNotMatchBase()
    {
        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FilePath unrelated = new FilePath(@"C:\project\OtherStrings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(unrelated, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenSatelliteIsInDifferentDirectory()
    {
        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FilePath satellite = new FilePath(@"C:\other\Strings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnFalse_WhenUnrelatedFileChanges()
    {
        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FilePath unrelated = new FilePath(@"C:\project\SomeScreen.gusx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(unrelated, baseFile).ShouldBeFalse();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsAnySatellite()
    {
        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FilePath satellite = new FilePath(@"C:\project\Strings.zh-Hans.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsBaseLocalizationCsv()
    {
        FilePath baseFile = new FilePath(@"C:\project\Localization.csv");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(baseFile, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsBaseLocalizationFile()
    {
        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(baseFile, baseFile).ShouldBeTrue();
    }

    [Fact]
    public void IsLocalizationFileThatShouldTriggerReload_ShouldReturnTrue_WhenFileIsSatelliteResx()
    {
        FilePath baseFile = new FilePath(@"C:\project\Strings.resx");
        FilePath satellite = new FilePath(@"C:\project\Strings.es.resx");
        FileChangeReactionLogic.IsLocalizationFileThatShouldTriggerReload(satellite, baseFile).ShouldBeTrue();
    }
}
