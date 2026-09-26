using Avalonia.Threading;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// A new project in a temp folder, loaded into the head's real service graph, built and selected
/// through the tool's own commands so every plugin sees the events it would see in the tool. Every
/// dialog the tool opens meanwhile is answered by <see cref="Dialogs"/>. Dispose puts the tool back
/// the way it was: no project in the temp folder, no selection, the real dialogs, the user's own
/// per-user folder.
/// </summary>
internal sealed class ToolProjectFixture : IDisposable
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly string? _originalUserDataOverride;
    private readonly IDisposable _dialogScope;
    private readonly PluginManager _pluginManager;
    private readonly IEnumerable<PluginBase>? _originalPlugins;

    /// <param name="folderName">Folder under the temp folder the project goes into, named for the harness.</param>
    public ToolProjectFixture(string folderName)
    {
        // Work another test left queued (a tree view syncing its selection, say) runs now, against
        // that test's state, not later against this fixture's project and selection.
        Dispatcher.UIThread.RunJobs();
        ProjectFolder = Path.Combine(Path.GetTempPath(), folderName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ProjectFolder);

        _originalUserDataOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = Path.Combine(ProjectFolder, "UserData");
        Dialogs = new ScriptedDialogService();
        _dialogScope = ((SwitchableDialogService)Services.GetRequiredService<IDialogService>()).Use(Dialogs);
        // The editor tab draws on a canvas the headless run never builds, so every element it is
        // asked to show throws and the plugin manager disables it for the rest of the run, which
        // breaks later tests that need it (CanvasRedrawTests). It sits out while the fixture runs.
        _pluginManager = Services.GetRequiredService<PluginManager>();
        _originalPlugins = _pluginManager.Plugins;
        _pluginManager.Plugins = _pluginManager.InitializedPlugins.Where(plugin => plugin is not AvaloniaEditorTabPlugin).ToList();
        try
        {
            SelectedState = Services.GetRequiredService<ISelectedState>();
            UndoManager = Services.GetRequiredService<IUndoManager>();
            ElementCommands = Services.GetRequiredService<IElementCommands>();
            SelectedState.SelectedElement = null;
            IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
            projectManager.CreateNewProject();
            Project = projectManager.GumProjectSave!;
            Project.FullFileName = Path.Combine(ProjectFolder, "Harness.gumx");
        }
        catch
        {
            // Nothing disposes a fixture whose constructor threw.
            Restore();
            throw;
        }
    }

    /// <summary>The temp project.</summary>
    public GumProjectSave Project { get; }

    /// <summary>The folder <see cref="Project"/> lives in.</summary>
    public string ProjectFolder { get; }

    /// <summary>The scripted dialogs; queue an answer before the gesture that opens one.</summary>
    public ScriptedDialogService Dialogs { get; }

    public ISelectedState SelectedState { get; }

    public IUndoManager UndoManager { get; }

    public IElementCommands ElementCommands { get; }

    #region Building the project

    /// <summary>Adds a component of <paramref name="baseType"/> through the tool's add command, which selects it.</summary>
    public ComponentSave AddComponent(string name, string baseType = "Container")
    {
        ProjectCommands projectCommands = Services.GetRequiredService<ProjectCommands>();
        ComponentSave component = new ComponentSave();
        projectCommands.PrepareNewComponentSave(component, name, baseType);
        projectCommands.AddComponent(component);
        return component;
    }

    /// <summary>Adds a screen through the tool's add command.</summary>
    public ScreenSave AddScreen(string name) => Services.GetRequiredService<ProjectCommands>().AddScreen(name);

    /// <summary>Adds an instance of <paramref name="type"/> to <paramref name="owner"/> through the tool's command, which selects it.</summary>
    public InstanceSave AddInstance(ElementSave owner, string name, string type) =>
        ElementCommands.AddInstance(owner, name, type) ?? throw new InvalidOperationException($"A plugin rejected the instance {name}.");

    /// <summary>Adds a state category to <paramref name="owner"/>.</summary>
    public StateSaveCategory AddCategory(ElementSave owner, string name) => ElementCommands.AddCategory(owner, name);

    /// <summary>Adds a state named <paramref name="name"/> to <paramref name="category"/>.</summary>
    public StateSave AddState(ElementSave owner, StateSaveCategory category, string name) => ElementCommands.AddState(owner, category, name);

    /// <summary>The standard element named <paramref name="name"/> (Text, Sprite, Container...).</summary>
    public StandardElementSave Standard(string name) => Project.StandardElements.Single(element => element.Name == name);

    #endregion

    /// <summary>Clears the selection and the project, and restores the dialogs and per-user folder.</summary>
    public void Dispose()
    {
        try
        {
            SelectedState.SelectedInstance = null;
            SelectedState.SelectedElement = null;
            // The temp folder goes away below, so the tool must not keep a project that points into it.
            Services.GetRequiredService<IProjectManager>().CreateNewProject();
            ObjectFinder.Self.GumProjectSave = null;
        }
        finally
        {
            // A failed test's harness must still hand the dialogs back, or every later one fails too.
            Restore();
        }
        try
        {
            Directory.Delete(ProjectFolder, recursive: true);
        }
        catch
        {
            // A file watcher may still hold the folder; the temp folder is cleaned up later.
        }
    }

    private void Restore()
    {
        _pluginManager.Plugins = _originalPlugins;
        _dialogScope.Dispose();
        FileManager.UserApplicationDataFolderOverride = _originalUserDataOverride;
    }
}
