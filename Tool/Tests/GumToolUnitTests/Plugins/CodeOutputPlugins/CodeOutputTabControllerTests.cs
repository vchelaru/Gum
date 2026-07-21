using CodeOutputPlugin;
using CodeOutputPlugin.Manager;
using CodeOutputPlugin.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace GumToolUnitTests.Plugins.CodeOutputPlugins;

/// <summary>
/// Covers <see cref="CodeOutputTabController"/> (issue #3917), the ambient-input-reading handler logic
/// extracted from <c>MainCodeOutputPlugin</c>. Unlike a drop-in <c>AnimationTabController</c>-style
/// extraction, this controller reads the view (<see cref="ICodeOutputTabView"/>) and the tab's
/// visibility/selection state (<see cref="ITabVisibility"/>/<see cref="ITabSelectionState"/>) as live
/// mid-method inputs, not just a push at the end - these tests pin that decision logic: what
/// <see cref="CodeOutputTabController.RefreshCodeDisplay"/> renders under each
/// <see cref="GenerationBehavior"/>/visibility combination, and when
/// <see cref="CodeOutputTabController.HandleRefreshAndExport"/> actually triggers code generation.
/// </summary>
public class CodeOutputTabControllerTests : BaseTestClass
{
    private readonly Mock<ICodeOutputTabView> _view = new();
    private readonly Mock<ITabVisibility> _tabVisibility = new();
    private readonly Mock<ITabSelectionState> _tabSelectionState = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly CodeGenerator _codeGenerator;
    private readonly CodeGenerationService _codeGenerationService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly CodeOutputProjectSettingsManager _codeOutputProjectSettingsManager;
    private readonly CodeWindowViewModel _viewModel;
    private readonly string _tempDirectory;

    public CodeOutputTabControllerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCodeOutputTabControllerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        Mock<INameVerifier> mockNameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new(mockNameVerifier.Object);
        // Trailing separator matters: the code-generation path helpers do plain string
        // concatenation ("folder" + "Screens\\Foo.cs"), not Path.Combine.
        FixedProjectDirectoryProvider directoryProvider = new(_tempDirectory + Path.DirectorySeparatorChar);
        _elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new();
        _codeGenerator = new CodeGenerator(codeGenNameVerifier, localizationService, _elementSettingsManager, directoryProvider);
        CustomCodeGenerator customCodeGenerator = new(_codeGenerator, codeGenNameVerifier);

