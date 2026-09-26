using CommunityToolkit.Mvvm.Messaging;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Extensions;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Gum.Startup;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum;

public class ProjectManager : IProjectManager, IDeleteProjectProvider, ICopyPasteProjectProvider, IReferenceFinderProjectProvider, IRenameProjectProvider
{
    #region Fields

    GumProjectSave? _gumProjectSave;

    bool mHaveErrorsOccurredLoadingProject = false;

    // Guards against a second LoadProjectAsync call overlapping the first now that the load is
    // async (e.g. a double-clicked recent file, or a drag-drop while a menu click is also
    // queued) - two loads racing to assign _gumProjectSave / mutate ObjectFinder.Self concurrently
    // would corrupt state. A call that arrives while one is in flight is ignored, not queued.
    private Task? _inFlightLoadProjectTask;

    private readonly ISelectedState _selectedState;
    private readonly Lazy<IElementCommands> _elementCommands;
    private readonly IDialogService _dialogService;
    private readonly IFileSystemRevealService _fileSystemRevealService;
    private readonly IGuiCommands _guiCommands;
    private readonly Lazy<IFileCommands> _fileCommands;
    private readonly IMessenger _messenger;
    private readonly Lazy<IFileWatchManager> _fileWatchManager;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IRetryService _retryService;
    // Lazy because CommandLineManager depends back on IProjectManager (ReadCommandLine's
    // rebuild-fonts path reads GumProjectSave) — deferring it breaks the construction cycle.
    private readonly Lazy<ICommandLineManager> _commandLineManager;
    private readonly IPluginManager _pluginManager;
    // Lazy because HotkeyManager depends (via ISetVariableLogic -> SetVariableLogic) back on
    // IProjectManager — deferring it breaks the construction cycle, same as _commandLineManager above.
    private readonly Lazy<IHotkeyManager> _hotkeyManager;
    private readonly IGumProjectRepairLogic _gumProjectRepairLogic;
    private readonly IFilePickingFolderProvider _filePickingFolderProvider;
    // Lazy: NewProjectLogic calls back into this manager, so a direct reference would be a
    // construction cycle.
    private readonly Lazy<INewProjectLogic> _newProjectLogic;
    private readonly IProjectOpenRequestRouter _projectOpenRequests;

    #endregion

    #region Properties

    public GumProjectSave? GumProjectSave => _gumProjectSave;

    /// <summary>
    /// The full settings object. Concrete-only -- <see cref="IProjectManager"/> intentionally exposes
    /// only the narrowed members below instead of this whole object. <see cref="Gum.Settings.GeneralSettingsFile"/>
    /// is no longer WinForms-typed itself (see <see cref="Gum.Settings.LegacyMainWindowState"/>), but the
    /// narrowed surface is kept as the smaller, encapsulated contract for headless consumers.
    /// </summary>
    public GeneralSettingsFile GeneralSettingsFile
    {
        get;
        private set;
    }

    public bool AutoSave
    {
        get => GeneralSettingsFile.AutoSave;
        set => GeneralSettingsFile.AutoSave = value;
    }

    public bool? UseStandardsPalette
    {
        get => GeneralSettingsFile.UseStandardsPalette;
        set => GeneralSettingsFile.UseStandardsPalette = value;
    }

    public bool EffectiveUseStandardsPalette => GeneralSettingsFile.EffectiveUseStandardsPalette;
    public bool ShowTextOutlines => GeneralSettingsFile.ShowTextOutlines;
    public int FrameRate => GeneralSettingsFile.FrameRate;

    public byte OutlineColorR => GeneralSettingsFile.OutlineColorR;
    public byte OutlineColorG => GeneralSettingsFile.OutlineColorG;
    public byte OutlineColorB => GeneralSettingsFile.OutlineColorB;

    public byte GuideLineColorR => GeneralSettingsFile.GuideLineColorR;
    public byte GuideLineColorG => GeneralSettingsFile.GuideLineColorG;
    public byte GuideLineColorB => GeneralSettingsFile.GuideLineColorB;

