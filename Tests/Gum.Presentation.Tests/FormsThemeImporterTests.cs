using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using ImportFromGumxPlugin;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Add Forms imports a theme through the shared <see cref="IGumxImportService"/>. These tests pin
/// what is specific to Forms themes: what gets selected, the prerequisites, the conflict choice,
/// the Standards confirmation and the font license files. The import mechanics (format conversion,
/// assets, animations, file watching) are covered by GumxImportServiceTests.
/// </summary>
public class FormsThemeImporterTests
{
    private const string ThemeProjectPath = "C:/themes/Neon/GumProject.gumx";

    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IFileWatchIgnoreList> _fileWatch = new();
    private readonly Mock<ISkiaShapeStandardsLogic> _skiaShapeStandards = new();
    private readonly Mock<IFontManager> _fontManager = new();
    private readonly Mock<IImportFromGumxDialog> _importDialog = new();
    private readonly Mock<IGumxSourceService> _sourceService = new();
    private readonly Mock<IGumxDependencyResolver> _dependencyResolver = new();
    private readonly Mock<IGumxImportService> _importService = new();
    private readonly Mock<IModifiedStandardsFinder> _modifiedStandards = new();
    private readonly GumProjectSave _theme = new GumProjectSave();
    private readonly FormsThemeImporter _importer;

    private ImportSelections? _importedSelections;
    private string? _importedSourceBase;
    private string? _importedSubfolder;
    private bool? _importedReplaceStandardAnimations;
    private Action? _importHook;
    private ImportResult _importResult = new ImportResult();

    public FormsThemeImporterTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _formsFileService.Setup(x => x.GetFormsGumxPath(It.IsAny<string>())).Returns(ThemeProjectPath);
        _projectState.Setup(x => x.GumProjectSave)
            .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumx" });
        _sourceService.Setup(x => x.LoadProjectAsync(ThemeProjectPath, null)).ReturnsAsync(_theme);
        _sourceService.Setup(x => x.GetSourceBase(ThemeProjectPath)).Returns("C:/themes/Neon");
        _dependencyResolver.Setup(x => x.SortLeavesFirst(It.IsAny<IEnumerable<ComponentSave>>(), It.IsAny<GumProjectSave>()))
            .Returns((IEnumerable<ComponentSave> components, GumProjectSave _) => components.Reverse().ToList());
        _importService.Setup(x => x.GetDestinationStatus(It.IsAny<GumProjectSave>(), It.IsAny<string>()))
            .Returns(ImportDestinationStatus.None);
        _modifiedStandards.Setup(x => x.FindModifiedInUse(It.IsAny<GumProjectSave>())).Returns(new List<string>());
        _importService
            .Setup(x => x.ImportAsync(
                It.IsAny<ImportSelections>(), It.IsAny<GumProjectSave>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ConflictResolution>(), It.IsAny<Action?>(), It.IsAny<bool>()))
            .Callback((ImportSelections selections, GumProjectSave _, string sourceBase, string subfolder,
                ConflictResolution _, Action? beforeImport, bool replaceStandardAnimations) =>
            {
                _importedSelections = selections;
                _importedSourceBase = sourceBase;
                _importedSubfolder = subfolder;
                _importedReplaceStandardAnimations = replaceStandardAnimations;
                _importHook = beforeImport;
                beforeImport?.Invoke();
            })
            .ReturnsAsync(() => _importResult);

