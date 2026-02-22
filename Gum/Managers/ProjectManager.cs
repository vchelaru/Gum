using CommonFormsAndControls.Forms;
using CommunityToolkit.Mvvm.Messaging;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Extensions;
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
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum;

public class ProjectManager : IProjectManager
{
    #region Fields

    GumProjectSave? _gumProjectSave;

    static ProjectManager mSelf;

    bool mHaveErrorsOccurredLoadingProject = false;
    
    private ISelectedState _selectedState;
    private IElementCommands _elementCommands;
    private IDialogService _dialogService;
    private IGuiCommands _guiCommands;
    private IFileCommands _fileCommands;
    private IMessenger _messenger;
    private IFileWatchManager _fileWatchManager;
    private StandardElementsManagerGumTool _standardElementsManagerGumTool;

    #endregion

    #region Properties

    public static ProjectManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new ProjectManager();
            }
            return mSelf;
        }
    }

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


    private ProjectManager()
    {

    }

    public void LoadSettings()
    {
        GeneralSettingsFile = GeneralSettingsFile.LoadOrCreateNew();
    }

    public async Task Initialize()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _elementCommands = Locator.GetRequiredService<IElementCommands>();
        _dialogService = Locator.GetRequiredService<IDialogService>();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _fileCommands = Locator.GetRequiredService<IFileCommands>();
        _messenger =  Locator.GetRequiredService<IMessenger>();
        _fileWatchManager = Locator.GetRequiredService<IFileWatchManager>();
        _standardElementsManagerGumTool = Locator.GetRequiredService<StandardElementsManagerGumTool>();

        await CommandLineManager.Self.ReadCommandLine();

        if (!CommandLineManager.Self.ShouldExitImmediately)
        {
            var isShift = (Control.ModifierKeys & Keys.Shift) != 0;

            if (!isShift && !string.IsNullOrEmpty(CommandLineManager.Self.GlueProjectToLoad))
            {
                _fileCommands.LoadProject(CommandLineManager.Self.GlueProjectToLoad);

                if (!string.IsNullOrEmpty(CommandLineManager.Self.ElementName))
                {
                    _selectedState.SelectedElement = ObjectFinder.Self.GetElementSave(CommandLineManager.Self.ElementName);
                }
            }
            else if (!isShift && !string.IsNullOrEmpty(GeneralSettingsFile.LastProject))
            {
                _fileCommands.LoadProject(GeneralSettingsFile.LastProject);

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
            if(CommandLineManager.Self.ShouldCodeGenAll)
            {
                _fileCommands.LoadProject(CommandLineManager.Self.GlueProjectToLoad);

                await _messenger.SendAsync(new RequestCodeGenerationMessage());
            }
        }
    }

    public void CreateNewProject()
    {
        _gumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _gumProjectSave;

        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_gumProjectSave);

        PluginManager.Self.ProjectLoad(_gumProjectSave);

        _fileCommands.LoadLocalizationFile();
    }

    public bool LoadProject()
    {
        var openFileDialog = new OpenFileDialog();

        openFileDialog.Filter = "Gum Project (*.gumx)|*.gumx";
        openFileDialog.Title = "Select project to load";

        DialogResult result = openFileDialog.ShowDialog();



        if (result == DialogResult.OK)
        {
            string fileName = openFileDialog.FileName;

            _selectedState.SelectedInstance = null;
            _selectedState.SelectedElement = null;

            _fileCommands.LoadProject(fileName);

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
                $"(file version {_gumProjectSave.Version}.\n\nGum supports up to version {GumProjectSave.NativeVersion}).\n\n" +
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

        WpfDataUi.Controls.FileSelectionDisplay.FolderRelativeTo = fileName.GetDirectoryContainingThis().FullPath;

        if (_gumProjectSave != null)
        {
            bool wasModified = false;
            ObjectFinder.Self.EnableCache();
            {

                wasModified = _gumProjectSave.Initialize();
                _standardElementsManagerGumTool.FixCustomTypeConverters(_gumProjectSave);
                RecreateMissingStandardElements();

                if (RecreateMissingDefinedByBaseObjects())
                {
                    wasModified = true;
                }

                if (_gumProjectSave.AddNewStandardElementTypes())
                {
                    wasModified = true;
                }
                if (FixSlashesInNames(_gumProjectSave))
                {
                    wasModified = true;
                }
                if (RemoveSpacesInVariables(_gumProjectSave))
                {
                    wasModified = true;
                }
                if (RemoveDuplicateVariables(_gumProjectSave))
                {
                    wasModified = true;
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
                wasModified = true;
            }
            PluginManager.Self.ProjectLoad(_gumProjectSave);

            if (_gumProjectSave.Version < (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                // TODO: Replace placeholder URL with actual docs URL once available
                _guiCommands.PrintOutput(
                    $"This project is using legacy version {_gumProjectSave.Version}. " +
                    $"The current version is {(int)GumProjectSave.GumxVersions.AttributeVersion}. " +
                    $"For upgrading, see https://docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version");
            }

            _standardElementsManagerGumTool.RefreshStateVariablesThroughPlugins();

            if (wasModified)
            {
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
            _fileCommands.LoadLocalizationFile();
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

    private void RecreateMissingStandardElements()
    {
        List<StandardElementSave> missingElements = new List<StandardElementSave>();
        foreach (var element in _gumProjectSave.StandardElements)
        {
            if (element.IsSourceFileMissing)
            {
                missingElements.Add(element);

            }
        }

        foreach (var element in missingElements)
        {
            var result = _dialogService.ShowYesNoMessage(
                "The following standard is missing: " + element.Name + "  Recreate it?", "Recreate " + element.Name + "?");

            if (result)
            {
                _gumProjectSave.StandardElements.RemoveAll(item => item.Name == element.Name);
                _gumProjectSave.StandardElementReferences.RemoveAll(item => item.Name == element.Name);

                var newElement = StandardElementsManager.Self.AddStandardElementSaveInstance(_gumProjectSave, element.Name);

                string gumProjectDirectory = FileManager.GetDirectory(_gumProjectSave.FullFileName);

                _gumProjectSave.SaveStandardElements(gumProjectDirectory);
            }
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
                PluginManager.Self.BeforeSavingProjectSave(GumProjectSave);

                _elementCommands.SortVariables();

                bool saveContainedElements = isNewProject || forceSaveContainedElements;

                try
                {

                    _fileWatchManager.IgnoreNextChangeUntil(GumProjectSave.FullFileName);

                    if (saveContainedElements)
                    {
                        foreach (var screenSave in GumProjectSave.Screens)
                        {
                            PluginManager.Self.BeforeSavingElementSave(screenSave);
                            _fileWatchManager.IgnoreNextChangeUntil(screenSave.GetFullPathXmlFile());
                        }
                        foreach (var componentSave in GumProjectSave.Components)
                        {
                            PluginManager.Self.BeforeSavingElementSave(componentSave);
                            _fileWatchManager.IgnoreNextChangeUntil(componentSave.GetFullPathXmlFile());
                        }
                        foreach (var standardElementSave in GumProjectSave.StandardElements)
                        {
                            PluginManager.Self.BeforeSavingElementSave(standardElementSave);
                            _fileWatchManager.IgnoreNextChangeUntil(standardElementSave.GetFullPathXmlFile());
                        }
                    }

                    // todo - this should go through the plugin...

                    GumCommands.Self.TryMultipleTimes(() => GumProjectSave.Save(GumProjectSave.FullFileName, saveContainedElements));
                    succeeded = true;

                    if (succeeded && saveContainedElements)
                    {
                        foreach (var screenSave in GumProjectSave.Screens)
                        {
                            PluginManager.Self.AfterSavingElementSave(screenSave);
                        }
                        foreach (var componentSave in GumProjectSave.Components)
                        {
                            PluginManager.Self.AfterSavingElementSave(componentSave);
                        }
                        foreach (var standardElementSave in GumProjectSave.StandardElements)
                        {
                            PluginManager.Self.AfterSavingElementSave(standardElementSave);
                        }
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    var tempFileName = FileManager.RemoveExtension(GumProjectSave.FullFileName) + DateTime.Now.ToString("s") + "gumx";
                    GumCommands.Self.TryMultipleTimes(() => GumProjectSave.Save(tempFileName, saveContainedElements));

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
                    PluginManager.Self.ProjectSave(GumProjectSave);
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

            var openFileDialog = new SaveFileDialog();

            openFileDialog.Filter = "Gum Project (*.gumx)|*.gumx";
            openFileDialog.Title = "Where would you like to save the Gum project?";

            DialogResult result = openFileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                FilePath desiredLocation = openFileDialog.FileName;
                var directory = desiredLocation.GetDirectoryContainingThis();

                if(directory.Exists())
                {
                    var files = System.IO.Directory.GetFiles(directory.FullPath);
                    var directories = System.IO.Directory.GetDirectories(directory.FullPath);

                    if(files.Length > 0 || directories.Length > 0)
                    {
                        var areYouSure = _dialogService.ShowYesNoMessage(
                            $"The location\n\n{directory}\n\nis not empty. It's best to save new Gum projects in " +
                            $"an empty folder. Do you want to continue?");

                        result = areYouSure ? DialogResult.OK : DialogResult.Cancel;
                    }
                }
            }

            if(result == DialogResult.OK)
            { 
                GumProjectSave.FullFileName = openFileDialog.FileName;
                var filePath = new FilePath(openFileDialog.FileName);
                PluginManager.Self.ProjectLocationSet(filePath);
                WpfDataUi.Controls.FileSelectionDisplay.FolderRelativeTo = filePath.GetDirectoryContainingThis().FullPath;

                shouldSave = true;
                isProjectNew = true;
            }
        }
        return shouldSave;
    }

    public static void ShowReadOnlyDialog(string fileName)
    {
        IDialogService dialogService = Locator.GetRequiredService<IDialogService>();

        string message = "Could not save the file\n\n" + fileName + "\n\nbecause it is read-only." +
            "What would you like to do?";
        DialogChoices<string> choices = new()
        {
            ["nothing"] = "Nothing (file will not save, Gum will continue to work normally)",
            ["open-folder"] = "Open folder containing file"
        };

        string? result = dialogService.ShowChoices(message, choices);

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
