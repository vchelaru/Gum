using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Localization;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum.Commands;

public class FileCommands : IFileCommands
{
    private readonly ILocalizationService _localizationService;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly ISelectedState _selectedState;
    private readonly Lazy<IUndoManager> _undoManager;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IOutputManager _outputManager;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;

    public FileCommands(ISelectedState selectedState,
        Lazy<IUndoManager> undoManager,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        ILocalizationService localizationService,
        IOutputManager outputManager,
        IFileWatchManager fileWatchManager,
        IProjectManager projectManager,
        IProjectState projectState)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _localizationService = localizationService;
        _fileWatchManager = fileWatchManager;
        _outputManager = outputManager;
        _projectManager = projectManager;
        _projectState = projectState;

    }

    /// <summary>
    /// Saves the current Behavior or Element
    /// </summary>

    public FilePath? ProjectDirectory => FileManager.RelativeDirectory;

    public void DeleteDirectory(FilePath directory) =>
        FileManager.DeleteDirectory(directory.FullPath);

    public void MoveToRecycleBin(FilePath filePath) =>
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath.FullPath,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

    public string[] GetFiles(string path) => System.IO.Directory.GetFiles(path);

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) =>
        System.IO.Directory.GetFiles(path, searchPattern, searchOption);

    public string ReadAllText(string path) => System.IO.File.ReadAllText(path);

    public void MoveDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        // Move files
        foreach (string file in Directory.GetFiles(source))
        {
            string destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Move(file, destFile, overwrite: true); // .NET 6+
        }

        // Move subdirectories recursively
        foreach (string dir in Directory.GetDirectories(source))
        {
            string destDir = Path.Combine(destination, Path.GetFileName(dir));
            MoveDirectory(dir, destDir);
        }

        // Clean up empty source directory
        Directory.Delete(source);
    }

    public void SaveEmbeddedResource(Assembly assembly, string resourceName, string targetFileName) =>
                FileManager.SaveEmbeddedResource(assembly, resourceName, targetFileName);

    public void TryAutoSaveCurrentObject()
    {
        if(_selectedState.SelectedBehavior != null)
        {
            TryAutoSaveBehavior(_selectedState.SelectedBehavior);
        }
        else
        {
            TryAutoSaveCurrentElement();
        }
    }

    public void TryAutoSaveCurrentElement()
    {
        TryAutoSaveElement(_selectedState.SelectedElement);
    }


    public void TryAutoSaveElement(ElementSave? elementSave)
    {
        if (_projectManager.GeneralSettingsFile.AutoSave && elementSave != null)
        {
            SaveElement(elementSave);
        }
    }

    public void TryAutoSaveBehavior(BehaviorSave behavior)
    {
        if(_projectManager.GeneralSettingsFile.AutoSave && behavior != null)
        {
            ForceSaveBehavior(behavior);
        }
    }

    // object could be an IStateContainer or IInstanceContainer,
    // and if we have 2 functions, one for each, this causes ambiguous references
    public void TryAutoSaveObject(object objectToSave)
    {
        if(objectToSave is ElementSave elementSave)
        {
            TryAutoSaveElement(elementSave);
        }
        if(objectToSave is BehaviorSave behaviorSave)
        {
            TryAutoSaveBehavior(behaviorSave);
        }
    }


    public void NewProject()
    {
        _selectedState.SelectedElement = null;
        _selectedState.SelectedInstance = null;
        _selectedState.SelectedBehavior = null;
        _selectedState.SelectedStateCategorySave = null;
        _selectedState.SelectedStateSave = null;

        _projectManager.CreateNewProject();

        _guiCommands.RefreshStateTreeView();
        _guiCommands.RefreshVariables();
    }

    /// <summary>
    /// Attempts to save the project (gumx) and optionally all contained elements.
    /// Does not save if auto save is turned off, or if there were errors loading the
    /// project.
    /// </summary>
    /// <param name="forceSaveContainedElements">Whether to also save all elements.</param>
    /// <returns>Whether a save occurred.</returns>
    public bool TryAutoSaveProject(bool forceSaveContainedElements = false)
    {
        if (_projectManager.GeneralSettingsFile.AutoSave && !_projectManager.HaveErrorsOccurredLoadingProject)
        {
            ForceSaveProject(forceSaveContainedElements);
            return true;
        }
        return false;
    }

    public void ForceSaveProject(bool forceSaveContainedElements = false)
    {
        if (_projectManager.HaveErrorsOccurredLoadingProject)
        {
            _dialogService.ShowMessage("Cannot save project because of earlier errors");
            return;
        }

        var succeeded = _projectManager.SaveProject(forceSaveContainedElements);

        if (string.IsNullOrEmpty(_projectState.GumProjectSave.FullFileName))
        {
            // The user most likely canceled the save, as such, we have no filename
            // Do nothing, do not error.
            return;
        }

        if (!succeeded)
        {
            _dialogService.ShowMessage("Cannot save project because of earlier errors");
            return;
        }

        _outputManager.AddOutput("Saved Gum project to " + _projectState.GumProjectSave.FullFileName);
        CreateDefaultFontCharacterFile();
    }

    /// <summary>
    /// This will copy the ascii character file ".gumfcs" that contains the default characters that match the BmfcSave.DefaultRanges.
    /// The file contains all the actual characters like "abcde..." etc, not the numerical range.
    /// This file is used in the project properties for the optional "Use Font Character File" checkbox.
    /// Method is non-destructive by default, it will not overwrite an existing .gumfcs file.
    /// </summary>
    public void CreateDefaultFontCharacterFile(bool forceOverwrite = false)
    {
        var gumProject = ObjectFinder.Self.GumProjectSave;
        if (gumProject == null)
            return;

        var sourceFile = System.IO.Path.Combine(GetExecutingDirectory(), "Content\\.gumfcs");
        var destinationFile = FileManager.GetDirectory(gumProject.FullFileName) + ".gumfcs";

        // Exit early if the destination file already exists and we are not forcing an overwrite
        if (System.IO.File.Exists(destinationFile) && !forceOverwrite)
        {
            return;
        }

        // Copy the file from Content to the saved Project folder
        try
        {
            System.IO.File.Copy(sourceFile, destinationFile);
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput($"Error copying .gumfcs: {e}");
        }
    }

    // Method copied from MineNineSlicePlugin.cs, potential candidate for DRY refactor
    static string GetExecutingDirectory()
    {
        string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return path;
    }

    public void ForceSaveElement(ElementSave element)
    {
        SaveElement(element);
    }

    private void SaveElement(ElementSave elementSave)
    {
        if (elementSave.IsSourceFileMissing)
        {
            _dialogService.ShowMessage("Cannot save " + elementSave + " because its source file is missing");
        }
        else
        {
            bool succeeded = true;

            // April 10, 2023
            // This is potentially slow, dishonest (SaveElement should only save the element), and prevents multi-select edit
            // from properly creating a single undo state:
            //UndoManager.Self.RecordUndo();

            bool doesProjectNeedToSave = false;
            bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

            if (doesProjectNeedToSave)
            {
                _projectManager.SaveProject();
            }

            if (shouldSave)
            {
                PluginManager.Self.BeforeSavingElementSave(elementSave);

                var fileName = elementSave.GetFullPathXmlFile();

                // if it's readonly, let's warn the user
                bool isReadOnly = ProjectManager.IsFileReadOnly(fileName.FullPath);

                if (isReadOnly)
                {
                    ProjectManager.ShowReadOnlyDialog(fileName.FullPath);
                }
                else
                {
                    _fileWatchManager.IgnoreNextChangeUntil(fileName.FullPath);

                    const int maxNumberOfTries = 5;
                    const int msBetweenSaves = 100;
                    int numberOfTimesTried = 0;

                    succeeded = false;
                    Exception exception = null;

                    while (numberOfTimesTried < maxNumberOfTries)
                    {
                        try
                        {
                            bool useCompact = _projectState.GumProjectSave?.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
                            elementSave.Save(fileName.FullPath, useCompact);
                            succeeded = true;
                            break;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                            System.Threading.Thread.Sleep(msBetweenSaves);
                            numberOfTimesTried++;
                        }
                    }


                    if (succeeded == false)
                    {
                        _dialogService.ShowMessage("Unknown error trying to save the file\n\n" + fileName + "\n\n" + exception.ToString());
                        succeeded = false;
                    }
                }
                if (succeeded)
                {
                    _outputManager.AddOutput("Saved " + elementSave + " to " + fileName);
                    PluginManager.Self.AfterSavingElementSave(elementSave);
                }
            }

            PluginManager.Self.Export(elementSave);
        }
    }

    public void LoadProject(string fileName)
    {
        _projectManager.LoadProject(fileName);
    }

    public FilePath GetFullFileName(ElementSave element)
    {
        return element.GetFullPathXmlFile();
    }


    public void LoadLocalizationFile()
    {
        _localizationService.Clear();

        // Design note (issue #2512): GumProjectSave.LocalizationFiles is a list. We pick a
        // policy here based on what the runtime LocalizationService supports:
        //   - ILocalizationService.AddDatabase REPLACES the internal database (it does not
        //     merge). So calling AddCsvDatabase / single-file AddResxDatabase back-to-back
        //     would clobber earlier loads — that rules out a naive "iterate and call
        //     single-file overloads" approach for multi-file cases.
        //   - The multi-file AddResxDatabase(IEnumerable<string>, onWarning) overload merges
        //     files in one pass and reports cross-file string-ID collisions via the callback.
        //
        // Policy: strictly homogeneous.
        //   * 0 files: load nothing, not an error.
        //   * 1 CSV: load via AddDatabaseFromCsv (no multi-CSV overload exists).
        //   * 1+ RESX: route through the multi-file AddResxDatabase overload. Missing files
        //     are reported via AddError and skipped; existing files still load. Collision
        //     warnings route to the Output tab.
        //   * Mixed CSV+RESX or multiple CSVs: rejected with an Output-tab error because
        //     the runtime has no merge API for those shapes today.
        var projectFiles = _projectState.GumProjectSave.LocalizationFiles;
        if (projectFiles == null || projectFiles.Count == 0)
        {
            _guiCommands.RefreshVariables();
            LocalizationLoaded?.Invoke();
            return;
        }

        var resolvedFiles = new System.Collections.Generic.List<FilePath>();
        foreach (var relative in projectFiles)
        {
            if (string.IsNullOrEmpty(relative))
            {
                continue;
            }
            resolvedFiles.Add(_projectState.ProjectDirectory + relative);
        }

        if (resolvedFiles.Count == 0)
        {
            _guiCommands.RefreshVariables();
            LocalizationLoaded?.Invoke();
            return;
        }

        try
        {
            // Single-CSV stays on its own path (no multi-CSV overload exists by design).
            // All RESX cases (1 or many) flow through the multi-file overload for consistent
            // missing-file reporting and collision warnings.
            if (resolvedFiles.Count == 1 && resolvedFiles[0].Extension != "resx")
            {
                FilePath file = resolvedFiles[0];
                if (file.Exists())
                {
                    _localizationService.AddDatabaseFromCsv(file.FullPath, ',');
                }
                else
                {
                    _outputManager.AddError(
                        $"Localization: file not found, skipping: {file.FullPath}");
                }
            }
            else
            {
                // All-RESX path (1 or more files): require every entry to be .resx.
                var allResx = true;
                foreach (var file in resolvedFiles)
                {
                    if (file.Extension != "resx")
                    {
                        allResx = false;
                        break;
                    }
                }

                if (!allResx)
                {
                    _outputManager.AddError(
                        "Localization: multiple files configured but not all are .resx. " +
                        "Mixed CSV/RESX and multi-CSV loading are not supported. " +
                        "Loading was skipped.");
                }
                else
                {
                    var existingPaths = new System.Collections.Generic.List<string>();
                    foreach (var file in resolvedFiles)
                    {
                        if (file.Exists())
                        {
                            existingPaths.Add(file.FullPath);
                        }
                        else
                        {
                            _outputManager.AddError(
                                $"Localization: file not found, skipping: {file.FullPath}");
                        }
                    }

                    if (existingPaths.Count > 0)
                    {
                        _localizationService.AddResxDatabase(
                            existingPaths,
                            onWarning: message => _outputManager.AddOutput("Localization warning: " + message));
                    }
                }
            }

            _localizationService.CurrentLanguage = _projectState.GumProjectSave.CurrentLanguageIndex;
        }
        catch (Exception e)
        {
            var joined = string.Join(", ", resolvedFiles);
            _dialogService.ShowMessage($"Error loading localization file(s) {joined}\n\n{e}");
        }

        _guiCommands.RefreshVariables();
        LocalizationLoaded?.Invoke();
    }

    public event Action? LocalizationLoaded;

    private void ForceSaveBehavior(BehaviorSave behavior)
    {
        if (behavior.IsSourceFileMissing)
        {
            _dialogService.ShowMessage("Cannot save " + behavior + " because its source file is missing");
        }
        else
        {
            bool succeeded = true;

            _undoManager.Value.RecordUndo();

            bool doesProjectNeedToSave = false;
            bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

            if (doesProjectNeedToSave)
            {
                _projectManager.SaveProject();
            }

            if (shouldSave)
            {
                //PluginManager.Self.BeforeBehaviorSave(behavior);

                string fileName = GetFullPathXmlFile( behavior).FullPath;
                _fileWatchManager.IgnoreNextChangeUntil(fileName);
                // if it's readonly, let's warn the user
                bool isReadOnly = ProjectManager.IsFileReadOnly(fileName);

                if (isReadOnly)
                {
                    ProjectManager.ShowReadOnlyDialog(fileName);
                }
                else
                {
                    const int maxNumberOfTries = 5;
                    const int msBetweenSaves = 100;
                    int numberOfTimesTried = 0;

                    succeeded = false;
                    Exception exception = null;

                    while (numberOfTimesTried < maxNumberOfTries)
                    {
                        try
                        {
                            bool useCompact = _projectState.GumProjectSave?.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
                            behavior.Save(fileName, useCompact);

                            succeeded = true;
                            break;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                            System.Threading.Thread.Sleep(msBetweenSaves);
                            numberOfTimesTried++;
                        }
                    }


                    if (succeeded == false)
                    {
                        _dialogService.ShowMessage("Unknown error trying to save the file\n\n" + fileName + "\n\n" + exception.ToString());
                        succeeded = false;
                    }
                }
                if (succeeded)
                {
                    _outputManager.AddOutput("Saved " + behavior + " to " + fileName);
                    //PluginManager.Self.AfterBehaviorSave(behavior);
                }
            }

            //PluginManager.Self.Export(elementSave);
        }

    }

    public FilePath GetFullPathXmlFile(BehaviorSave behaviorSave)
    {
        return GetFullPathXmlFile(behaviorSave, behaviorSave.Name);
    }

    string GetFullPathXmlFile(BehaviorSave behaviorSave, string behaviorName)
    {
        if (string.IsNullOrEmpty(_projectManager.GumProjectSave.FullFileName))
        {
            return null;
        }

        string directory = FileManager.GetDirectory(_projectManager.GumProjectSave.FullFileName);

        return directory + BehaviorReference.Subfolder + "\\" + behaviorName + "." + BehaviorReference.Extension;
    }

    public void SaveGeneralSettings()
    {
        var settings = _projectManager.GeneralSettingsFile;
        settings.Save();
    }

    public void SaveIfDiffers(FilePath filePath, string contents)
    {
        if (filePath.Exists() == false)
        {
            FileManager.SaveText(contents, filePath.FullPath);
        }
        else
        {
            string existingContents = FileManager.FromFileText(filePath.FullPath);
            if (existingContents != contents)
            {
                FileManager.SaveText(contents, filePath.FullPath);
            }
        }
    }
}
