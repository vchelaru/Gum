using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Localization;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Commands;

public class FileCommands : IFileCommands
{
    private readonly ILocalizationService _localizationService;
    private readonly IFileWatchIgnoreList _fileWatchIgnoreList;
    private readonly ISelectedState _selectedState;
    private readonly Lazy<IUndoManager> _undoManager;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IOutputManager _outputManager;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;
    private readonly IPluginManager _pluginManager;
    private readonly IRecycleBinService _recycleBinService;
    private readonly IPathCaseSensitivity _pathCaseSensitivity;
    private readonly IUnsavedChangesTracker _unsavedChangesTracker;
    // Lazy: NewProjectLogic saves through IFileCommands, so a direct reference would be a
    // construction cycle.
    private readonly Lazy<INewProjectLogic> _newProjectLogicLazy;

    private INewProjectLogic _newProjectLogic => _newProjectLogicLazy.Value;

    public FileCommands(ISelectedState selectedState,
        Lazy<IUndoManager> undoManager,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        ILocalizationService localizationService,
        IOutputManager outputManager,
        IFileWatchIgnoreList fileWatchIgnoreList,
        IProjectManager projectManager,
        IProjectState projectState,
        IPluginManager pluginManager,
        IRecycleBinService recycleBinService,
        Lazy<INewProjectLogic> newProjectLogic,
        IPathCaseSensitivity pathCaseSensitivity,
        IUnsavedChangesTracker unsavedChangesTracker)
    {
        _unsavedChangesTracker = unsavedChangesTracker;
        _newProjectLogicLazy = newProjectLogic;
        _pathCaseSensitivity = pathCaseSensitivity;
        _selectedState = selectedState;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _localizationService = localizationService;
        _fileWatchIgnoreList = fileWatchIgnoreList;
        _outputManager = outputManager;
        _projectManager = projectManager;
        _projectState = projectState;
        _pluginManager = pluginManager;
        _recycleBinService = recycleBinService;

    }

    /// <summary>
    /// Saves the current Behavior or Element
    /// </summary>

    public FilePath ProjectDirectory => FileManager.RelativeDirectory;

    public void DeleteDirectory(FilePath directory) =>
        FileManager.DeleteDirectory(directory.FullPath);

    public void ClearDirectoryContents(FilePath directory) =>
        FileManager.ClearDirectoryContents(directory.FullPath);

    public void MoveToRecycleBin(FilePath filePath) =>
        MoveToRecycleBin(new[] { filePath }, () => _recycleBinService.MoveToRecycleBin(filePath));

