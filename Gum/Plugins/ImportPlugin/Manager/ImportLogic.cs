using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using Gum.Commands;
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
    private readonly StandardElementsManagerGumTool _standardElementsManagerGumTool;

    public ImportLogic(ISelectedState selectedState, IGuiCommands guiCommands, IFileCommands fileCommands, IDialogService dialogService, IProjectManager projectManager, StandardElementsManagerGumTool standardElementsManagerGumTool)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
        _projectManager = projectManager;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
    }

    public ScreenSave? ImportScreen(FilePath filePath, string desiredDirectory = null, bool saveProject = true)
    {
        string screensOrComponents = "Screens";
        var elementReferences = _projectManager.GumProjectSave.ScreenReferences;

        bool shouldAdd = DetermineIfShouldAdd(ref filePath, ref desiredDirectory, screensOrComponents);

        ScreenSave? toReturn = null;
        ScreenSave? screenSave = null;
        if (shouldAdd)
        {
            screenSave = FileManager.XmlDeserialize<ScreenSave>(filePath.FullPath);

            var isDuplicate = ObjectFinder.Self.GetElementSave(screenSave.Name) != null;

            if (isDuplicate)
            {
                _dialogService.ShowMessage($"This project already a screen named {screenSave.Name} in this project");
                shouldAdd = false;
            }
        }

        if(shouldAdd)
        { 
            elementReferences.Add(new ElementReference { Name = screenSave!.Name, ElementType = ElementType.Screen });
            elementReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            var screens = _projectManager.GumProjectSave.Screens;
            screens.Add(screenSave);
            screens.Sort((first, second) => first.Name.CompareTo(second.Name));

            screenSave.Initialize(null);

            DoAfterImportLogic(saveProject, screenSave);

            toReturn = screenSave;
        }

        return toReturn;
    }


    public ComponentSave? ImportComponent(FilePath filePath, string desiredDirectory = null, bool saveProject = true)
    {
        string screensOrComponents = "Components";
        var elementReferences = _projectManager.GumProjectSave.ComponentReferences;

        bool shouldAdd = DetermineIfShouldAdd(ref filePath, ref desiredDirectory, screensOrComponents);

        ComponentSave? toReturn = null;
        ComponentSave? componentSave = null;
        if (shouldAdd)
        {
            componentSave = FileManager.XmlDeserialize<ComponentSave>(filePath.FullPath);

            var isDuplicate = ObjectFinder.Self.GetElementSave(componentSave.Name) != null;

            if(isDuplicate)
            {
                _dialogService.ShowMessage($"This project already a component named {componentSave.Name} in this project");
                shouldAdd = false;
            }
        }

        if(shouldAdd)
        {
            elementReferences.Add(new ElementReference { Name = componentSave!.Name, ElementType = ElementType.Component });
            elementReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            var components = _projectManager.GumProjectSave.Components;
            components.Add(componentSave);
            components.Sort((first, second) => first.Name.CompareTo(second.Name));

            componentSave.InitializeDefaultAndComponentVariables();
            _standardElementsManagerGumTool.FixCustomTypeConverters(componentSave);

            DoAfterImportLogic(saveProject, componentSave);

            toReturn = componentSave;
        }

        return toReturn;
    }

    private void DoAfterImportLogic(bool saveProject, ElementSave screenSave)
    {
        _standardElementsManagerGumTool.FixCustomTypeConverters(screenSave);

        if (saveProject)
        {
            _guiCommands.RefreshElementTreeView();
            _selectedState.SelectedElement = screenSave;
            _fileCommands.TryAutoSaveProject();
        }
        _fileCommands.TryAutoSaveElement(screenSave);
    }

    private bool DetermineIfShouldAdd(ref FilePath filePath, ref string desiredDirectory, string screensOrComponents)
    {
        var shouldAdd = true;
        desiredDirectory = desiredDirectory ?? FileManager.GetDirectory(
            _projectManager.GumProjectSave.FullFileName) + $"{screensOrComponents}/";

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


    public BehaviorSave ImportBehavior(FilePath filePath, string desiredDirectory = null, bool saveProject = false)
    {
        var shouldAdd = true;

        desiredDirectory = desiredDirectory ?? FileManager.GetDirectory(
            _projectManager.GumProjectSave.FullFileName) + "Behaviors/";

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

        BehaviorSave toReturn = null;

        if (shouldAdd)
        {
            string strippedName = filePath.RemoveExtension().FileNameNoPath;

            var behaviorSave = FileManager.XmlDeserialize<BehaviorSave>(filePath.FullPath);

            var behaviorReferences = _projectManager.GumProjectSave.BehaviorReferences;
            behaviorReferences.Add(new BehaviorReference { Name = behaviorSave.Name });
            behaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            var behaviors = _projectManager.GumProjectSave.Behaviors;
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