        _importer = new FormsThemeImporter(
            _formsFileService.Object,
            _dialogService.Object,
            _projectState.Object,
            _fileWatch.Object,
            _skiaShapeStandards.Object,
            _fontManager.Object,
            new ImportVersionGate(),
            _importDialog.Object,
            _sourceService.Object,
            _dependencyResolver.Object,
            _importService.Object,
            _modifiedStandards.Object);
    }

    private static ComponentSave Component(string name) => new ComponentSave { Name = name };

    private void AddThemeContent()
    {
        _theme.Components.Add(Component("Neon/Styles"));
        _theme.Components.Add(Component("Neon/Controls/Button"));
        _theme.Screens.Add(new ScreenSave { Name = "Neon/DemoScreenGum" });
        _theme.Screens.Add(new ScreenSave { Name = "Neon/OtherScreen" });
        _theme.Behaviors.Add(new Gum.DataTypes.Behaviors.BehaviorSave { Name = "ButtonBehavior" });
        _theme.StandardElements.Add(new StandardElementSave { Name = "Text" });
    }

    private void SetDifferingInProject(params ImportItemKey[] keys)
    {
        _importService.Setup(x => x.GetDestinationStatus(It.IsAny<GumProjectSave>(), string.Empty))
            .Returns(new ImportDestinationStatus(new HashSet<ImportItemKey>(keys), new HashSet<ImportItemKey>()));
    }

    private void UserAnswersTheConflictPrompt(MessageDialogResult answer) =>
        _dialogService.Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(),
                It.Is<MessageDialogStyle?>(s => s != null && s.AffirmativeText == "Pick what to import")))
            .Returns(answer);

    [Theory]
    [InlineData("Standard", false, true, false)]
    [InlineData("Neon", true, false, true)]
    public async Task ImportThemeAsync_ImportsTheThemeThroughTheSharedService_WithOnlyWhatTheThemeMayTouch(
        string themeName, bool includeDemoScreen, bool expectStandards, bool expectDemoScreen)
    {
        AddThemeContent();

        bool result = await _importer.ImportThemeAsync(themeName, includeDemoScreen);

        result.ShouldBeTrue();
        _importedSelections.ShouldNotBeNull();
        // Components come back from the resolver in import order and go in as direct components, so
        // behaviors are registered before them.
        _importedSelections.DirectComponents.Select(c => c.Name).ShouldBe(new[] { "Neon/Controls/Button", "Neon/Styles" });
        _importedSelections.TransitiveComponents.ShouldBeEmpty();
        _importedSelections.Behaviors.Select(b => b.Name).ShouldBe(new[] { "ButtonBehavior" });
        _importedSelections.Standards.Select(s => s.Name).ShouldBe(expectStandards ? new[] { "Text" } : Array.Empty<string>());
        _importedSelections.DirectScreens.Select(s => s.Name)
            .ShouldBe(expectDemoScreen ? new[] { "Neon/DemoScreenGum" } : Array.Empty<string>());
        // Theme names are already prefixed, so nothing is added; the theme is read from its own folder.
        _importedSubfolder.ShouldBe("");
        _importedSourceBase.ShouldBe("C:/themes/Neon");
        // Add Forms has never touched the project's Standards animations.
        _importedReplaceStandardAnimations.ShouldBe(false);
    }

    [Fact]
    public async Task ImportThemeAsync_TellsTheUserAndStopsAtOnce_WhenTheThemeProjectCannotBeLoaded()
    {
        _sourceService.Setup(x => x.LoadProjectAsync(ThemeProjectPath, null)).ReturnsAsync((GumProjectSave?)null);

        bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        result.ShouldBeFalse();
        _dialogService.Verify(
            x => x.ShowMessage(It.Is<string>(m => m.Contains(ThemeProjectPath)), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Once);
        _importService.Verify(x => x.ImportAsync(
            It.IsAny<ImportSelections>(), It.IsAny<GumProjectSave>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<ConflictResolution>(), It.IsAny<Action?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ImportThemeAsync_ReturnsFalse_WhenNoProjectIsOpen()
    {
        _projectState.Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);

        bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    [InlineData(3, 5, true)]
    public async Task ImportThemeAsync_RejectsAThemeNewerThanTheProject_WithoutImporting(
        int themeVersion, int projectVersion, bool shouldImport)
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumImporterVersion_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"),
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GumProjectSave>\n  <Version>{themeVersion}</Version>\n</GumProjectSave>");
            _projectState.Setup(x => x.GumProjectSave)
                .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumx", Version = projectVersion });
            _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns(themeDirectory);

            bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

            result.ShouldBe(shouldImport);
            (_importedSelections != null).ShouldBe(shouldImport);
            _dialogService.Verify(
                x => x.ShowMessage(It.Is<string>(m => m.Contains("upgrading-file-gumx-version")), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
                shouldImport ? Times.Never() : Times.Once());
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData("32-126,160-255", true)]
    [InlineData("32-126,160-255,10003-10007", false)]
    public async Task ImportThemeAsync_ClearsTheFontCacheOnlyWhenTheThemeWidensTheProjectsFontRanges(
        string projectRanges, bool shouldClearCache)
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumImporterRanges_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GumProjectSave>\n  <FontRanges>32-126,10003-10007</FontRanges>\n</GumProjectSave>");
            GumProjectSave project = new GumProjectSave { FullFileName = "C:/project/Test.gumx", FontRanges = projectRanges };
            _projectState.Setup(x => x.GumProjectSave).Returns(project);
            _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns(themeDirectory);

            await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

            _fontManager.Verify(x => x.DeleteFontCacheFolder(), shouldClearCache ? Times.Once() : Times.Never());
            project.FontRanges.ShouldContain("10003");
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_AppliesTheThemesRequirementsThroughTheImportsBeforeImportHook()
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumImporterHook_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GumProjectSave>\n  <FontRanges>32-126,10003-10007</FontRanges>\n</GumProjectSave>");
            GumProjectSave project = new GumProjectSave { FullFileName = "C:/project/Test.gumx", FontRanges = "32-126" };
            _projectState.Setup(x => x.GumProjectSave).Returns(project);
            _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns(themeDirectory);
            string? rangesWhenImportStarted = null;
            _importService
                .Setup(x => x.ImportAsync(
                    It.IsAny<ImportSelections>(), It.IsAny<GumProjectSave>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<ConflictResolution>(), It.IsAny<Action?>(), It.IsAny<bool>()))
                .Callback(() => rangesWhenImportStarted = project.FontRanges)
                .ReturnsAsync(() => _importResult);

            await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

            // Nothing is applied until the import runs the hook it was given.
            rangesWhenImportStarted.ShouldBe("32-126");
            project.FontRanges.ShouldBe("32-126");
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_TellsTheUserAndStillImports_WhenTheFontCacheCannotBeCleared()
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumImporterRanges_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GumProjectSave>\n  <FontRanges>32-126,10003-10007</FontRanges>\n</GumProjectSave>");
            _projectState.Setup(x => x.GumProjectSave)
                .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumx", FontRanges = "32-126" });
            _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns(themeDirectory);
            _fontManager.Setup(x => x.DeleteFontCacheFolder()).Throws(new IOException("locked"));

            bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

            result.ShouldBeTrue();
            _dialogService.Verify(
                x => x.ShowMessage(It.Is<string>(m => m.Contains("FontCache")), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_TellsTheUserToSaveByHand_WhenTheImportCouldNotSaveTheProject()
    {
        _importResult = new ImportResult { NeedsManualSave = true };

        bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
        _dialogService.Verify(
            x => x.ShowMessage("You must Save, then close/reopen the project.", null, null),
            Times.Once);
    }

    [Fact]
    public async Task ImportThemeAsync_SaysNothingExtra_WhenTheImportGoesThrough()
    {
        bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportThemeAsync_NamesWhatWasNotAdded_WhenFilesThoseElementsUseAreMissing()
    {
        _importResult.SkippedElements.Add("Neon/Controls/Icon");

        await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        _dialogService.Verify(
            x => x.ShowMessage(It.Is<string>(m => m.Contains("Neon/Controls/Icon")), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportThemeAsync_ReturnsFalse_WhenSomethingConflictsAfterTheCheck()
    {
        _importResult.ConflictingElements.Add("Neon/Controls/Button");

        bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        result.ShouldBeFalse();
        _dialogService.Verify(
            x => x.ShowMessage(It.Is<string>(m => m.Contains("Neon/Controls/Button")), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportThemeAsync_ReturnsFalseWithoutImporting_WhenTheUserCancelsOverItemsThatWouldBeOverwritten()
    {
        // Something the project has with different content is a conflict. The user is offered "Pick
        // what to import" or "Cancel"; Cancel (the mock's default answer) leaves the project alone.
        AddThemeContent();
        SetDifferingInProject(new ImportItemKey(ElementItemType.Component, "Neon/Controls/Button"));

        bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        result.ShouldBeFalse();
        _dialogService.Verify(
            x => x.ShowMessage(It.Is<string>(m => m.Contains("Components/Neon/Controls/Button")), It.IsAny<string?>(),
                It.Is<MessageDialogStyle?>(s => s != null && s.AffirmativeText == "Pick what to import" && s.NegativeText == "Cancel")),
            Times.Once);
        _importDialog.Verify(x => x.ShowAsync(It.IsAny<FixedImportSource>()), Times.Never);
        _importedSelections.ShouldBeNull();
    }

    [Fact]
    public async Task ImportThemeAsync_IgnoresDifferingItemsItWillNotImport()
    {
        // The other screen is not the demo screen and the demo screen was not asked for.
        AddThemeContent();
        SetDifferingInProject(new ImportItemKey(ElementItemType.Screen, "Neon/OtherScreen"));

        bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Standard", true, false)]
    [InlineData("Neon", false, true)]
    public async Task ImportThemeAsync_OpensTheImportDialogOnTheThemeProject_WhenTheUserPicksWhatToImport(
        string themeName, bool expectStandards, bool includeDemoScreen)
    {
        AddThemeContent();
        SetDifferingInProject(new ImportItemKey(ElementItemType.Behavior, "ButtonBehavior"));
        UserAnswersTheConflictPrompt(MessageDialogResult.Affirmative);
        FixedImportSource? shown = null;
        _importDialog.Setup(x => x.ShowAsync(It.IsAny<FixedImportSource>()))
            .Callback<FixedImportSource>(source => shown = source)
            .ReturnsAsync(true);

        bool result = await _importer.ImportThemeAsync(themeName, includeDemoScreen);

        result.ShouldBeTrue();
        shown.ShouldNotBeNull();
        shown.GumxPath.ShouldBe(ThemeProjectPath);
        shown.IncludeStandards.ShouldBe(expectStandards);
        shown.PreselectScreens.ShouldBe(includeDemoScreen);
        shown.BeforeImport.ShouldNotBeNull();
        _importedSelections.ShouldBeNull("the dialog does the importing");
    }

    [Fact]
    public async Task ImportThemeAsync_AppliesThemeRequirementsOnlyWhenThePickedImportRuns()
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumImporterPick_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "GumProject.gumx"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GumProjectSave>\n  <FontRanges>32-126,10003-10007</FontRanges>\n</GumProjectSave>");
            GumProjectSave project = new GumProjectSave { FullFileName = "C:/project/Test.gumx", FontRanges = "32-126" };
            _projectState.Setup(x => x.GumProjectSave).Returns(project);
            _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns(themeDirectory);
            AddThemeContent();
            SetDifferingInProject(new ImportItemKey(ElementItemType.Component, "Neon/Controls/Button"));
            UserAnswersTheConflictPrompt(MessageDialogResult.Affirmative);
            FixedImportSource? shown = null;
            _importDialog.Setup(x => x.ShowAsync(It.IsAny<FixedImportSource>()))
                .Callback<FixedImportSource>(source => shown = source)
                .ReturnsAsync(true);

            await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

            // Nothing is applied to the project merely by opening the dialog...
            project.FontRanges.ShouldBe("32-126");
            _fontManager.Verify(x => x.DeleteFontCacheFolder(), Times.Never);

            // ...only when the import it hosts goes ahead.
            shown!.BeforeImport!();
            project.FontRanges.ShouldContain("10003");
            _fontManager.Verify(x => x.DeleteFontCacheFolder(), Times.Once);
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_TreatsAStandardTheUserModifiedAndUses_AsAConflict_ListedWithTheRest()
    {
        // Replacing it would lose the user's styling, so it goes through the same Pick/Cancel choice.
        AddThemeContent();
        _modifiedStandards.Setup(x => x.FindModifiedInUse(_theme)).Returns(new List<string> { "Text" });

        bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

        result.ShouldBeFalse("Cancel is the mock's default answer");
        _dialogService.Verify(
            x => x.ShowMessage(It.Is<string>(m => m.Contains("Standards/Text")), It.IsAny<string?>(),
                It.Is<MessageDialogStyle?>(s => s != null && s.AffirmativeText == "Pick what to import")),
            Times.Once);
        _importedSelections.ShouldBeNull();
    }

    [Fact]
    public async Task ImportThemeAsync_ImportsWithoutAsking_WhenNoModifiedStandardIsInUse()
    {
        AddThemeContent();

        bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportThemeAsync_NeverLooksAtStandards_ForAThemeThatDoesNotTouchThem()
    {
        _modifiedStandards.Setup(x => x.FindModifiedInUse(It.IsAny<GumProjectSave>()))
            .Throws(new InvalidOperationException("a non-default theme has no Standards to ask about"));

        bool result = await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ImportThemeAsync_CopiesTheFontLicenseFiles_ButNotOverOnesTheProjectHas()
    {
        string root = Path.Combine(Path.GetTempPath(), "GumImporterLicense_" + Guid.NewGuid().ToString("N"));
        string themeDirectory = Path.Combine(root, "theme");
        string projectDirectory = Path.Combine(root, "project");
        Directory.CreateDirectory(Path.Combine(themeDirectory, "Fonts"));
        Directory.CreateDirectory(Path.Combine(projectDirectory, "Fonts"));
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, "Fonts", "Neon-LICENSE.txt"), "new license");
            File.WriteAllText(Path.Combine(themeDirectory, "Fonts", "OFL.txt"), "theme OFL");
            File.WriteAllBytes(Path.Combine(themeDirectory, "Fonts", "Neon.ttf"), new byte[] { 1 });
            File.WriteAllText(Path.Combine(projectDirectory, "Fonts", "OFL.txt"), "project OFL");
            _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns(themeDirectory);
            _projectState.Setup(x => x.ProjectDirectory).Returns(projectDirectory);

            await _importer.ImportThemeAsync("Neon", isIncludeDemoScreenGum: false);

            File.ReadAllText(Path.Combine(projectDirectory, "Fonts", "Neon-LICENSE.txt")).ShouldBe("new license");
            File.ReadAllText(Path.Combine(projectDirectory, "Fonts", "OFL.txt")).ShouldBe("project OFL");
            File.Exists(Path.Combine(projectDirectory, "Fonts", "Neon.ttf")).ShouldBeFalse("fonts come with the import, not here");
            _fileWatch.Verify(
                x => x.IgnoreNextChangeUntil(It.Is<FilePath>(p => p.FullPath.EndsWith("Neon-LICENSE.txt")), It.IsAny<DateTime?>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