    public byte GuideTextColorR => GeneralSettingsFile.GuideTextColorR;
    public byte GuideTextColorG => GeneralSettingsFile.GuideTextColorG;
    public byte GuideTextColorB => GeneralSettingsFile.GuideTextColorB;

    public IReadOnlyList<RecentProjectReference> RecentProjects => GeneralSettingsFile.RecentProjects;

    public void SaveGeneralSettings() => GeneralSettingsFile.Save();

    public bool HaveErrorsOccurredLoadingProject
    {
        get
        {
            return mHaveErrorsOccurredLoadingProject;
        }
    }
    #endregion

    #region Methods


    public ProjectManager(
        ISelectedState selectedState,
        Lazy<IElementCommands> elementCommands,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        Lazy<IFileCommands> fileCommands,
        IMessenger messenger,
        Lazy<IFileWatchManager> fileWatchManager,
        IStandardElementsManagerGumTool standardElementsManagerGumTool,
        IRetryService retryService,
        Lazy<ICommandLineManager> commandLineManager,
        IPluginManager pluginManager,
        Lazy<IHotkeyManager> hotkeyManager,
        IGumProjectRepairLogic gumProjectRepairLogic,
        IFilePickingFolderProvider filePickingFolderProvider,
        Lazy<INewProjectLogic> newProjectLogic,
        IFileSystemRevealService fileSystemRevealService,
        IProjectOpenRequestRouter projectOpenRequests)
    {
        _newProjectLogic = newProjectLogic;
        _projectOpenRequests = projectOpenRequests;
        _fileSystemRevealService = fileSystemRevealService;
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _messenger = messenger;
        _fileWatchManager = fileWatchManager;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _retryService = retryService;
        _commandLineManager = commandLineManager;
        _pluginManager = pluginManager;
        _hotkeyManager = hotkeyManager;
        _gumProjectRepairLogic = gumProjectRepairLogic;
        _filePickingFolderProvider = filePickingFolderProvider;
        // Default settings until LoadSettings() replaces this with the loaded (or newly-created)
        // file. Avoids a null-before-load window that every narrowed IProjectManager member above
        // would otherwise need to guard against (some plugins read these before LoadSettings runs -
        // see StandardMenuModelBuilder.Build).
        GeneralSettingsFile = new GeneralSettingsFile();
    }

    public void LoadSettings()
    {
        GeneralSettingsFile = GeneralSettingsFile.LoadOrCreateNew();
    }

    public async Task Initialize()
    {
        await _commandLineManager.Value.ReadCommandLine();

        if (!_commandLineManager.Value.ShouldExitImmediately)
        {
            var isShift = _hotkeyManager.Value.IsPressedInControl(KeyCombination.Shift());

            // A project the OS asked to open (e.g. a macOS Finder double-click) is the user's
            // explicit choice, so it wins over the command line, the last project and Shift.
            if (_projectOpenRequests.TakePendingStartupProject() is { } requestedProject)
            {
                await _fileCommands.Value.LoadProjectAsync(requestedProject);
            }
            else if (!isShift && !string.IsNullOrEmpty(_commandLineManager.Value.GlueProjectToLoad))
            {
                await _fileCommands.Value.LoadProjectAsync(_commandLineManager.Value.GlueProjectToLoad);

                if (!string.IsNullOrEmpty(_commandLineManager.Value.ElementName))
                {
                    _selectedState.SelectedElement = ObjectFinder.Self.GetElementSave(_commandLineManager.Value.ElementName);
                }
            }
            else if (!isShift && !string.IsNullOrEmpty(GeneralSettingsFile.LastProject))
            {
                await _fileCommands.Value.LoadProjectAsync(GeneralSettingsFile.LastProject);

                if(GumProjectSave == null)
                {
                    // we tried loading the last file, it didn't load. If it doesn't exist, let's remove it from the last project file:
                    if(!System.IO.File.Exists(GeneralSettingsFile.LastProject))
                    {
                        GeneralSettingsFile.LastProject = string.Empty;

                        GeneralSettingsFile.Save();
                    }
                }
            }
            else
            {
                // Startup with nothing to reopen goes through the same populate-a-starter-project
                // flow as File > New Project, so a first-time user isn't dropped into a blank tool.
                await _newProjectLogic.Value.CreateNewProjectAsync();
            }

            // A request that arrived after the startup project was chosen opens now.
            await _projectOpenRequests.CompleteStartupAsync();
        }
        else
        {
            if(_commandLineManager.Value.ShouldCodeGenAll)
            {
                if (_commandLineManager.Value.GlueProjectToLoad is not { } projectToLoad)
                {
                    _guiCommands.PrintOutput("--generatecode requires a project file");
                    return;
                }

                await _fileCommands.Value.LoadProjectAsync(projectToLoad);

                await _messenger.SendAsync(new RequestCodeGenerationMessage());
            }
        }
    }