        Mock<IRetryService> retryService = new();
        retryService
            .Setup(r => r.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()))
            .Callback<Action, int>((action, _) => action());

        _codeGenerationService = new CodeGenerationService(
            Mock.Of<IGuiCommands>(),
            _codeGenerator,
            Mock.Of<IDialogService>(),
            customCodeGenerator,
            codeGenNameVerifier,
            directoryProvider,
            retryService.Object);

        _codeOutputProjectSettingsManager = new CodeOutputProjectSettingsManager(Mock.Of<ICodeGenLogger>(), directoryProvider);

        _viewModel = new CodeWindowViewModel(
            _projectState.Object,
            Mock.Of<IFileCommands>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<ICodeGenerationAutoSetupService>());
    }

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }

    private CodeOutputTabController CreateController() => new(
        _view.Object,
        _tabVisibility.Object,
        _tabSelectionState.Object,
        _selectedState.Object,
        _projectState.Object,
        _codeGenerator,
        _codeGenerationService,
        _elementSettingsManager,
        _codeOutputProjectSettingsManager,
        _viewModel);

    private static ScreenSave CreateScreenWithDefaultState(GumProjectSave project, string name = "MyScreen")
    {
        ScreenSave screen = new() { Name = name };
        StateSave defaultState = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        project.Screens.Add(screen);
        return screen;
    }

    [Fact]
    public void GenerateCodeForElement_NoExplicitSettings_FallsBackToViewElementSettings_AndWritesGeneratedFile()
    {
        GumProjectSave project = new();
        ScreenSave screen = CreateScreenWithDefaultState(project);
        ObjectFinder.Self.GumProjectSave = project;

        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateManually });
        CodeOutputProjectSettings codeOutputProjectSettings = new()
        {
            CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGame
        };
        CodeOutputTabController controller = CreateController();

        controller.GenerateCodeForElement(showPopups: false, screen, codeOutputProjectSettings);

        File.Exists(Path.Combine(_tempDirectory, "Screens", "MyScreenRuntime.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    public void GenerateCodeForElement_StandardElement_NeverReadsViewSettings()
    {
        StandardElementSave element = new() { Name = "Container" };
        CodeOutputTabController controller = CreateController();

        Should.NotThrow(() => controller.GenerateCodeForElement(showPopups: false, element, new CodeOutputProjectSettings()));

        _view.VerifyGet(v => v.CodeOutputElementSettings, Times.Never);
    }

    [Fact]
    public void HandleCodeOutputPropertyChanged_PersistsElementAndProjectSettingsToDisk()
    {
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Screens"));
        ScreenSave screen = new() { Name = "MyScreen" };
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(false);
        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateManually });
        CodeOutputTabController controller = CreateController();

        controller.HandleCodeOutputPropertyChanged(new CodeOutputProjectSettings());

        File.Exists(Path.Combine(_tempDirectory, "Screens", "MyScreen.codsj")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "ProjectCodeSettings.codsj")).ShouldBeTrue();
    }

    [Fact]
    public void HandleRefreshAndExport_AutoGenerateOnChangeFalse_DoesNotGenerateCode()
    {
        GumProjectSave project = new();
        ScreenSave screen = CreateScreenWithDefaultState(project);
        ObjectFinder.Self.GumProjectSave = project;

        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(true);
        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateManually });
        CodeOutputProjectSettings codeOutputProjectSettings = new()
        {
            CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGame
        };
        CodeOutputTabController controller = CreateController();

        controller.HandleRefreshAndExport(codeOutputProjectSettings);

        File.Exists(Path.Combine(_tempDirectory, "Screens", "MyScreenRuntime.Generated.cs")).ShouldBeFalse();
    }

    [Fact]
    public void HandleRefreshAndExport_AutoGenerateOnChangeTrue_GeneratesCodeFile()
    {
        GumProjectSave project = new();
        ScreenSave screen = CreateScreenWithDefaultState(project);
        ObjectFinder.Self.GumProjectSave = project;

        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(true);
        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateAutomaticallyOnPropertyChange });
        CodeOutputProjectSettings codeOutputProjectSettings = new()
        {
            CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGame
        };
        CodeOutputTabController controller = CreateController();

        controller.HandleRefreshAndExport(codeOutputProjectSettings);

        File.Exists(Path.Combine(_tempDirectory, "Screens", "MyScreenRuntime.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    public void LoadCodeSettingsFile_ElementAndProjectLoaded_LoadsDefaultsViaElementSettingsManager()
    {
        ScreenSave screen = new() { Name = "MyScreen" };
        GumProjectSave project = new() { FullFileName = Path.Combine(_tempDirectory, "Project.gumx") };
        _projectState.Setup(p => p.GumProjectSave).Returns(project);
        _view.SetupProperty(v => v.CodeOutputElementSettings);
        CodeOutputTabController controller = CreateController();

        controller.LoadCodeSettingsFile(screen);

        // No .codsj exists on disk yet, so LoadOrCreateSettingsFor's "create defaults" branch runs,
        // which defaults AutoGenerateOnChange (GenerationBehavior) to true - distinct from the
        // "no element/project" branch below, which leaves a brand new CodeOutputElementSettings().
        _view.Object.CodeOutputElementSettings!.GenerationBehavior.ShouldBe(GenerationBehavior.GenerateAutomaticallyOnPropertyChange);
    }

    [Fact]
    public void LoadCodeSettingsFile_NullElement_SetsBrandNewDefaultSettings()
    {
        _view.SetupProperty(v => v.CodeOutputElementSettings);
        CodeOutputTabController controller = CreateController();

        controller.LoadCodeSettingsFile(null);

        _view.Object.CodeOutputElementSettings.ShouldNotBeNull();
        _view.Object.CodeOutputElementSettings!.GenerationBehavior.ShouldBe(GenerationBehavior.NeverGenerate);
    }

    [Fact]
    public void LoadCodeSettingsFile_ProjectNotLoaded_SetsBrandNewDefaultSettings()
    {
        ScreenSave screen = new() { Name = "MyScreen" };
        _projectState.Setup(p => p.GumProjectSave).Returns((GumProjectSave?)null);
        _view.SetupProperty(v => v.CodeOutputElementSettings);
        CodeOutputTabController controller = CreateController();

        controller.LoadCodeSettingsFile(screen);

        _view.Object.CodeOutputElementSettings!.GenerationBehavior.ShouldBe(GenerationBehavior.NeverGenerate);
    }

    [Fact]
    public void RefreshCodeDisplay_ElementSelectedButTabNotActive_ShowsTabButNeverTouchesElementSettings()
    {
        ScreenSave screen = new() { Name = "MyScreen" };
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(false);
        CodeOutputTabController controller = CreateController();

        controller.RefreshCodeDisplay(new CodeOutputProjectSettings());

        _tabVisibility.Verify(t => t.Show(), Times.Once);
        _tabVisibility.Verify(t => t.Hide(), Times.Never);
        _view.VerifySet(v => v.CodeOutputProjectSettings = It.IsAny<CodeOutputProjectSettings>(), Times.Never);
    }

    [Fact]
    public void RefreshCodeDisplay_GenerationBehaviorNeverGenerate_ShowsDisabledMessage()
    {
        ScreenSave screen = new() { Name = "MyScreen" };
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(true);
        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.NeverGenerate });
        CodeOutputTabController controller = CreateController();

        controller.RefreshCodeDisplay(new CodeOutputProjectSettings());

        _viewModel.Code.ShouldBe("// code generation disabled for this object");
        _view.VerifySet(v => v.CodeOutputProjectSettings = It.IsAny<CodeOutputProjectSettings>(), Times.Once);
    }

    [Fact]
    public void RefreshCodeDisplay_ManualGeneration_SelectedElementView_PopulatesGeneratedElementCode()
    {
        GumProjectSave project = new();
        ScreenSave screen = CreateScreenWithDefaultState(project);
        ObjectFinder.Self.GumProjectSave = project;

        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _selectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(true);
        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateManually });
        _viewModel.WhatToView = WhatToView.SelectedElement;
        CodeOutputTabController controller = CreateController();

        controller.RefreshCodeDisplay(new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

        _viewModel.Code.ShouldStartWith("//Code for");
        _viewModel.Code.ShouldContain("MyScreen");
    }

    [Fact]
    public void RefreshCodeDisplay_ManualGeneration_SelectedStateView_PopulatesGeneratedStateCode()
    {
        GumProjectSave project = new();
        ScreenSave screen = CreateScreenWithDefaultState(project);
        ObjectFinder.Self.GumProjectSave = project;

        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        _selectedState.Setup(s => s.SelectedStateSave).Returns(screen.States[0]);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(true);
        _view.SetupProperty(v => v.CodeOutputElementSettings,
            new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateManually });
        _viewModel.WhatToView = WhatToView.SelectedState;
        CodeOutputTabController controller = CreateController();

        controller.RefreshCodeDisplay(new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

        _viewModel.Code.ShouldStartWith("//State Code for Default:");
    }

    [Fact]
    public void RefreshCodeDisplay_NoElementSelected_HidesTabAndShowsSelectPrompt()
    {
        _selectedState.Setup(s => s.SelectedElement).Returns((ElementSave?)null);
        _tabSelectionState.Setup(t => t.IsSelected).Returns(true);
        CodeOutputTabController controller = CreateController();

        controller.RefreshCodeDisplay(new CodeOutputProjectSettings());

        _tabVisibility.Verify(t => t.Hide(), Times.Once);
        _tabVisibility.Verify(t => t.Show(), Times.Never);
        _viewModel.Code.ShouldBe("// Select a Screen, Component, or Standard to see generated code");
    }
}