    public void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths) =>
        MoveToRecycleBin(filePaths, () => _recycleBinService.MoveToRecycleBin(filePaths));

    // A head's trash can fail with any exception type (e.g. Win32Exception when Linux has no gio), so
    // failures are reported here and surfaced as IOException, the one type callers handle.
    private void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths, Action moveToRecycleBin)
    {
        try
        {
            moveToRecycleBin();
        }
        catch (Exception exception)
        {
            string files = string.Join(", ", filePaths.Select(filePath => filePath.FullPath));
            _outputManager.AddError($"Could not move to the recycle bin: {files}\n{exception.Message}");
            if (exception is IOException)
            {
                throw;
            }
            throw new IOException(exception.Message, exception);
        }
    }

    public string[] GetFiles(string path) => System.IO.Directory.GetFiles(path);

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) =>
        System.IO.Directory.GetFiles(path, searchPattern, searchOption);

    public string ReadAllText(string path) => System.IO.File.ReadAllText(path);

    public void MoveDirectory(string source, string destination)
    {
        // A rename that only changes casing (e.g. "GameMenuScreens" -> "gamemenuscreens") points
        // source and destination at the same physical directory on a case-insensitive filesystem
        // (Windows/macOS). The general merge-into-destination logic below no-ops the "create" and
        // "move each file into itself" steps, then throws on the final Directory.Delete(source)
        // because the directory is still non-empty. Directory.Move handles this case correctly.
        // On a case-sensitive file system the two are separate folders and merge like any other.
        bool isSameDirectoryDifferentCase =
            string.Equals(NormalizeDirectoryPath(source), NormalizeDirectoryPath(destination), _pathCaseSensitivity.GetComparison(source));

        if (isSameDirectoryDifferentCase)
        {
            Directory.Move(source, destination);
            return;
        }

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

    private static string NormalizeDirectoryPath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

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
        if (elementSave == null || _projectManager.IsNotifyingProjectLoad)
        {
            return;
        }
        // A successful save clears the mark; a failed one (read-only file, missing source) keeps it.
        _unsavedChangesTracker.MarkUnsaved(elementSave);
        if (_projectManager.AutoSave)
        {
            SaveElement(elementSave);
        }
    }

    public void TryAutoSaveBehavior(BehaviorSave behavior)
    {
        if (behavior == null || _projectManager.IsNotifyingProjectLoad)
        {
            return;
        }
        // As for elements, a successful save clears the mark.
        _unsavedChangesTracker.MarkUnsaved(behavior);
        if (_projectManager.AutoSave)
        {
            _undoManager.Value.RecordUndo();
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


    public async Task NewProjectAsync()
    {
        _selectedState.SelectedElement = null;
        _selectedState.SelectedInstance = null;
        _selectedState.SelectedBehavior = null;
        _selectedState.SelectedStateCategorySave = null;
        _selectedState.SelectedStateSave = null;

        await _newProjectLogic.CreateNewProjectAsync();

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
        // What a plugin fills in while the project opens is written by the next real save (#5412).
        if (_projectManager.IsNotifyingProjectLoad)
        {
            return false;
        }
        // A successful save clears the mark (ProjectManager.SaveProject).
        if (_projectState.GumProjectSave is { } project)
        {
            _unsavedChangesTracker.MarkUnsaved(project);
        }
        if (_projectManager.AutoSave && !_projectManager.HaveErrorsOccurredLoadingProject)
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

        if (string.IsNullOrEmpty(_projectState.GumProjectSave?.FullFileName))
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

        if (forceSaveContainedElements)
        {
            // The project save writes no behaviors, so with Auto Save off a behavior edit would
            // otherwise never reach disk (#5387). Only here, where the user saves everything: the
            // re-save a project gets on load must not rewrite behavior files other projects share.
            // A linked behavior's save re-saves the project, so iterate a copy.
            foreach (BehaviorSave behavior in _projectState.GumProjectSave.Behaviors.ToList())
            {
                if (behavior != null && !behavior.IsSourceFileMissing)
                {
                    ForceSaveBehavior(behavior);
                }
            }
        }

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
        // A project that was never saved has no folder to put the file in.
        if (ObjectFinder.Self.GumProjectSave?.FullFileName is not { } projectFileName)
            return;

        var sourceFile = System.IO.Path.Combine(GetExecutingDirectory(), "Content", ".gumfcs");
        var destinationFile = FileManager.GetDirectory(projectFileName) + ".gumfcs";

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
        // Assembly.GetExecutingAssembly().Location returns empty string in single-file published apps.
        return AppContext.BaseDirectory;
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

            bool doesProjectNeedToSave = false;
            bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

            if (doesProjectNeedToSave)
            {
                _projectManager.SaveProject();
            }

            if (shouldSave)
            {
                _pluginManager.BeforeSavingElementSave(elementSave);
                // As a project save does, so an auto-save writes the bytes Save All would.
                ElementCommands.SortStateVariables(elementSave);

                // shouldSave means the project has a file name, and so a path for the element.
                var fileName = GetFullPathXmlFileForElement(elementSave, elementSave.Name)!;

                // if it's readonly, let's warn the user
                bool isReadOnly = IsFileReadOnly(fileName.FullPath);

                if (isReadOnly)
                {
                    _projectManager.ShowReadOnlyDialog(fileName.FullPath);
                    succeeded = false;
                }
                else
                {
                    _fileWatchIgnoreList.IgnoreNextChangeUntil(fileName.FullPath);

                    const int maxNumberOfTries = 5;
                    const int msBetweenSaves = 100;
                    int numberOfTimesTried = 0;

                    succeeded = false;
                    Exception? exception = null;

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
                        _dialogService.ShowMessage("Unknown error trying to save the file\n\n" + fileName + "\n\n" + exception?.ToString());
                        succeeded = false;
                    }
                }
                if (succeeded)
                {
                    _outputManager.AddOutput("Saved " + elementSave + " to " + fileName);
                    _unsavedChangesTracker.MarkSaved(elementSave);
                    _pluginManager.AfterSavingElementSave(elementSave);
                }
            }

            _pluginManager.Export(elementSave);
        }
    }

    public Task LoadProjectAsync(string fileName)
    {
        return _projectManager.LoadProjectAsync(fileName);
    }

    public FilePath? GetFullFileName(ElementSave element)
    {
        return GetFullPathXmlFileForElement(element, element.Name);
    }

    public FilePath? GetFullPathXmlFile(ElementSave element, string elementName)
    {
        return GetFullPathXmlFileForElement(element, elementName);
    }

    /// <summary>
    /// Computes the full path to <paramref name="elementSave"/>'s file, using
    /// <paramref name="elementSaveName"/> for the file name and Subfolder path. Inlined from the
    /// (Locator-based) <c>ElementSave.GetFullPathXmlFile(string)</c> extension method - that method
    /// resolves <see cref="IProjectManager"/> via <c>Locator</c>, which is a WinForms-tool-only
    /// static and can't be referenced from this headless assembly, so this uses the already-injected
    /// <see cref="_projectManager"/> instead. Behavior is identical. Mirrors the private duplicate in
    /// <c>DragDropManager</c>.
    /// <para>
    /// The extension follows the open project's own format, so a .gumj project resolves
    /// .gusj/.gucj/.gutj. Hardcoding the XML extension here silently wrote element edits to a file
    /// the project never loads back (issue #4595).
    /// </para>
    /// </summary>
    private FilePath? GetFullPathXmlFileForElement(ElementSave elementSave, string elementSaveName)
    {
        var gumProject = _projectManager.GumProjectSave;
        if (string.IsNullOrEmpty(gumProject?.FullFileName))
        {
            return null;
        }

        var extension = elementSave.GetFileExtension(GumProjectSave.IsJsonFormat(gumProject.FullFileName));

        var reference =
            gumProject.ScreenReferences.FirstOrDefault(item => item.Name == elementSave.Name) ??
            gumProject.ComponentReferences.FirstOrDefault(item => item.Name == elementSave.Name) ??
            gumProject.StandardElementReferences.FirstOrDefault(item => item.Name == elementSave.Name);

        FilePath gumDirectory = FileManager.GetDirectory(gumProject.FullFileName);
        if (!string.IsNullOrWhiteSpace(reference?.Link))
        {
            return gumDirectory.Original + reference.Link;
        }
        else
        {
            return gumDirectory.Original + elementSave.Subfolder + "\\" + elementSaveName + "." + extension;
        }
    }

    /// <summary>
    /// Inlined from the static <c>ProjectManager.IsFileReadOnly</c> helper - a pure, stateless
    /// check that doesn't need DI, so it's duplicated here rather than referencing the concrete
    /// (still tool-side) <c>ProjectManager</c> class from this headless assembly.
    /// </summary>
    private static bool IsFileReadOnly(string fileName) =>
        File.Exists(fileName) && new FileInfo(fileName).IsReadOnly;


    public void LoadLocalizationFile()
    {
        _localizationService.Clear();

        var gumProject = _projectState.GumProjectSave;
        string? projectDirectory = _projectState.ProjectDirectory;
        if (gumProject != null && projectDirectory != null
            && gumProject.LocalizationFiles.Any(file => !string.IsNullOrEmpty(file)))
        {
            try
            {
                // The policy and parsers are shared with gumcli and the runtime; skipped files are
                // errors in the Output tab and string ID collisions are output.
                ProjectLocalizationLoader.Load(gumProject, projectDirectory, _localizationService,
                    new ProjectLocalizationLoadOptions
                    {
                        OnSkipped = _outputManager.AddError,
                        OnWarning = _outputManager.AddOutput,
                    });

                _localizationService.CurrentLanguage = gumProject.CurrentLanguageIndex;
            }
            catch (Exception e)
            {
                var joined = string.Join(", ", gumProject.LocalizationFiles);
                _dialogService.ShowMessage($"Error loading localization file(s) {joined}\n\n{e}");
            }
        }

        // Forced: the database decides whether Text rows are a text box or a combo of string
        // IDs, and an unforced refresh keeps the rows of an unchanged selection.
        _guiCommands.RefreshVariables(force: true);
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

            bool doesProjectNeedToSave = false;
            bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

            if (doesProjectNeedToSave)
            {
                _projectManager.SaveProject();
            }

            if (shouldSave)
            {
                //PluginManager.Self.BeforeBehaviorSave(behavior);

                // shouldSave means the project has a file name, and so a path for the behavior.
                GumProjectSave project = _projectManager.GetLoadedProject();
                string fileName = GetFullPathXmlFile( behavior)!.FullPath;

                // A SourcePath-linked behavior's DefaultImplementation is per-project (each theme
                // has its own default visual) even though the rest of the behavior is shared.
                // Never let one project's save write its DefaultImplementation into the shared
                // file - capture it as a reference-level override instead, and restore whatever
                // DefaultImplementation is already on disk before writing.
                var matchingReference = project.BehaviorReferences
                    ?.FirstOrDefault(item => item.Name == behavior.Name);
                var linkedReference = !string.IsNullOrEmpty(matchingReference?.SourcePath) ? matchingReference : null;
                bool isLinkedBehavior = linkedReference != null;
                string? userIntendedDefaultImplementation = behavior.DefaultImplementation;

                if (linkedReference != null)
                {
                    linkedReference.DefaultImplementationOverride = userIntendedDefaultImplementation;

                    string? sharedDefaultImplementationOnDisk = null;
                    if (FileManager.FileExists(fileName))
                    {
                        sharedDefaultImplementationOnDisk = BehaviorReference
                            .DeserializeBehavior(fileName, project.Version)
                            .DefaultImplementation;
                    }
                    behavior.DefaultImplementation = sharedDefaultImplementationOnDisk;
                }

                _fileWatchIgnoreList.IgnoreNextChangeUntil(fileName);
                // if it's readonly, let's warn the user
                bool isReadOnly = IsFileReadOnly(fileName);

                if (isReadOnly)
                {
                    _projectManager.ShowReadOnlyDialog(fileName);
                }
                else
                {
                    const int maxNumberOfTries = 5;
                    const int msBetweenSaves = 100;
                    int numberOfTimesTried = 0;

                    succeeded = false;
                    Exception? exception = null;

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
                        _dialogService.ShowMessage("Unknown error trying to save the file\n\n" + fileName + "\n\n" + exception?.ToString());
                        succeeded = false;
                    }
                }

                if (isLinkedBehavior)
                {
                    behavior.DefaultImplementation = userIntendedDefaultImplementation;
                }

                if (succeeded)
                {
                    _outputManager.AddOutput("Saved " + behavior + " to " + fileName);
                    _unsavedChangesTracker.MarkSaved(behavior);

                    if (isLinkedBehavior)
                    {
                        _projectManager.SaveProject();
                    }
                }
            }

            //PluginManager.Self.Export(elementSave);
        }

    }

    public FilePath? GetFullPathXmlFile(BehaviorSave behaviorSave)
    {
        return GetFullPathXmlFile(behaviorSave, behaviorSave.Name) is { } path ? new FilePath(path) : null;
    }

    string? GetFullPathXmlFile(BehaviorSave behaviorSave, string behaviorName)
    {
        var gumProject = _projectManager.GumProjectSave;
        if (gumProject == null || string.IsNullOrEmpty(gumProject.FullFileName))
        {
            return null;
        }

        string directory = FileManager.GetDirectory(gumProject.FullFileName);

        var matchingReference = gumProject.BehaviorReferences
            ?.FirstOrDefault(item => item.Name == behaviorName);

        // Same project-format routing as the element overload above (issue #4595): a .gumj project
        // stores behaviors as .behj, and saving to .behx would write content it never loads back.
        bool isJsonFormat = GumProjectSave.IsJsonFormat(gumProject.FullFileName);

        string relativeFilePath = matchingReference != null
            ? matchingReference.GetRelativeFilePath(isJsonFormat)
            : BehaviorReference.Subfolder + "\\" + behaviorName + "." +
                (isJsonFormat ? BehaviorReference.JsonExtension : BehaviorReference.Extension);

        return directory + relativeFilePath;
    }

    public void SaveGeneralSettings()
    {
        _projectManager.SaveGeneralSettings();
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