    public void CreateNewProject()
    {
        _gumProjectSave = new GumProjectSave
        {
            FontGenerator = FontGeneratorType.KernSmith,
            // PopulateProjectWithDefaultStandards (below) seeds the latest variable surface
            // on the standard elements, so stamp the project at the matching version rather
            // than the GumProjectSave ctor default. Without this, the variable-grid version
            // gate hides newer-only variables (Fill/Dropshadow/Gradient, LocalizeText, etc.)
            // on a brand-new project.
            Version = GumProjectSave.NativeVersion
        };
        ObjectFinder.Self.GumProjectSave = _gumProjectSave;

        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_gumProjectSave);

        _pluginManager.ProjectLoad(_gumProjectSave);

        _fileCommands.Value.LoadLocalizationFile();
    }

    public async Task<bool> LoadProjectAsync()
    {
        List<string>? files = _dialogService.OpenFile(new OpenFileDialogOptions
        {
            Filter = "Gum Project (*.gumx;*.gumj)|*.gumx;*.gumj",
            Title = "Select project to load",
        });

        string? fileName = files?.FirstOrDefault();

        if (fileName != null)
        {
            _selectedState.SelectedInstance = null;
            _selectedState.SelectedElement = null;

            await _fileCommands.Value.LoadProjectAsync(fileName);

            return true;
        }

        return false;
    }

    // made public so that File commands can access this function
    public Task LoadProjectAsync(FilePath fileName)
    {
        if (_inFlightLoadProjectTask != null)
        {
            _guiCommands.PrintOutput(
                $"Ignoring request to load \"{fileName}\" because a project load is already in progress.");
            return Task.CompletedTask;
        }

        _inFlightLoadProjectTask = LoadProjectCoreAsync(fileName);
        return _inFlightLoadProjectTask;
    }

    private async Task LoadProjectCoreAsync(FilePath fileName)
    {
        try
        {
            await LoadProjectUnguardedAsync(fileName);
        }
        finally
        {
            _inFlightLoadProjectTask = null;
        }
    }

    private async Task LoadProjectUnguardedAsync(FilePath fileName)
    {
        using IDisposable totalScope = StartupTiming.Time("LoadProject (total)");
        // LoadProject can legitimately take several seconds on a large project (see #4869) - long
        // enough to starve the freeze watchdog's heartbeat and have it mistake this known-long load
        // for a real hang. Suspend detection for the duration; this is a no-op on the WPF head,
        // which has no watchdog registered.
        using IDisposable watchdogSuspendScope = UiFreezeWatchdogHook.SuspendScope();
        GumLoadResult result = null!;

        using (StartupTiming.Time("  GumProjectSave.Load (xml deserialize)"))
        {
            // The XML/JSON deserialize is pure parsing with no UI-thread dependency (#4871) - move
            // it off the calling thread so it doesn't block the UI while it runs. Everything else in
            // this method stays on whatever thread resumes after the await (the UI thread, for a
            // caller with a synchronization context) since it touches live tool state.
            _gumProjectSave = await Task.Run(() => GumProjectSave.Load(fileName.FullPath, out result));
        }

        if (_gumProjectSave != null && _gumProjectSave.Version > GumProjectSave.NativeVersion)
        {
            _dialogService.ShowMessage(
                $"Could not load \"{fileName}\" because it was saved with a newer version of Gum " +
                $" - version {_gumProjectSave.Version}.\n\nGum supports up to version {GumProjectSave.NativeVersion}.\n\n" +
                $"Please update Gum to open this project.");
            _gumProjectSave = null;
        }

        string? errors = result.ErrorMessage;

        if (!string.IsNullOrEmpty(errors))
        {
            _dialogService.ShowMessage("Errors loading " + fileName + "\n\n" + errors);

            // If the file doesn't exist, that's okay we will let the user still work - it's not like they can overwrite a file that doesn't exist
            // But if it does exist, we want to be careful and not allow overwriting because they could be wiping out good data
            if (fileName.Exists())
            {
                mHaveErrorsOccurredLoadingProject = true;
            }

            // We used to not load the project, but maybe we still should, just disable autosaving
            //
            //mGumProjectSave = new GumProjectSave();
        }
        else
        {
            mHaveErrorsOccurredLoadingProject = false;
        }

        ObjectFinder.Self.GumProjectSave = _gumProjectSave;

        // A project file path is absolute, so it always has a containing directory.
        _filePickingFolderProvider.FolderRelativeTo = fileName.GetDirectoryContainingThis()!.FullPath;

        if (_gumProjectSave != null)
        {
            // Each load-time repair pass that actually changes something records its name here. If the list
            // is non-empty the project is re-saved (forcing all contained elements to disk). Carrying the
            // names -- rather than a single bool -- makes it visible in the Output tab which pass dirtied a
            // freshly-loaded project, which is otherwise painful to track down.
            List<string> modifications = new List<string>();
            ObjectFinder.Self.EnableCache();
            using (StartupTiming.Time("  load-time repair passes (total)"))
            {

                // Initialize is the heaviest pass; have it report which specific elements it changed so a
                // re-save's cause is identifiable rather than a bare "Initialize".
                List<string> initializeModifications = new List<string>();
                bool initializeChangedSomething;
                using (StartupTiming.Time("    GumProjectSave.Initialize"))
                {
                    initializeChangedSomething = _gumProjectSave.Initialize(
                        // tolerate this so we don't immediately crash the tool
                        tolerateMissingDefaultStates: true, initializeModifications);
                }
                if (initializeChangedSomething)
                {
                    foreach (string reason in initializeModifications)
                    {
                        modifications.Add($"Initialize/{reason}");
                    }
                    if (initializeModifications.Count == 0)
                    {
                        modifications.Add("Initialize");
                    }
                }
                _standardElementsManagerGumTool.FixCustomTypeConverters(_gumProjectSave);
                RecreateMissingStandardElements(_gumProjectSave);

                if (RecreateMissingDefinedByBaseObjects())
                {
                    modifications.Add(nameof(RecreateMissingDefinedByBaseObjects));
                }

                if (_gumProjectSave.AddNewStandardElementTypes())
                {
                    modifications.Add("AddNewStandardElementTypes");
                }
                if (_gumProjectRepairLogic.FixSlashesInNames(_gumProjectSave))
                {
                    modifications.Add("FixSlashesInNames");
                }
                if (_gumProjectRepairLogic.RemoveSpacesInVariables(_gumProjectSave))
                {
                    modifications.Add("RemoveSpacesInVariables");
                }
                if (_gumProjectSave.MigrateCircleRadiusToWidthHeight())
                {
                    modifications.Add("MigrateCircleRadiusToWidthHeight");
                }
                if (_gumProjectSave.StripCircleRectangleGradientColor1())
                {
                    modifications.Add("StripCircleRectangleGradientColor1");
                }
                if (_gumProjectRepairLogic.RemoveDuplicateVariables(_gumProjectSave))
                {
                    modifications.Add("RemoveDuplicateVariables");
                }

                _gumProjectSave.FixStandardVariables();
            }
            ObjectFinder.Self.DisableCache();

            FileManager.RelativeDirectory = fileName.GetDirectoryContainingThis()!.FullPath;
            _gumProjectSave.RemoveDuplicateVariables();


            GraphicalUiElement.ShowLineRectangles = _gumProjectSave.ShowOutlines;

            CopyLinkedComponents(_gumProjectSave);

            if (_gumProjectRepairLogic.FixRecursiveAssignments(_gumProjectSave))
            {
                modifications.Add("FixRecursiveAssignments");
            }
            using (StartupTiming.Time("  PluginManager.ProjectLoad (total)"))
            {
                _pluginManager.ProjectLoad(_gumProjectSave);
            }

            if (_gumProjectSave.Version < (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                // TODO: Replace placeholder URL with actual docs URL once available
                _guiCommands.PrintOutput(
                    $"This project is using legacy version {_gumProjectSave.Version}. " +
                    $"The current version is {(int)GumProjectSave.GumxVersions.AttributeVersion}. " +
                    $"For upgrading, see https://docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version");
            }

            _standardElementsManagerGumTool.RefreshStateVariablesThroughPlugins();

            if (modifications.Count > 0)
            {
                _guiCommands.PrintOutput(
                    $"Re-saving \"{fileName}\" on load because it was modified by: {string.Join(", ", modifications)}");
                SaveProject(forceSaveContainedElements: true);
            }
        }
        else
        {
            // No don't do this if it's null, why would we?
            //PluginManager.Self.ProjectLoad(mGumProjectSave);
        }

        // Deselect everything
        _selectedState.SelectedElement = null;
        _selectedState.SelectedInstance = null;
        _selectedState.SelectedBehavior = null;
        _selectedState.SelectedStateCategorySave = null;
        _selectedState.SelectedStateSave = null;


        if (_gumProjectSave != null)
        {
            _fileCommands.Value.LoadLocalizationFile();
        }

        GeneralSettingsFile.AddToRecentFilesIfNew(fileName);

        var shouldSaveSettings = false;
        if (GeneralSettingsFile.LastProject != fileName)
        {
            GeneralSettingsFile.LastProject = fileName.FullPath;
            shouldSaveSettings = true;
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            if (!fileName.Exists())
            {
                var numberRemoved = GeneralSettingsFile.RecentProjects.RemoveAll(item => item.FilePath == fileName);
                if (numberRemoved > 0)
                {
                    shouldSaveSettings = true;
                }
            }

        }

        if (shouldSaveSettings)
        {
            GeneralSettingsFile.Save();
        }
    }

    internal void CopyLinkedComponents(GumProjectSave gumProjectSave)
    {
        // Runs while loading a project from disk, so it has a file name. A project file path is
        // absolute, so it always has a containing directory.
        string projectFileName = gumProjectSave.GetSavedFileName();
        var gumDirectory = new FilePath(projectFileName).GetDirectoryContainingThis()!;
        var isJsonFormat = GumProjectSave.IsJsonFormat(projectFileName);

        void CopyReference(ElementReference reference)
        {
            if (reference.LinkType == LinkType.CopyLocally && !string.IsNullOrEmpty(reference.Link))
            {
                // copy from the original location here
                var source = gumDirectory.Original + reference.Link;
                var destination = gumDirectory.Original + reference.Subfolder + "/" + reference.Name + "." + reference.GetExtension(isJsonFormat);

                try
                {
                    System.IO.File.Copy(source, destination, overwrite: true);
                }
                catch (Exception e)
                {
                    _guiCommands.PrintOutput($"Error {e}");
                }
            }
        }

        foreach (var reference in gumProjectSave.ScreenReferences)
        {
            CopyReference(reference);
        }

        foreach (var reference in gumProjectSave.ComponentReferences)
        {
            CopyReference(reference);
        }

        foreach (var reference in gumProjectSave.StandardElementReferences)
        {
            CopyReference(reference);
        }
    }

    internal void RecreateMissingStandardElements(GumProjectSave gumProjectSave)
    {
        List<StandardElementSave> missingElements = new List<StandardElementSave>();
        foreach (var element in gumProjectSave.StandardElements)
        {
            if (element.IsSourceFileMissing)
            {
                missingElements.Add(element);

            }
        }

        List<string> unrecreatableStandardNames = new List<string>();

        foreach (var element in missingElements)
        {
            // Plugin-contributed standards (the Skia shapes Arc/Canvas/Line/Svg/LottieAnimation and the
            // legacy ColoredCircle/RoundedRectangle) are not in StandardElementsManager's built-in
            // defaults, so the tool can't rebuild them from mDefaults -- clicking "Yes" used to crash
            // with a KeyNotFoundException (#3373). Collect them and inform the user instead of offering
            // a Yes/No we can't honor.
            if (!StandardElementsManager.Self.IsDefaultType(element.Name))
            {
                unrecreatableStandardNames.Add(element.Name);
                continue;
            }

            var result = _dialogService.ShowYesNoMessage(
                "The following standard is missing: " + element.Name + "  Recreate it?", "Recreate " + element.Name + "?");

            if (result)
            {
                gumProjectSave.StandardElements.RemoveAll(item => item.Name == element.Name);
                gumProjectSave.StandardElementReferences.RemoveAll(item => item.Name == element.Name);

                StandardElementsManager.Self.AddStandardElementSaveInstance(gumProjectSave, element.Name);

                // Runs while loading a project from disk, so it has a file name.
                string projectFileName = gumProjectSave.GetSavedFileName();
                string gumProjectDirectory = FileManager.GetDirectory(projectFileName);

                // Both flags must be passed explicitly: the defaults (verbose XML) would recreate
                // the standard as a .gutx a .gumj project never loads back, and in a compact-format
                // project would write a verbose file its siblings don't match (issue #4595).
                gumProjectSave.SaveStandardElements(gumProjectDirectory,
                    useCompact: gumProjectSave.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion,
                    isJsonFormat: GumProjectSave.IsJsonFormat(projectFileName));
            }
        }

        if (unrecreatableStandardNames.Count > 0)
        {
            string names = string.Join(", ", unrecreatableStandardNames);
            _dialogService.ShowMessage(
                $"The following standard element(s) are missing and can't be recreated automatically " +
                $"because they are provided by a plugin: {names}.\n\n" +
                $"Restore the matching Standards/<name>.gutx file from version control, or re-add the " +
                $"plugin that provides it (for the Skia shapes: Plugins → Add Skia).");
        }
    }

    private bool RecreateMissingDefinedByBaseObjects()
    {
        var wasAnythingAdded = false;

        foreach (var component in this.GetLoadedProject().Components)
        {
            List<InstanceSave> necessaryInstances = new List<InstanceSave>();
            FillWithNecessaryInstances(component, necessaryInstances);
            foreach (var instanceInBase in necessaryInstances)
            {
                // see if there's a match:
                var matching = component.GetInstance(instanceInBase.Name);

                if (matching == null)
                {
                    var instance = instanceInBase.Clone();
                    instance.DefinedByBase = true;
                    component.Instances.Add(instance);
                    wasAnythingAdded = true;
                }
            }
        }
        return wasAnythingAdded;
    }

    private void FillWithNecessaryInstances(ComponentSave component, List<InstanceSave> necessaryInstances)
    {
        if (!string.IsNullOrWhiteSpace(component.BaseType))
        {
            var baseComponent = ObjectFinder.Self.GetElementSave(component.BaseType) as ComponentSave;

            if (baseComponent != null)
            {
                necessaryInstances.AddRange(baseComponent.Instances);

                FillWithNecessaryInstances(baseComponent, necessaryInstances);
            }
        }
    }

    public bool SaveProject(bool forceSaveContainedElements = false)
    {
        bool succeeded = false;

        if (mHaveErrorsOccurredLoadingProject)
        {
            _dialogService.ShowMessage("Can't save project because errors occurred when the project was last loaded");
        }
        else
        {
            bool isNewProject;
            bool shouldSave = AskUserForProjectNameIfNecessary(out isNewProject);

            // shouldSave means a project is loaded and has a file name.
            if (shouldSave && GumProjectSave is { FullFileName: { } projectFileName } project)
            {
                _pluginManager.BeforeSavingProjectSave(project);

                _elementCommands.Value.SortVariables();

                bool saveContainedElements = isNewProject || forceSaveContainedElements;

                try
                {

                    _fileWatchManager.Value.IgnoreNextChangeUntil(projectFileName);

                    if (saveContainedElements)
                    {
                        foreach (var screenSave in project.Screens)
                        {
                            _pluginManager.BeforeSavingElementSave(screenSave);
                            if (_fileCommands.Value.GetFullPathXmlFile(screenSave, screenSave.Name) is { } screenSavePath)
                            {
                                _fileWatchManager.Value.IgnoreNextChangeUntil(screenSavePath);
                            }
                        }
                        foreach (var componentSave in project.Components)
                        {
                            _pluginManager.BeforeSavingElementSave(componentSave);
                            if (_fileCommands.Value.GetFullPathXmlFile(componentSave, componentSave.Name) is { } componentSavePath)
                            {
                                _fileWatchManager.Value.IgnoreNextChangeUntil(componentSavePath);
                            }
                        }
                        foreach (var standardElementSave in project.StandardElements)
                        {
                            _pluginManager.BeforeSavingElementSave(standardElementSave);
                            if (_fileCommands.Value.GetFullPathXmlFile(standardElementSave, standardElementSave.Name) is { } standardElementSavePath)
                            {
                                _fileWatchManager.Value.IgnoreNextChangeUntil(standardElementSavePath);
                            }
                        }
                    }

                    // todo - this should go through the plugin...

                    _retryService.TryMultipleTimes(() => project.Save(project.FullFileName, saveContainedElements));
                    succeeded = true;

                    if (succeeded && saveContainedElements)
                    {
                        foreach (var screenSave in project.Screens)
                        {
                            _pluginManager.AfterSavingElementSave(screenSave);
                        }
                        foreach (var componentSave in project.Components)
                        {
                            _pluginManager.AfterSavingElementSave(componentSave);
                        }
                        foreach (var standardElementSave in project.StandardElements)
                        {
                            _pluginManager.AfterSavingElementSave(standardElementSave);
                        }
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    // Keep the project's own extension so the fallback copy saves in the same format
                    // (and stays loadable). "s" formats as 2026-09-03T12:34:56, whose colons are
                    // illegal in a Windows file name, so use a colon-free stamp (issue #4595).
                    var tempFileName = FileManager.RemoveExtension(projectFileName)
                        + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss")
                        + "." + FileManager.GetExtension(projectFileName);
                    _retryService.TryMultipleTimes(() => project.Save(tempFileName, saveContainedElements));

                    string? fileName = TryGetFileNameFromException(exception);
                    if (fileName != null && IsFileReadOnly(fileName))
                    {
                        ShowReadOnlyDialog(fileName);
                    }
                    else
                    {
                        _dialogService.ShowMessage($"Error trying to save the project, but backup was saved at \n\n{tempFileName}\n\n Additional information:\n\n" + exception.ToString());
                    }
                }

                // This may be the first time the file is being saved.  If so, we should make it relative
                FileManager.RelativeDirectory = FileManager.GetDirectory(project.FullFileName);

                if (succeeded)
                {
                    _pluginManager.ProjectSave(project);
                    GeneralSettingsFile.AddToRecentFilesIfNew(project.FullFileName);
                    GeneralSettingsFile.LastProject = project.FullFileName;
                    GeneralSettingsFile.Save();
                }
            }
        }

        return succeeded;
    }


    private static string? TryGetFileNameFromException(UnauthorizedAccessException exception)
    {
        string message = exception.Message;

        int start = message.IndexOf('\'') + 1;
        int end = message.IndexOf('\'', start);

        if (start != -1 && end != -1)
            return message.Substring(start, end - start);

        return null;
    }

    public string MakeAbsoluteIfNecessary(string textureAsString)
    {
        if (!string.IsNullOrEmpty(textureAsString) && FileManager.IsRelative(textureAsString))
        {
            textureAsString = FileManager.RemoveDotDotSlash(FileManager.RelativeDirectory + textureAsString);
        }
        return textureAsString;
    }

    public bool AskUserForProjectNameIfNecessary(out bool isProjectNew)
    {
        isProjectNew = false;
        // With no project loaded there is nothing to save.
        if (GumProjectSave is not { } project)
        {
            return false;
        }
        bool shouldSave = true;
        // If it's null, that means the user hasn't saved this file yet
        if (string.IsNullOrEmpty(project.FullFileName))
        {
            shouldSave = false;

            string? chosenFileName = _dialogService.SaveFile(new SaveFileDialogOptions
            {
                // .gumj (JSON, AOT-safe) is the default (#4705). It is listed first because macOS's
                // save panel appends the first extension to a name typed without one (#4980). XML stays
                // available - the user can still type/pick a .gumx name.
                Filter = "Gum Project (*.gumj;*.gumx)|*.gumj;*.gumx",
                Title = "Where would you like to save the Gum project?",
                FileName = "NewProject.gumj",
            });

            if (chosenFileName != null && !HasProjectExtension(chosenFileName))
            {
                chosenFileName += ".gumj";
            }

            bool shouldProceed = chosenFileName != null;

            if (shouldProceed)
            {
                FilePath desiredLocation = chosenFileName!;
                var directory = desiredLocation.GetDirectoryContainingThis();

                if(directory?.Exists() == true)
                {
                    var files = System.IO.Directory.GetFiles(directory.FullPath);
                    var directories = System.IO.Directory.GetDirectories(directory.FullPath);

                    if(files.Length > 0 || directories.Length > 0)
                    {
                        bool areYouSure = _dialogService.ShowYesNoMessage(
                            $"The location\n\n{directory}\n\nis not empty. It's best to save new Gum projects in " +
                            $"an empty folder. Do you want to continue?");

                        shouldProceed = areYouSure;
                    }
                }
            }

            if(shouldProceed)
            {
                project.FullFileName = chosenFileName!;
                var filePath = new FilePath(chosenFileName!);
                _pluginManager.ProjectLocationSet(filePath);
                // The save dialog returns an absolute path, so it has a containing directory.
                _filePickingFolderProvider.FolderRelativeTo = filePath.GetDirectoryContainingThis()!.FullPath;

                shouldSave = true;
                isProjectNew = true;
            }
        }
        return shouldSave;
    }

    private static bool HasProjectExtension(string fileName)
    {
        string extension = System.IO.Path.GetExtension(fileName);
        return extension.Equals(".gumj", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".gumx", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public void ShowReadOnlyDialog(string fileName)
    {
        string message = "Could not save the file\n\n" + fileName + "\n\nbecause it is read-only." +
            "What would you like to do?";
        DialogChoices<string> choices = new()
        {
            ["nothing"] = "Nothing (file will not save, Gum will continue to work normally)",
            ["open-folder"] = "Open folder containing file"
        };

        string? result = _dialogService.ShowChoices(message, choices);

        if (result == "open-folder")
        {
            // Select the file in the file manager rather than only opening its folder; the
            // reveal service issues the right command per OS.
            _fileSystemRevealService.RevealFile(fileName);
        }
    }

    public static bool IsFileReadOnly(string fileName)
    {
        return System.IO.File.Exists(fileName) && new FileInfo(fileName).IsReadOnly;
    }

    #endregion
}
