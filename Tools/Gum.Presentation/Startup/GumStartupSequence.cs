using System;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Dialogs;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Startup;

/// <summary>
/// The ordered startup of the tool after the service container exists: settings, theme, type
/// manager, views, plugins, standard elements, project load, and the file-watch flush timer. The
/// order is the contract; both heads run this one sequence and supply the framework-specific
/// steps through <see cref="IHeadStartup"/>.
/// </summary>
public class GumStartupSequence
{
    private readonly IServiceProvider _services;
    private readonly IHeadStartup _head;

    /// <summary>Creates the sequence over a built container and the head's step implementations.</summary>
    public GumStartupSequence(IServiceProvider services, IHeadStartup head)
    {
        _services = services;
        _head = head;
    }

    /// <summary>Runs every step in order. Returns once the project (if any) has loaded.</summary>
    public async Task RunAsync()
    {
        IProjectManager projectManager = _services.GetRequiredService<IProjectManager>();

        // This has to happen before plugins are loaded since they may depend on settings.
        projectManager.LoadSettings();
        StartupTiming.Mark("ProjectManager.LoadSettings");

        // Migration needs the whole settings object, so it resolves the concrete ProjectManager
        // (same singleton - LoadSettings() above already ran against it).
        _head.MigrateLegacySettings(_services.GetRequiredService<ProjectManager>().GeneralSettingsFile);
        StartupTiming.Mark("MigrateAppSettings");
        _services.GetRequiredService<IThemingService>().ApplyInitialTheme();
        StartupTiming.Mark("ApplyInitialTheme");
        _services.GetRequiredService<ITypeManager>().Initialize();
        StartupTiming.Mark("TypeManager.Initialize");

        // Grid file-picking editors are created by the grid, not the container, so they share one picker.
        WpfDataUi.Controls.FilePickingLogic.FilePicker = _services.GetRequiredService<WpfDataUi.Controls.IDataUiFilePicker>();

        _head.InitializeElementTreeView();
        StartupTiming.Mark("ElementTreeViewManager.Initialize");

        // Initialized very early because other things depend on it.
        ((WireframeObjectManager)_services.GetRequiredService<IWireframeObjectManager>()).Initialize();
        StartupTiming.Mark("WireframeObjectManager.Initialize");

        // The property grid must exist before the menu strip plugin starts.
        _head.InitializePropertyGrid();
        StartupTiming.Mark("PropertyGridManager.InitializeEarly");

        _head.InitializePlugins();
        StartupTiming.Mark("PluginManager.Initialize");

        IPluginManager pluginManager = _services.GetRequiredService<IPluginManager>();
        StandardElementsManager.Self.Initialize();
        StandardElementsManager.Self.CustomGetDefaultState = pluginManager.GetDefaultStateFor;
        StartupTiming.Mark("StandardElementsManager.Initialize");

        ElementSaveExtensions.VariableChangedThroughReference += pluginManager.VariableSet;

        _services.GetRequiredService<IStandardElementsManagerGumTool>().Initialize();
        StartupTiming.Mark("StandardElementsManagerGumTool.Initialize");

        // The project manager may load a project, so wireframe controls must be set up first.
        _head.RenderSurfaceReady();
        StartupTiming.Mark("PluginManager.XnaInitialized");

        await projectManager.Initialize();
        StartupTiming.Mark("ProjectManager.Initialize (project load)");

        PeriodicUiTimer fileWatchTimer = _services.GetRequiredService<PeriodicUiTimer>();
        IFileWatchManager fileWatchManager = _services.GetRequiredService<IFileWatchManager>();
        IProjectState projectState = _services.GetRequiredService<IProjectState>();

        fileWatchTimer.Tick += () =>
        {
            GumProjectSave? gumProject = projectState.GumProjectSave;
            if (gumProject != null && !string.IsNullOrEmpty(gumProject.FullFileName))
            {
                fileWatchManager.Flush();
            }
        };

        fileWatchTimer.Start(TimeSpan.FromMilliseconds(500));
    }
}
