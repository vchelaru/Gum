using CommunityToolkit.Mvvm.Messaging;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
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
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum;

public class ProjectManager : IProjectManager, IDeleteProjectProvider, ICopyPasteProjectProvider
{
    #region Fields

    GumProjectSave? _gumProjectSave;

    bool mHaveErrorsOccurredLoadingProject = false;

    private readonly ISelectedState _selectedState;
    private readonly Lazy<IElementCommands> _elementCommands;
    private readonly IDialogService _dialogService;
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

    #endregion

    #region Properties

    public GumProjectSave? GumProjectSave => _gumProjectSave;

    public GeneralSettingsFile GeneralSettingsFile
    {
        get;
        private set;
    }

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
        IPluginManager pluginManager)
    {
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
            var isShift = (Control.ModifierKeys & Keys.Shift) != 0;

            if (!isShift && !string.IsNullOrEmpty(_commandLineManager.Value.GlueProjectToLoad))
            {
                _fileCommands.Value.LoadProject(_commandLineManager.Value.GlueProjectToLoad);

                if (!string.IsNullOrEmpty(_commandLineManager.Value.ElementName))
                {
                    _selectedState.SelectedElement = ObjectFinder.Self.GetElementSave(_commandLineManager.Value.ElementName);
                }
            }
            else if (!isShift && !string.IsNullOrEmpty(GeneralSettingsFile.LastProject))
            {
                _fileCommands.Value.LoadProject(GeneralSettingsFile.LastProject);

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
                CreateNewProject();
            }
        }
        else
        {
            if(_commandLineManager.Value.ShouldCodeGenAll)
            {
                _fileCommands.Value.LoadProject(_commandLineManager.Value.GlueProjectToLoad);

                await _messenger.SendAsync(new RequestCodeGenerationMessage());
            }
        }
    }

    public void CreateNewProject()
    {
        _gumProjectSave = new GumProjectSave
        {
            FontGenerator = FontGeneratorType.KernSmith,
            // PopulateProjectWithDefaultStandards (below) seeds the v3 shape variable surface
            // on the standard Circle/Rectangle, so stamp the project at the matching version
            // rather than the GumProjectSave ctor default. Without this, the variable-grid
            // version gate hides Fill/Dropshadow/Gradient on a brand-new project.
            Version = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion
        };
        ObjectFinder.Self.GumProjectSave = _gumProjectSave;

        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_gumProjectSave);

        _pluginManager.ProjectLoad(_gumProjectSave);

        _fileCommands.Value.LoadLocalizationFile();
    }

    public bool LoadProject()
    {
        List<string>? files = _dialogService.OpenFile(new OpenFileDialogOptions
        {
            Filter = "Gum Project (*.gumx)|*.gumx",
            Title = "Select project to load",
        });

        string? fileName = files?.FirstOrDefault();

        if (fileName != null)
        {
            _selectedState.SelectedInstance = null;
            _selectedState.SelectedElement = null;

            _fileCommands.Value.LoadProject(fileName);

            return true;
        }

        return false;
    }

    // made public so that File commands can access this function
    public void LoadProject(FilePath fileName)
    {
        GumLoadResult result;

        _gumProjectSave = GumProjectSave.Load(fileName.FullPath, out result);

        if (_gumProjectSave != null && _gumProjectSave.Version > GumProjectSave.NativeVersion)
        {
            _dialogService.ShowMessage(
                $"Could not load \"{fileName}\" because it was saved with a newer version of Gum " +
                $" - version {_gumProjectSave.Version}.\n\nGum supports up to version {GumProjectSave.NativeVersion}.\n\n" +
                $"Please update Gum to open this project.");
            _gumProjectSave = null;
        }

        string errors = result.ErrorMessage;

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

        WpfDataUi.Controls.FilePickingLogic.FolderRelativeTo = fileName.GetDirectoryContainingThis().FullPath;

        if (_gumProjectSave != null)
        {
            // Each load-time repair pass that actually changes something records its name here. If the list
            // is non-empty the project is re-saved (forcing all contained elements to disk). Carrying the
            // names -- rather than a single bool -- makes it visible in the Output tab which pass dirtied a
            // freshly-loaded project, which is otherwise painful to track down.
            List<string> modifications = new List<string>();
            ObjectFinder.Self.EnableCache();
            {

                // Initialize is the heaviest pass; have it report which specific elements it changed so a
                // re-save's cause is identifiable rather than a bare "Initialize".
                List<string> initializeModifications = new List<string>();
                if (_gumProjectSave.Initialize(
                    // tolerate this so we don't immediately crash the tool
                    tolerateMissingDefaultStates: true, initializeModifications))
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
                RecreateMissingStandardElements();

                if (RecreateMissingDefinedByBaseObjects())
                {
                    modifications.Add(nameof(RecreateMissingDefinedByBaseObjects));
                }

                if (_gumProjectSave.AddNewStandardElementTypes())
                {
                    modifications.Add("AddNewStandardElementTypes");
                }
                if (FixSlashesInNames(_gumProjectSave))
                {
                    modifications.Add(nameof(FixSlashesInNames));
                }
                if (RemoveSpacesInVariables(_gumProjectSave))
                {
                    modifications.Add(nameof(RemoveSpacesInVariables));
                }
                if (_gumProjectSave.MigrateCircleRadiusToWidthHeight())
                {
                    modifications.Add("MigrateCircleRadiusToWidthHeight");
                }
                if (_gumProjectSave.StripCircleRectangleGradientColor1())
                {
                    modifications.Add("StripCircleRectangleGradientColor1");
                }
                if (RemoveDuplicateVariables(_gumProjectSave))
                {
                    modifications.Add(nameof(RemoveDuplicateVariables));
                }

                _gumProjectSave.FixStandardVariables();
            }
            ObjectFinder.Self.DisableCache();

            FileManager.RelativeDirectory = fileName.GetDirectoryContainingThis().FullPath;
            _gumProjectSave.RemoveDuplicateVariables();


            GraphicalUiElement.ShowLineRectangles = _gumProjectSave.ShowOutlines;

            CopyLinkedComponents();

            if (FixRecursiveAssignments(_gumProjectSave))
            {
                modifications.Add(nameof(FixRecursiveAssignments));
            }
            _pluginManager.ProjectLoad(_gumProjectSave);

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

    private bool RemoveSpacesInVariables(GumProjectSave gumProjectSave)
    {
        bool didChange = false;
        foreach(var element in gumProjectSave.AllElements)
        {
            foreach(var state in element.AllStates)
            {
                foreach(var variable in state.Variables)
                {
                    Replace(variable, "Base Type");
                    Replace(variable, "Children Layout");
                    Replace(variable, "Clips Children");
                    Replace(variable, "Contained Type");
                    Replace(variable, "Font Scale");
                    Replace(variable, "Height Units");
                    Replace(variable, "Texture Address");
                    Replace(variable, "Texture Height");
                    Replace(variable, "Texture Height Scale");
                    Replace(variable, "Texture Left");
                    Replace(variable, "Texture Top");
                    Replace(variable, "Texture Width");
                    Replace(variable, "Texture Width Scale");
                    Replace(variable, "Width Units");
                    Replace(variable, "Wraps Children");
                    Replace(variable, "X Origin");
                    Replace(variable, "X Units");
                    Replace(variable, "Y Origin");
                    Replace(variable, "Y Units");

                    void Replace(VariableSave variableSave, string oldName)
                    {
                        if(variable.Name.EndsWith(oldName))
                        {
                            var newName = variable.Name.Substring(0, variable.Name.Length - oldName.Length) +
                                oldName.Replace(" ", "");
                            variable.Name = newName;
                            didChange = true;
                        }
                    }
                }
            }
        }
        return didChange;
    }

    private bool FixRecursiveAssignments(GumProjectSave gumProjectSave)
    {
        var toReturn = false;
        // Instances can't be of type screen, so don't check this (unless someone messes with the XML but that's on them)
        //foreach(var screen in mGumProjectSave.Screens)
        //{
        //    if(FixRecursiveAssignments(screen))
        //    {
        //        toReturn = true;
        //    }
        //}
        foreach (var component in gumProjectSave.Components)
        {
            if (FixRecursiveAssignments(component))
            {
                toReturn = true;
            }
        }

        return toReturn;
    }

    private bool FixRecursiveAssignments(ElementSave element)
    {
        var didModify = false;
        // see if the child is either of this type, or a base type
        foreach (var instance in element.Instances)
        {
            var isRecursive = ObjectFinder.Self.IsInstanceRecursivelyReferencingElement(instance, element);

            if (isRecursive)
            {
                instance.BaseType = "Container";
                didModify = true;
            }
        }

        return didModify;
    }

    private void CopyLinkedComponents()
    {
        var gumDirectory = new FilePath(_gumProjectSave.FullFileName).GetDirectoryContainingThis();

        void CopyReference(ElementReference reference)
        {
            if (reference.LinkType == LinkType.CopyLocally && !string.IsNullOrEmpty(reference.Link))
            {
                // copy from the original location here
                var source = gumDirectory.Original + reference.Link;
                var destination = gumDirectory.Original + reference.Subfolder + "\\" + reference.Name + "." + reference.Extension;

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

        foreach (var reference in _gumProjectSave.ScreenReferences)
        {
            CopyReference(reference);
        }

        foreach (var reference in _gumProjectSave.ComponentReferences)
        {
            CopyReference(reference);
        }

        foreach (var reference in _gumProjectSave.StandardElementReferences)
        {
            CopyReference(reference);
        }
    }

    /// <summary>
    /// Fixes slashes in all references, component names, and instance references.
    /// </summary>
    /// <param name="gumProjectSave">The project for which to fix slashes.</param>
    /// <returns>Whether any changes were made.</returns>
    private bool FixSlashesInNames(GumProjectSave gumProjectSave)
    {
        var didAnythingChange = false;

        foreach (var reference in gumProjectSave.ScreenReferences)
        {
            if (reference.Name?.Contains("\\") == true)
            {
                reference.Name = reference.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
        }

        foreach (var reference in gumProjectSave.ComponentReferences)
        {
            if (reference?.Name.Contains("\\") == true)
            {
                reference.Name = reference.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
        }

        foreach(var reference in gumProjectSave.BehaviorReferences)
        {
            if(reference.Name?.Contains("\\") == true)
            {
                reference.Name = reference.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
        }


        foreach (var screen in gumProjectSave.Screens)
        {
            if (screen.Name?.Contains("\\") == true)
            {
                screen.Name = screen.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
            foreach (var instance in screen.Instances)
            {
                if (instance.BaseType?.Contains("\\") == true)
                {
                    instance.BaseType = instance.BaseType.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }

            foreach (var behavior in screen.Behaviors)
            {
                if (behavior.BehaviorName?.Contains("\\") == true)
                {
                    behavior.BehaviorName = behavior.BehaviorName.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }
        }

        foreach (var component in gumProjectSave.Components)
        {
            if (component.Name?.Contains("\\") == true)
            {
                component.Name = component.Name.Replace("\\", "/");
                didAnythingChange = true;
            }

            foreach (var instance in component.Instances)
            {
                if (instance.BaseType?.Contains("\\") == true)
                {
                    instance.BaseType = instance.BaseType.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }

            foreach(var behavior in component.Behaviors)
            {
                if(behavior.BehaviorName?.Contains("\\") == true)
                {
                    behavior.BehaviorName = behavior.BehaviorName.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }
        }

        foreach (var behavior in gumProjectSave.Behaviors)
        {
            if(behavior.Name?.Contains("\\") == true)
            {
                behavior.Name = behavior.Name.Replace("\\", "/");
                didAnythingChange = true;
            }

            foreach (var instance in behavior.RequiredInstances)
            {
                if (instance.BaseType?.Contains("\\") == true)
                {
                    instance.BaseType = instance.BaseType.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }
        }

        return didAnythingChange;
    }

    private bool RemoveDuplicateVariables(GumProjectSave gumProjectSave)
    {
        var didChange = false;
        foreach (var screen in gumProjectSave.Screens)
        {
            didChange = RemoveDuplicateVariables(screen) || didChange;
        }
        foreach (var component in gumProjectSave.Components)
        {
            didChange = RemoveDuplicateVariables(component) || didChange;
        }
        foreach (var standard in gumProjectSave.StandardElements)
        {
            didChange = RemoveDuplicateVariables(standard) || didChange;
        }
        return didChange;
    }

    private bool RemoveDuplicateVariables(ElementSave element)
    {
        var didChange = false;
        foreach (var state in element.AllStates)
        {
            didChange = RemoveDuplicateVariables(state) || didChange;
        }
        return didChange;
    }

    private bool RemoveDuplicateVariables(StateSave state)
    {
        var variableNames = state.Variables.Select(item => item.Name).ToHashSet();

        var didChange = false;
        if (variableNames.Count != state.Variables.Count)
        {
            var newVariables = new List<VariableSave>();
            foreach (var variableName in variableNames)
            {
                var matchingVariable = state.Variables.FirstOrDefault(item => item.Name == variableName);
                newVariables.Add(matchingVariable);
            }

            state.Variables.Clear();
            state.Variables.AddRange(newVariables);
            didChange = true;
        }
        return didChange;
    }

    internal void RecreateMissingStandardElements()
    {
        List<StandardElementSave> missingElements = new List<StandardElementSave>();
        foreach (var element in _gumProjectSave.StandardElements)
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
                _gumProjectSave.StandardElements.RemoveAll(item => item.Name == element.Name);
                _gumProjectSave.StandardElementReferences.RemoveAll(item => item.Name == element.Name);

                StandardElementsManager.Self.AddStandardElementSaveInstance(_gumProjectSave, element.Name);

                string gumProjectDirectory = FileManager.GetDirectory(_gumProjectSave.FullFileName);

                _gumProjectSave.SaveStandardElements(gumProjectDirectory);
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

        foreach (var component in _gumProjectSave.Components)
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

            if (shouldSave)
            {
                _pluginManager.BeforeSavingProjectSave(GumProjectSave);

                _elementCommands.Value.SortVariables();

                bool saveContainedElements = isNewProject || forceSaveContainedElements;

                try
                {

                    _fileWatchManager.Value.IgnoreNextChangeUntil(GumProjectSave.FullFileName);

                    if (saveContainedElements)
                    {
                        foreach (var screenSave in GumProjectSave.Screens)
                        {
                            _pluginManager.BeforeSavingElementSave(screenSave);
                            _fileWatchManager.Value.IgnoreNextChangeUntil(screenSave.GetFullPathXmlFile());
                        }
                        foreach (var componentSave in GumProjectSave.Components)
                        {
                            _pluginManager.BeforeSavingElementSave(componentSave);
                            _fileWatchManager.Value.IgnoreNextChangeUntil(componentSave.GetFullPathXmlFile());
                        }
                        foreach (var standardElementSave in GumProjectSave.StandardElements)
                        {
                            _pluginManager.BeforeSavingElementSave(standardElementSave);
                            _fileWatchManager.Value.IgnoreNextChangeUntil(standardElementSave.GetFullPathXmlFile());
                        }
                    }

                    // todo - this should go through the plugin...

                    _retryService.TryMultipleTimes(() => GumProjectSave.Save(GumProjectSave.FullFileName, saveContainedElements));
                    succeeded = true;

                    if (succeeded && saveContainedElements)
                    {
                        foreach (var screenSave in GumProjectSave.Screens)
                        {
                            _pluginManager.AfterSavingElementSave(screenSave);
                        }
                        foreach (var componentSave in GumProjectSave.Components)
                        {
                            _pluginManager.AfterSavingElementSave(componentSave);
                        }
                        foreach (var standardElementSave in GumProjectSave.StandardElements)
                        {
                            _pluginManager.AfterSavingElementSave(standardElementSave);
                        }
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    var tempFileName = FileManager.RemoveExtension(GumProjectSave.FullFileName) + DateTime.Now.ToString("s") + "gumx";
                    _retryService.TryMultipleTimes(() => GumProjectSave.Save(tempFileName, saveContainedElements));

                    string fileName = TryGetFileNameFromException(exception);
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
                FileManager.RelativeDirectory = FileManager.GetDirectory(GumProjectSave.FullFileName);

                if (succeeded)
                {
                    _pluginManager.ProjectSave(GumProjectSave);
                    GeneralSettingsFile.AddToRecentFilesIfNew(GumProjectSave.FullFileName);
                    GeneralSettingsFile.LastProject = GumProjectSave.FullFileName;
                    GeneralSettingsFile.Save();
                }
            }
        }

        return succeeded;
    }


    private static string TryGetFileNameFromException(UnauthorizedAccessException exception)
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
        bool shouldSave = true;
        // If it's null, that means the user hasn't saved this file yet
        if (string.IsNullOrEmpty(GumProjectSave.FullFileName))
        {
            shouldSave = false;

            string? chosenFileName = _dialogService.SaveFile(new SaveFileDialogOptions
            {
                Filter = "Gum Project (*.gumx)|*.gumx",
                Title = "Where would you like to save the Gum project?",
            });

            bool shouldProceed = chosenFileName != null;

            if (shouldProceed)
            {
                FilePath desiredLocation = chosenFileName!;
                var directory = desiredLocation.GetDirectoryContainingThis();

                if(directory.Exists())
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
                GumProjectSave.FullFileName = chosenFileName!;
                var filePath = new FilePath(chosenFileName!);
                _pluginManager.ProjectLocationSet(filePath);
                WpfDataUi.Controls.FilePickingLogic.FolderRelativeTo = filePath.GetDirectoryContainingThis().FullPath;

                shouldSave = true;
                isProjectNew = true;
            }
        }
        return shouldSave;
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
            // Let's select the file instead of just opening the folder
            //string folder = FileManager.GetDirectory(fileName);
            //Process.Start(folder);
            Process.Start("explorer.exe", "/select," + fileName);
        }
    }

    public static bool IsFileReadOnly(string fileName)
    {
        return System.IO.File.Exists(fileName) && new FileInfo(fileName).IsReadOnly;
    }

    #endregion
}
