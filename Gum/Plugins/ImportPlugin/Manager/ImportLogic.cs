using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using Gum.Commands;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.Manager;

public static class ImportLogic
{
    private static readonly ISelectedState _selectedState = Locator.GetRequiredService<ISelectedState>();
    private static readonly IGuiCommands _guiCommands = Locator.GetRequiredService<IGuiCommands>();
    private static readonly IFileCommands _fileCommands = Locator.GetRequiredService<IFileCommands>();
    private static readonly IDialogService _dialogService = Locator.GetRequiredService<IDialogService>();

    public static void ImportScreen(FilePath fileName, string desiredDirectory = null, bool saveProject = true)
    {
        desiredDirectory = desiredDirectory ?? 
            FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName) + "Screens/";

        var shouldAdd = true;


        if (FileManager.IsRelativeTo(fileName.FullPath, desiredDirectory) == false)
        {
            var copyResult = _dialogService.ShowYesNoMessage("The file must be in the Gum project's Screens folder.  " +
                "Would you like to copy the file?.", "Copy?");

            shouldAdd = copyResult;

            try
            {
                string destination = desiredDirectory + FileManager.RemovePath(fileName.FullPath);
                System.IO.File.Copy(fileName.FullPath,
                    destination);

                fileName = destination;
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage("Error copying the file: " + ex.ToString());
            }
        }

        if (shouldAdd)
        {
            string strippedName = FileManager.RemovePath(FileManager.RemoveExtension(fileName.FullPath));

            ProjectManager.Self.GumProjectSave.ScreenReferences.Add(
                new ElementReference { Name = strippedName, ElementType = ElementType.Screen });

            ProjectManager.Self.GumProjectSave.ScreenReferences.Sort(
                (first, second) => first.Name.CompareTo(second.Name));

            var screenSave = FileManager.XmlDeserialize<ScreenSave>(fileName.FullPath);

            ProjectManager.Self.GumProjectSave.Screens.Add(screenSave);

            ProjectManager.Self.GumProjectSave.Screens.Sort(
                (first, second) => first.Name.CompareTo(second.Name));

            screenSave.Initialize(null);

            StandardElementsManagerGumTool.Self.FixCustomTypeConverters(screenSave);
            if(saveProject)
            {
                _guiCommands.RefreshElementTreeView();
                _selectedState.SelectedScreen = screenSave;
                _fileCommands.TryAutoSaveProject();
            }
            _fileCommands.TryAutoSaveElement(screenSave);
        }
    }

    public static ComponentSave ImportComponent(FilePath filePath, string desiredDirectory = null, bool saveProject = true)
    {
        var shouldAdd = true;

        desiredDirectory = desiredDirectory ?? FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Components/";

        if (!FileManager.IsRelativeTo(filePath.FullPath, desiredDirectory))
        {
            string fileNameWithoutPath = FileManager.RemovePath(filePath.FullPath);

            var copyResult = _dialogService.ShowYesNoMessage("The file " + fileNameWithoutPath + " must be in the Gum project's Components folder. " +
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

        ComponentSave toReturn = null;

        if (shouldAdd)
        {
            string strippedName = filePath.RemoveExtension().FileNameNoPath;

            var componentSave = FileManager.XmlDeserialize<ComponentSave>(filePath.FullPath);

            var componentReferences = ProjectManager.Self.GumProjectSave.ComponentReferences;
            componentReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
            componentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            var components = ProjectManager.Self.GumProjectSave.Components;
            components.Add(componentSave);
            components.Sort((first, second) => first.Name.CompareTo(second.Name));

            componentSave.InitializeDefaultAndComponentVariables();
            StandardElementsManagerGumTool.Self.FixCustomTypeConverters(componentSave);


            if(saveProject)
            {
                _guiCommands.RefreshElementTreeView();
                _selectedState.SelectedComponent = componentSave;
                _fileCommands.TryAutoSaveProject();
            }
            _fileCommands.TryAutoSaveElement(componentSave);

            toReturn = componentSave;
        }

        return toReturn;
    }

    public static BehaviorSave ImportBehavior(FilePath filePath, string desiredDirectory = null, bool saveProject = false)
    {
        var shouldAdd = true;

        desiredDirectory = desiredDirectory ?? FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Behaviors/";

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

            var behaviorReferences = ProjectManager.Self.GumProjectSave.BehaviorReferences;
            behaviorReferences.Add(new BehaviorReference { Name = behaviorSave.Name });
            behaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            var behaviors = ProjectManager.Self.GumProjectSave.Behaviors;
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


