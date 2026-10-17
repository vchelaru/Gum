using System;
using System.IO;
using System.Linq;
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
using Gum.SettingsWindow;
using Gum.ToolStates;
using Moq;
using Newtonsoft.Json;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Code Output plugin's project-wide settings are the Code generation page of the Settings
/// window; the Code tab keeps the preview, the generate commands and the selected element's settings,
/// and links to the page.
/// </summary>
public class CodeOutputPluginSettingsPageTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly SettingsPageRegistry _registry;
    private readonly Mock<ISettingsWindowService> _settingsWindow;
    private readonly Mock<ICodeOutputTabHost> _host;
    private readonly TestCodeOutputPlugin _plugin;

    public CodeOutputPluginSettingsPageTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumCodeOutputPluginSettingsPageTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDirectory);

        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier.Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error)).Returns(true);
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.ProjectDirectory).Returns(_projectDirectory + Path.DirectorySeparatorChar);

        _host = new Mock<ICodeOutputTabHost>();
        _host.SetupGet(x => x.Control).Returns(new object());

        Mock<IPluginTab> pluginTab = new();
        pluginTab.As<ITabSelectionState>();
        Mock<ITabManager> tabManager = new();
        tabManager.Setup(x => x.AddControl(It.IsAny<object>(), "Code", It.IsAny<TabLocation>())).Returns(pluginTab.Object);

        _registry = new SettingsPageRegistry();
        _settingsWindow = new Mock<ISettingsWindowService>();

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
            new Mock<IDispatcher>().Object,
            _host.Object)
        {
            TabManager = tabManager.Object,
            SettingsPageRegistry = _registry,
            SettingsWindowService = _settingsWindow.Object,
        };
        _plugin.StartUp();
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
    public void StartUp_RegistersTheCodeGenerationPageInTheProjectSection()
    {
        SettingsPage page = _registry.Pages.ShouldHaveSingleItem();

        page.Id.ShouldBe(SettingsPageIds.CodeGeneration);
        page.Title.ShouldBe("Code generation");
        page.Section.ShouldBe(SettingsSection.Project);
        page.Content.ShouldBeOfType<CodeGenerationSettingsViewModel>();
    }

    [Fact]
    public void TheCodeTabsEditProjectSettingsLink_OpensTheSettingsWindowOnThePage()
    {
        _host.Raise(x => x.EditProjectSettingsClicked += null, EventArgs.Empty);

        _settingsWindow.Verify(x => x.Show(SettingsPageIds.CodeGeneration), Times.Once);
    }

    [Fact]
    public void ActivatingThePage_ShowsTheRowsOfTheLoadedProjectsSettings()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.Maui };
        File.WriteAllText(Path.Combine(_projectDirectory, "ProjectCodeSettings.codsj"), JsonConvert.SerializeObject(settings));
        _plugin.CallProjectLoad(new GumProjectSave());
        SettingsPage page = _registry.Pages.Single();

        page.Activated!.Invoke();

        // The density rows exist only for the libraries that use them, so they show the page reads the loaded settings.
        ((CodeGenerationSettingsViewModel)page.Content).BuildCategories()[0].Members
            .Select(member => member.Name).ShouldContain("Adjust Pixel Values for Density");
    }

    private sealed class TestCodeOutputPlugin : CodeOutputPluginBase
    {
        private readonly ICodeOutputTabHost _tabHost;

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
            IDispatcher dispatcher,
            ICodeOutputTabHost tabHost)
            : base(guiCommands, dialogService, nameVerifier, localizationService, projectState, typeManager,
                outputManager, selectedState, retryService, messenger, fileCommands, dispatcher)
        {
            _tabHost = tabHost;
        }

        protected override ICodeOutputTabHost CreateTabHost(CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers) => _tabHost;
    }
}
