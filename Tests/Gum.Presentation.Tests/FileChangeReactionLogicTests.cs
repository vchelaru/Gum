using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

public class FileChangeReactionLogicTests
{
    private static FileChangeReactionLogic BuildSut(
        out Mock<IGuiCommands> guiCommandsMock,
        out Mock<IFileCommands> fileCommandsMock,
        out Mock<IPluginManager> pluginManagerMock)
    {
        guiCommandsMock = new Mock<IGuiCommands>();
        fileCommandsMock = new Mock<IFileCommands>();
        pluginManagerMock = new Mock<IPluginManager>();

        return new FileChangeReactionLogic(
            new Mock<ISelectedState>().Object,
            new Mock<IWireframeCommands>().Object,
            guiCommandsMock.Object,
            fileCommandsMock.Object,
            new Mock<IOutputManager>().Object,
            new Mock<IWireframeObjectManager>().Object,
            new Mock<IProjectState>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            pluginManagerMock.Object);
    }

    [Fact]
    public void ReactToFileDeleted_ShouldFlagElementAndRefresh_WhenElementFileWasDeleted()
    {
        // Covers the delete WIRING end-to-end at the reaction layer (issue #3367): a deleted
        // element file must flag the loaded element IsSourceFileMissing and refresh the tree + errors
        // so the red "!" appears. The leaf FlagElementForDeletedFile is tested separately; this pins
        // that ReactToFileDeleted actually calls it and fires the UI refresh.
        string tempDir = Path.Combine(Path.GetTempPath(), "GumDeleteWiringTest_" + Guid.NewGuid().ToString("N"));
        FilePath projectDirectory = new FilePath(tempDir);
        FilePath deletedFile = new FilePath(Path.Combine(tempDir, "Components", "MyButton.gucx")); // never created -> "deleted"

        FileChangeReactionLogic sut = BuildSut(
            out Mock<IGuiCommands> guiCommandsMock,
            out Mock<IFileCommands> fileCommandsMock,
            out Mock<IPluginManager> pluginManagerMock);
        fileCommandsMock.Setup(f => f.ProjectDirectory).Returns(projectDirectory);

        GumProjectSave project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "MyButton" };
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            sut.ReactToFileDeleted(deletedFile);

            component.IsSourceFileMissing.ShouldBeTrue();
            guiCommandsMock.Verify(g => g.RefreshElementTreeView(), Times.Once);
            pluginManagerMock.Verify(p => p.ElementReloaded(component), Times.Once);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

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
    public void IsReappearanceOfMissingSourceElement_ShouldReturnTrue_WhenFileMapsToFlaggedElement()
    {
        // #3367 clear-on-restore: a previously-deleted element file is restored on disk (a Created
        // event). The watcher uses this to process that lone Created so the element reloads and the
        // red "!" / GUM0004 clears.
        GumProjectSave project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "MyButton", IsSourceFileMissing = true };
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            FilePath projectDirectory = new FilePath(@"C:\proj\");
            FilePath restoredFile = new FilePath(@"C:\proj\Components\MyButton.gucx");

            bool result = FileChangeReactionLogic.IsReappearanceOfMissingSourceElement(restoredFile, projectDirectory);

            result.ShouldBeTrue();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void IsReappearanceOfMissingSourceElement_ShouldReturnFalse_WhenElementIsNotFlagged()
    {
        // A normal Created event (e.g. a save) for an element that isn't flagged missing must stay
        // on the suppressed path - otherwise normal saves would double-reload.
        GumProjectSave project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "MyButton", IsSourceFileMissing = false };
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            FilePath projectDirectory = new FilePath(@"C:\proj\");
            FilePath createdFile = new FilePath(@"C:\proj\Components\MyButton.gucx");

            bool result = FileChangeReactionLogic.IsReappearanceOfMissingSourceElement(createdFile, projectDirectory);

            result.ShouldBeFalse();
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
