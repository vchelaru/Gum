using System;
using System.IO;
using CodeOutputPlugin;
using CodeOutputPlugin.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.CodeGeneration;
using Gum.Reflection;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Code Output plugin re-reads ProjectCodeSettings.codsj when it changes on disk (a pull, a
/// branch switch, a hand edit), so codegen and later settings edits never run on stale settings.
/// </summary>
public class CodeOutputPluginSettingsReloadTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly FilePath _settingsFile;
    private readonly TestCodeOutputPlugin _plugin;

    public CodeOutputPluginSettingsReloadTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumCodeOutputPluginSettingsReloadTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDirectory);
        _settingsFile = new FilePath(Path.Combine(_projectDirectory, "ProjectCodeSettings.codsj"));

        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier.Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error)).Returns(true);
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.ProjectDirectory).Returns(_projectDirectory + Path.DirectorySeparatorChar);

        _plugin = new TestCodeOutputPlugin(
            new Mock<IGuiCommands>().Object,
            new Mock<IDialogService>().Object,
            nameVerifier.Object,
            new LocalizationService(),
            projectState.Object,
            new Mock<ITypeManager>().Object,
            new Mock<IOutputManager>().Object,
            new Mock<ISelectedState>().Object,
            new Mock<IRetryService>().Object,
            new WeakReferenceMessenger(),
            new Mock<IFileCommands>().Object,
            new Mock<IDispatcher>().Object);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void FileChanged_ProjectCodeSettings_ReloadsSettings()
    {
        WriteSettings(OutputLibrary.MonoGame, "BeforePull");
        _plugin.CallProjectLoad(new GumProjectSave());

        WriteSettings(OutputLibrary.MonoGameForms, "AfterPull");
        _plugin.CallReactToFileChanged(_settingsFile);

        _plugin.CurrentProjectSettings.OutputLibrary.ShouldBe(OutputLibrary.MonoGameForms);
        _plugin.CurrentProjectSettings.RootNamespace.ShouldBe("AfterPull");
    }

    [Fact]
    public void FileChanged_OtherFile_DoesNotReloadSettings()
    {
        WriteSettings(OutputLibrary.MonoGame, "BeforePull");
        _plugin.CallProjectLoad(new GumProjectSave());

        WriteSettings(OutputLibrary.MonoGameForms, "AfterPull");
        // An element's own settings file shares the extension but is not the project's.
        _plugin.CallReactToFileChanged(new FilePath(Path.Combine(_projectDirectory, "Components", "Button.codsj")));

        _plugin.CurrentProjectSettings.RootNamespace.ShouldBe("BeforePull");
    }

    [Fact]
    public void FileChanged_UnchangedProjectCodeSettings_KeepsSameSettingsInstance()
    {
        WriteSettings(OutputLibrary.MonoGame, "BeforePull");
        _plugin.CallProjectLoad(new GumProjectSave());
        CodeOutputProjectSettings before = _plugin.CurrentProjectSettings;

        // Gum's own settings writes come back through the watcher; a new instance would rebuild the
        // settings grid while the user is editing it.
        File.WriteAllText(_settingsFile.FullPath, JsonConvert.SerializeObject(before));
        _plugin.CallReactToFileChanged(_settingsFile);

        _plugin.CurrentProjectSettings.ShouldBeSameAs(before);
    }

    [Fact]
    public void FileChanged_UnreadableProjectCodeSettings_KeepsCurrentSettings()
    {
        WriteSettings(OutputLibrary.MonoGame, "BeforePull");
        _plugin.CallProjectLoad(new GumProjectSave());

        // A half-written file or merge conflict markers must not reset everything to defaults,
        // which the next settings edit would then write back over the file.
        File.WriteAllText(_settingsFile.FullPath, "<<<<<<< HEAD\n{ \"RootNamespace\": \"Mine\"\n=======");
        _plugin.CallReactToFileChanged(_settingsFile);

        _plugin.CurrentProjectSettings.RootNamespace.ShouldBe("BeforePull");
        _plugin.CurrentProjectSettings.OutputLibrary.ShouldBe(OutputLibrary.MonoGame);
    }

    private void WriteSettings(OutputLibrary outputLibrary, string rootNamespace)
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = outputLibrary,
            RootNamespace = rootNamespace,
        };
        File.WriteAllText(_settingsFile.FullPath, JsonConvert.SerializeObject(settings));
    }

    private sealed class TestCodeOutputPlugin : CodeOutputPluginBase
    {
        public TestCodeOutputPlugin(
            IGuiCommands guiCommands,
            IDialogService dialogService,
            INameVerifier nameVerifier,
            LocalizationService localizationService,
            IProjectState projectState,
            ITypeManager typeManager,
            IOutputManager outputManager,
            ISelectedState selectedState,
            IRetryService retryService,
            IMessenger messenger,
            IFileCommands fileCommands,
            IDispatcher dispatcher)
            : base(guiCommands, dialogService, nameVerifier, localizationService, projectState, typeManager,
                outputManager, selectedState, retryService, messenger, fileCommands, dispatcher)
        {
        }

        public CodeOutputProjectSettings CurrentProjectSettings => ProjectSettings;

        protected override ICodeOutputTabHost CreateTabHost(CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers) =>
            throw new NotSupportedException("The tab is not built in this test.");
    }
}
