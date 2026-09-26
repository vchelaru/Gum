using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Diagnostics.CodeAnalysis;
using Gum.Commands;
using Gum.Plugins;
using Gum.Services.Dialogs;
using ToolsUtilities;
using System.Linq;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Plugins.ImportPlugin.Manager;

public class ImportLogic : IImportLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;
    private readonly IProjectManager _projectManager;
    private readonly IPluginManager _pluginManager;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IScreenImportService _screenImportService;

    public ImportLogic(ISelectedState selectedState, IGuiCommands guiCommands, IFileCommands fileCommands, IDialogService dialogService, IProjectManager projectManager, IPluginManager pluginManager, IStandardElementsManagerGumTool standardElementsManagerGumTool, IScreenImportService screenImportService)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
        _projectManager = projectManager;
        _pluginManager = pluginManager;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _screenImportService = screenImportService;
    }

    public ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
    {
        bool shouldAdd = DetermineIfShouldAdd(ref filePath, ref desiredDirectory, "Screens");
        if (!shouldAdd) { return null; }

        var screenSave = ElementReference.DeserializeElement<ScreenSave>(filePath.FullPath, GumProjectSave.NativeVersion);
        return ImportScreen(screenSave, saveProject);
    }

    public ScreenSave? ImportScreen(ScreenSave screenSave, bool saveProject = true)
    {
        if (_projectManager.GumProjectSave is not { } project)
        {
            return null;
        }

        var result = _screenImportService.ImportScreen(project, screenSave);
        if (!result.Success)
        {
            _dialogService.ShowMessage($"This project already a screen named {result.ConflictingScreenName} in this project");
            return null;
        }

        DoAfterImportLogic(saveProject, result.ImportedScreen!);
        return result.ImportedScreen;
    }

    public ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
    {
        bool shouldAdd = DetermineIfShouldAdd(ref filePath, ref desiredDirectory, "Components");
        if (!shouldAdd) { return null; }

        var componentSave = ElementReference.DeserializeElement<ComponentSave>(filePath.FullPath, GumProjectSave.NativeVersion);
        return ImportComponent(componentSave, saveProject);
    }

    public ComponentSave? ImportComponent(ComponentSave componentSave, bool saveProject = true)
    {
        if (ObjectFinder.Self.GetElementSave(componentSave.Name) != null)
        {
            _dialogService.ShowMessage($"This project already contains a component named {componentSave.Name}");
            return null;
        }

        if (_projectManager.GumProjectSave is not { } project)
        {
            return null;
        }

        var elementReferences = project.ComponentReferences;
        elementReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
        elementReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

        var components = project.Components;
        components.Add(componentSave);
        components.Sort((first, second) => first.Name.CompareTo(second.Name));

        componentSave.InitializeDefaultAndComponentVariables();
        _standardElementsManagerGumTool.FixCustomTypeConverters(componentSave);

        DoAfterImportLogic(saveProject, componentSave);
        return componentSave;
    }

    private void DoAfterImportLogic(bool saveProject, ElementSave elementSave)
    {
        _standardElementsManagerGumTool.FixCustomTypeConverters(elementSave);

        if (saveProject)
        {
            _selectedState.SelectedElement = elementSave;
            _fileCommands.TryAutoSaveProject();
        }
        _fileCommands.TryAutoSaveElement(elementSave);
        _pluginManager.ElementImported(elementSave);
    }

    private bool DetermineIfShouldAdd(ref FilePath filePath, ref string? desiredDirectory, string screensOrComponents)
    {
        var shouldAdd = true;
        if (desiredDirectory == null)
        {
            if (!TryGetProjectDirectory(out string? projectDirectory))
            {
                return false;
            }
            desiredDirectory = projectDirectory + $"{screensOrComponents}/";
        }

        if (!FileManager.IsRelativeTo(filePath.FullPath, desiredDirectory))
        {
            string fileNameWithoutPath = FileManager.RemovePath(filePath.FullPath);

            var copyResult = _dialogService.ShowYesNoMessage("The file " + fileNameWithoutPath + $" must be in the Gum project's {screensOrComponents} folder.  " +
                "Would you like to copy the file?.", "Copy?");

            shouldAdd = copyResult;

            if (shouldAdd)
            {
                try
                {
                    string destination = desiredDirectory + fileNameWithoutPath;
                    System.IO.File.Copy(filePath.FullPath, destination);

                    filePath = destination;
                }
                catch (Exception ex)
                {
                    _dialogService.ShowMessage("Error copying the file: " + ex);
                    shouldAdd = false;
                }
            }
        }

        return shouldAdd;
    }


    /// <summary>
    /// Gets the loaded project's directory, or shows a message and returns false when the project
    /// has never been saved, since there is no folder to import into.
    /// </summary>
    private bool TryGetProjectDirectory([NotNullWhen(true)] out string? projectDirectory)
    {
        string? projectFileName = _projectManager.GumProjectSave?.FullFileName;
        if (string.IsNullOrEmpty(projectFileName))
        {
            _dialogService.ShowMessage("You must first save the project before importing");
            projectDirectory = null;
            return false;
        }

        projectDirectory = FileManager.GetDirectory(projectFileName);
        return true;
    }

    public BehaviorSave? ImportBehavior(FilePath filePath, string? desiredDirectory = null, bool saveProject = false)
    {
        var shouldAdd = true;

        if (desiredDirectory == null)
        {
            if (!TryGetProjectDirectory(out string? projectDirectory))
            {
                return null;
            }
            desiredDirectory = projectDirectory + "Behaviors/";
        }

        if (!FileManager.IsRelativeTo(filePath.FullPath, desiredDirectory))
        {
            string fileNameWithoutPath = filePath.FileNameNoPath;

            var copyResult = _dialogService.ShowYesNoMessage("The file " + fileNameWithoutPath + " must be in the Gum project's Behaviors folder. " +
                "Would you like to copy the file?", "Copy?");

            shouldAdd = copyResult;

            if (shouldAdd)
            {
                try
                {
                    string destination = desiredDirectory + fileNameWithoutPath;
                    System.IO.File.Copy(filePath.FullPath, destination);

                    filePath = destination;
                }
                catch (Exception ex)
                {
                    _guiCommands.PrintOutput("Error copying file: " + ex);
                    shouldAdd = false;
                }
            }
        }

        BehaviorSave? toReturn = null;

        if (shouldAdd && _projectManager.GumProjectSave is { } project)
        {
            string strippedName = filePath.RemoveExtension().FileNameNoPath;

            var (_, isCompact) = GumFileSerializer.ReadAndDetectFormat(filePath.FullPath);
            int behaviorVersion = isCompact
                ? (int)GumProjectSave.GumxVersions.AttributeVersion
                : (int)GumProjectSave.GumxVersions.InitialVersion;
            var behaviorSave = BehaviorReference.DeserializeBehavior(filePath.FullPath, behaviorVersion);

            var behaviorReferences = project.BehaviorReferences;
            behaviorReferences.Add(new BehaviorReference { Name = behaviorSave.Name });
            behaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            var behaviors = project.Behaviors;
            behaviors.Add(behaviorSave);
            behaviors.Sort((first, second) => first.Name.CompareTo(second.Name));

            behaviorSave.Initialize();

            if(saveProject)
            {
                _guiCommands.RefreshElementTreeView();
                _selectedState.SelectedBehavior = behaviorSave;
                _fileCommands.TryAutoSaveProject();
            }

            _fileCommands.TryAutoSaveBehavior(behaviorSave);

            toReturn = behaviorSave;
        }

        return toReturn;
    }
}

