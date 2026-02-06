using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
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
    private readonly LocalizationService _localizationManager;
    private readonly FileWatchManager _fileWatchManager;
    private readonly ISelectedState _selectedState;
    private readonly Lazy<IUndoManager> _undoManager;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IOutputManager _outputManager;

    public FileCommands(ISelectedState selectedState, 
        Lazy<IUndoManager> undoManager, 
        IDialogService dialogService,
        IGuiCommands guiCommands,
        LocalizationService localizationManager,
        IOutputManager outputManager,
        FileWatchManager fileWatchManager)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _localizationManager = localizationManager;
        _fileWatchManager = fileWatchManager;
        _outputManager = outputManager;
    }

    /// <summary>
    /// Saves the current Behavior or Element
    /// </summary>

    public FilePath? ProjectDirectory => FileManager.RelativeDirectory;

    public void DeleteDirectory(FilePath directory) => 
        FileManager.DeleteDirectory(directory.FullPath);

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
        if (ProjectManager.Self.GeneralSettingsFile.AutoSave && elementSave != null)
        {
            SaveElement(elementSave);
        }
    }

    public void TryAutoSaveBehavior(BehaviorSave behavior)
    {
        if(ProjectManager.Self.GeneralSettingsFile.AutoSave && behavior != null)
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

        ProjectManager.Self.CreateNewProject();

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
        if (ProjectManager.Self.GeneralSettingsFile.AutoSave && !ProjectManager.Self.HaveErrorsOccurredLoadingProject)
        {
            ForceSaveProject(forceSaveContainedElements);
            return true;
        }
        return false;
    }

    public void ForceSaveProject(bool forceSaveContainedElements = false)
    {
        if (ProjectManager.Self.HaveErrorsOccurredLoadingProject)
        {
            _dialogService.ShowMessage("Cannot save project because of earlier errors");
            return;
        }

        var succeeded = ProjectManager.Self.SaveProject(forceSaveContainedElements);

        if (string.IsNullOrEmpty(ProjectState.Self.GumProjectSave.FullFileName))
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

        _outputManager.AddOutput("Saved Gum project to " + ProjectState.Self.GumProjectSave.FullFileName);
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
            bool shouldSave = ProjectManager.Self.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

            if (doesProjectNeedToSave)
            {
                ProjectManager.Self.SaveProject();
            }

            if (shouldSave)
            {
                PluginManager.Self.BeforeElementSave(elementSave);

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
                            elementSave.Save(fileName.FullPath);
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
                    PluginManager.Self.AfterElementSave(elementSave);
                }
            }

            PluginManager.Self.Export(elementSave);
        }
    }

    public void LoadProject(string fileName)
    {
        ProjectManager.Self.LoadProject(fileName);
    }

    public FilePath GetFullFileName(ElementSave element)
    {
        return element.GetFullPathXmlFile();
    }


    public void LoadLocalizationFile()
    {
        _localizationManager.Clear();

        if (!string.IsNullOrEmpty(GumState.Self.ProjectState.GumProjectSave.LocalizationFile))
        {
            FilePath file = GumState.Self.ProjectState.ProjectDirectory + GumState.Self.ProjectState.GumProjectSave.LocalizationFile;

            if (file.Exists())
            {
                try
                {
                    _localizationManager.AddDatabaseFromCsv(file.FullPath, ',');
                    _localizationManager.CurrentLanguage = GumState.Self.ProjectState.GumProjectSave.CurrentLanguageIndex;
                }
                catch (Exception e)
                {
                    // This can happen if the CSV has duplicate entries
                    _dialogService.ShowMessage($"Error loading CSV {file.FullPath}\n\n{e}");
                }
            }
        }
    }

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
            bool shouldSave = ProjectManager.Self.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

            if (doesProjectNeedToSave)
            {
                ProjectManager.Self.SaveProject();
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
                            FileManager.XmlSerialize(behavior.GetType(), behavior, fileName);
                            
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

    static string GetFullPathXmlFile(BehaviorSave behaviorSave, string behaviorName)
    {
        if (string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
        {
            return null;
        }

        string directory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

        return directory + BehaviorReference.Subfolder + "\\" + behaviorName + "." + BehaviorReference.Extension;
    }

    public void SaveGeneralSettings()
    {
        var settings = ProjectManager.Self.GeneralSettingsFile;
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
