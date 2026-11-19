using System.Runtime.CompilerServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Localization;
using GumRuntime;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.Startup;
using Gum.ToolStates;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Startup;

/// <summary>
/// The order of <see cref="GumStartupSequence.RunAsync"/>'s last steps. Creating the render
/// surface takes seconds on the UI thread, so it waits until the project has loaded and the tree
/// can be drawn.
/// </summary>
public class GumStartupSequenceOrderTests : BaseTestClass
{
    private readonly Func<VariableSave, bool>? _originalFixEnumerations = VariableSaveExtensionMethods.CustomFixEnumerations;
    private readonly WpfDataUi.Controls.IDataUiFilePicker? _originalFilePicker = WpfDataUi.Controls.FilePickingLogic.FilePicker;
    private readonly bool _originalBackspaceDeletes = WpfDataUi.Controls.DataUiKeyboard.BackspaceDeletes;
    private readonly Func<string, StateSave?>? _originalGetDefaultState = StandardElementsManager.Self.CustomGetDefaultState;
    private readonly Func<string, global::RenderingLibrary.ISystemManagers, global::RenderingLibrary.Graphics.IRenderable?>? _originalCreateComponent = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
    private readonly List<string> _steps = new List<string>();
    private readonly Mock<IPluginManager> _pluginManager = new Mock<IPluginManager>();

    public override void Dispose()
    {
        ElementSaveExtensions.VariableChangedThroughReference -= _pluginManager.Object.VariableSetThroughReference;
        VariableSaveExtensionMethods.CustomFixEnumerations = _originalFixEnumerations!;
        WpfDataUi.Controls.FilePickingLogic.FilePicker = _originalFilePicker!;
        WpfDataUi.Controls.DataUiKeyboard.BackspaceDeletes = _originalBackspaceDeletes;
        StandardElementsManager.Self.CustomGetDefaultState = _originalGetDefaultState!;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _originalCreateComponent!;
        base.Dispose();
    }

    [Fact]
    public async Task RunAsync_TellsTheHeadTheRenderSurfaceIsReady_OnlyAfterTheProjectHasLoaded()
    {
        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
        projectManager.Setup(manager => manager.Initialize()).Returns(() =>
        {
            _steps.Add("project loaded");
            return Task.CompletedTask;
        });
        Mock<IHeadStartup> head = new Mock<IHeadStartup>();
        head.Setup(startup => startup.RenderSurfaceReadyAsync()).Returns(() =>
        {
            _steps.Add("render surface ready");
            return Task.CompletedTask;
        });

        using ServiceProvider services = BuildServices(projectManager.Object);
        await new GumStartupSequence(services, head.Object).RunAsync();

        _steps.ShouldBe(new[] { "project loaded", "render surface ready" });
    }

    private ServiceProvider BuildServices(IProjectManager projectManager)
    {
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(projectManager);
        // Read only for its settings file, which the mocked head ignores.
        services.AddSingleton((ProjectManager)RuntimeHelpers.GetUninitializedObject(typeof(ProjectManager)));
        services.AddSingleton(Mock.Of<IThemingService>());
        services.AddSingleton(Mock.Of<ITypeManager>());
        services.AddSingleton(Mock.Of<WpfDataUi.Controls.IDataUiFilePicker>());
        services.AddSingleton(Mock.Of<IOperatingSystemInfo>());
        services.AddSingleton<IWireframeObjectManager>(new WireframeObjectManager(
            Mock.Of<IFontManager>(), Mock.Of<ISelectedState>(), Mock.Of<IDialogService>(), Mock.Of<Gum.Commands.IGuiCommands>(),
            new LocalizationService(), _pluginManager.Object, Mock.Of<IProjectState>()));
        services.AddSingleton(_pluginManager.Object);
        services.AddSingleton(Mock.Of<IStandardElementsManagerGumTool>());
        services.AddSingleton(new PeriodicUiTimer(dispatcher.Object, NullLogger<PeriodicUiTimer>.Instance));
        services.AddSingleton(Mock.Of<IFileWatchManager>());
        services.AddSingleton(Mock.Of<IProjectState>());
        return services.BuildServiceProvider();
    }
}
