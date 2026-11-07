using System.Collections.Generic;
using System.IO;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for <see cref="ProjectPropertiesChangeLogic"/>, extracted
/// out of <c>MainPropertiesWindowPlugin.HandlePropertyChanged</c> into headless
/// <c>Gum.Presentation</c> (#3929) so the decision logic is testable without plugin composition.
/// </summary>
public class ProjectPropertiesChangeLogicTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IWireframeCommands> _wireframeCommands = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<ILocalizationService> _localizationService = new();
    private readonly Mock<IProjectRestrictions> _projectRestrictions = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly ProjectPropertiesChangeLogic _logic;

    public ProjectPropertiesChangeLogicTests()
    {
        _logic = new ProjectPropertiesChangeLogic(
            _projectManager.Object,
            _projectState.Object,
            _wireframeObjectManager.Object,
            _fileCommands.Object,
            _wireframeCommands.Object,
            _pluginManager.Object,
            _localizationService.Object,
            _projectRestrictions.Object,
            _guiCommands.Object);
    }

    [Fact]
    public void HandlePropertyChanged_DoesNothing_ForViewOnlyProperties()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();

        _logic.HandlePropertyChanged(viewModel, nameof(ProjectPropertiesViewModel.AvailableLanguages));

        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
        _wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HandlePropertyChanged_DoesNothing_WhenViewModelIsUpdatingFromModel()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        // SetFrom leaves IsUpdatingFromModel false when it completes, so force it back on to
        // simulate a property changing while a bulk model->VM sync is still in progress.
        typeof(ProjectPropertiesViewModel)
            .GetProperty(nameof(ProjectPropertiesViewModel.IsUpdatingFromModel))!
            .SetValue(viewModel, true);

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.ShowCheckerBackground));

        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
        _wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HandlePropertyChanged_ShowCheckerBackground_AutoSavesWithoutRefreshingWireframe()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.ShowCheckerBackground));

        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HandlePropertyChanged_LocalizationFiles_NormalizesAbsolutePathsToRelative()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        // Leading-slash literal, not "C:\...": rooted on both Windows and Unix, unlike a
        // Windows drive letter (see gum-unit-tests skill).
        _projectState.Setup(p => p.ProjectDirectory).Returns("/MyProject/");
        viewModel.LocalizationFiles = new List<string> { "/MyProject/Localization/en.csv" };

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.LocalizationFiles));

        // FileManager.Standardize normalizes to the OS-native separator, so the expected
        // relative path must too.
        viewModel.LocalizationFiles.ShouldContain($"Localization{Path.DirectorySeparatorChar}en.csv");
        _fileCommands.Verify(f => f.LoadLocalizationFile(), Times.Never);
    }

    [Fact]
    public void HandlePropertyChanged_LocalizationFiles_KeepsAbsolutePath_WhenProjectIsUnsaved()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns((string?)null);
        viewModel.LocalizationFiles = new List<string> { "/Elsewhere/Localization/en.csv" };

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.LocalizationFiles));

        viewModel.LocalizationFiles.ShouldBe(new List<string> { "/Elsewhere/Localization/en.csv" });
    }

    [Fact]
    public void HandlePropertyChanged_SinglePixelTextureFile_KeepsAbsolutePath_WhenProjectIsUnsaved()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns((string?)null);
        viewModel.SinglePixelTextureFile = "/Elsewhere/pixel.png";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.SinglePixelTextureFile));

        viewModel.SinglePixelTextureFile.ShouldBe("/Elsewhere/pixel.png");
    }

    [Fact]
    public void HandlePropertyChanged_LanguageName_SetsTheLanguageThenRefreshesTheWireframe()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _localizationService.Setup(l => l.Languages).Returns(new List<string> { "English", "Spanish" });
        List<string> calls = new();
        _localizationService.SetupSet(l => l.CurrentLanguage = It.IsAny<int>()).Callback<int>(language => calls.Add("language " + language));
        _wireframeCommands.Setup(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>())).Callback<bool, bool>((forceLayout, _) => calls.Add("refresh " + forceLayout));
        viewModel.LanguageName = "Spanish";
        calls.Clear();

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.LanguageName));

        // References that read the language are re-evaluated by the refresh, so it must come second.
        calls.ShouldBe(new[] { "language 2", "refresh True" });
    }

    [Fact]
    public void HandlePropertyChanged_LanguageName_ForTheCurrentLanguage_DoesNotRefreshTheWireframe()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _localizationService.Setup(l => l.Languages).Returns(new List<string> { "English", "Spanish" });
        viewModel.LanguageIndex = 2;
        viewModel.LanguageName = "Spanish";
        _wireframeCommands.Invocations.Clear();

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.LanguageName));

        _wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_AbsolutePathInsideTheProject_BecomesRelativeWithForwardSlashes()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns("/MyProject/");
        viewModel.EngineProfile = "/MyProject/Engines/MonoGame.gumengine";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.EngineProfile));

        viewModel.EngineProfile.ShouldBe("Engines/MonoGame.gumengine");
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_AbsolutePathOutsideTheProject_GetsDotDotSegments()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns("/Games/MyProject/");
        viewModel.EngineProfile = "/Engines/MonoGame.gumengine";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.EngineProfile));

        viewModel.EngineProfile.ShouldBe("../../Engines/MonoGame.gumengine");
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_RelativePathWithBackslashes_UsesForwardSlashes()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns("/MyProject/");
        viewModel.EngineProfile = "..\\Engines\\MonoGame.gumengine";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.EngineProfile));

        viewModel.EngineProfile.ShouldBe("../Engines/MonoGame.gumengine");
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_KeepsAbsolutePath_WhenProjectIsUnsaved()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns((string?)null);
        viewModel.EngineProfile = "/Elsewhere/MonoGame.gumengine";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.EngineProfile));

        viewModel.EngineProfile.ShouldBe("/Elsewhere/MonoGame.gumengine");
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_AppliesTheProfileAndSavesTheProject()
    {
        GumProjectSave project = new GumProjectSave();
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(project);
        _projectState.Setup(p => p.ProjectDirectory).Returns("/MyProject/");
        List<string> calls = new List<string>();
        _projectRestrictions.Setup(r => r.Reload()).Callback(() => calls.Add("reload " + project.EngineProfile));
        _wireframeObjectManager.Setup(w => w.RefreshAll(true, true)).Callback<bool, bool>((_, _) => calls.Add("canvas"));
        _guiCommands.Setup(g => g.RefreshVariables(true)).Callback<bool>(_ => calls.Add("variables"));
        _fileCommands.Setup(f => f.TryAutoSaveProject(It.IsAny<bool>())).Callback<bool>(_ => calls.Add("save"));
        viewModel.EngineProfile = "Engines/MonoGame.gumengine";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.EngineProfile));

        project.EngineProfile.ShouldBe("Engines/MonoGame.gumengine");
        calls.ShouldBe(new[] { "reload Engines/MonoGame.gumengine", "canvas", "variables", "save" });
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_Cleared_ReloadsTheUnrestrictedProject()
    {
        GumProjectSave project = new GumProjectSave { EngineProfile = "Old.gumengine" };
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(project);
        viewModel.EngineProfile = "";

        _logic.HandlePropertyChanged(viewModel, nameof(viewModel.EngineProfile));

        project.EngineProfile.ShouldBe("");
        _projectRestrictions.Verify(r => r.Reload(), Times.Once);
    }

    [Fact]
    public void HandlePropertyChanged_EngineProfile_AbsolutePath_ReloadsOnlyOnceItIsRelative()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns("/MyProject/");
        viewModel.PropertyChanged += (_, e) => _logic.HandlePropertyChanged(viewModel, e.PropertyName);

        viewModel.EngineProfile = "/MyProject/MonoGame.gumengine";

        viewModel.EngineProfile.ShouldBe("MonoGame.gumengine");
        _projectRestrictions.Verify(r => r.Reload(), Times.Once);
    }
    private static ProjectPropertiesViewModel MakeViewModel()
    {
        ProjectPropertiesViewModel viewModel = new();
        GumProjectSave gumProject = new();
        viewModel.SetFrom(gumProject);
        return viewModel;
    }
}
