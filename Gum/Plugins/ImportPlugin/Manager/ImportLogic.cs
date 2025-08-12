using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.ImportPlugin.Views;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Gum.Commands;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.Manager;

public static class ImportLogic
{
    private static readonly ISelectedState _selectedState = Locator.GetRequiredService<ISelectedState>();
    private static readonly IGuiCommands _guiCommands = Locator.GetRequiredService<IGuiCommands>();
    private static readonly IFileCommands _fileCommands = Locator.GetRequiredService<IFileCommands>();
    
    #region Screen

    internal static void ShowImportScreenUi()
    {
        if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
        {
            MessageBox.Show("You must first save the project before adding a new screen");

            return;
        }

        ImportFileViewModel viewModel = ShowImportScreenView();

        for (int i = 0; i < viewModel.SelectedFiles.Count; ++i)
        {
            string fileName = viewModel.SelectedFiles[i];

            ImportScreen(fileName);

        }
    }

    public static void ImportScreen(FilePath fileName, string desiredDirectory = null, bool saveProject = true)
    {
        desiredDirectory = desiredDirectory ?? 
            FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName) + "Screens/";

        var shouldAdd = true;


        if (FileManager.IsRelativeTo(fileName.FullPath, desiredDirectory) == false)
        {
            var copyResult = MessageBox.Show("The file must be in the Gum project's Screens folder.  " +
                "Would you like to copy the file?.", "Copy?", MessageBoxButtons.YesNo);

            shouldAdd = copyResult == DialogResult.Yes;

            try
            {
                string destination = desiredDirectory + FileManager.RemovePath(fileName.FullPath);
                System.IO.File.Copy(fileName.FullPath,
                    destination);

                fileName = destination;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error copying the file: " + ex.ToString());
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

    private static ImportFileViewModel ShowImportScreenView()
    {
        var view = new ImportFileView();
        view.Title = "Import Screen";

        var viewModel = new ImportFileViewModel();

        viewModel.BrowseFileFilter = "Gum Screen (*.gusx)|*.gusx";


        var screenFilesNotInProject = FileManager.GetAllFilesInDirectory(
            GumState.Self.ProjectState.ScreenFilePath.FullPath, "gusx")
            .Select(item => new FilePath(item))
            .ToList();

        var screenFilesInProject = GumState.Self.ProjectState.GumProjectSave
            .Screens
            .Select(item => new FilePath(GumState.Self.ProjectState.ComponentFilePath + item.Name + ".gusx"))
            .ToArray();

        screenFilesNotInProject = screenFilesNotInProject
            .Except(screenFilesInProject)
            .ToList();

        viewModel.UnfilteredFileList.AddRange(screenFilesNotInProject.Select(item => item.FullPath));

        viewModel.RefreshFilteredList();

        view.DataContext = viewModel;
        var result = view.ShowDialog();
        return viewModel;
    }

    #endregion

    #region Component

    internal static void ShowImportComponentUi()
    {
        if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
        {
            MessageBox.Show("You must first save the project before adding a new component");
            return;
        }

        ImportFileViewModel viewModel = ShowImportComponentView();

        ComponentSave lastImportedComponent = null;

        string desiredDirectory = FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Components/";
        for (int i = 0; i < viewModel.SelectedFiles.Count; ++i)
        {
            lastImportedComponent = ImportComponent(viewModel.SelectedFiles[i], desiredDirectory, 
                // dont' save - we'll do it below:
                saveProject:false);
        }

        if (lastImportedComponent != null)
        {
            _guiCommands.RefreshElementTreeView();
            _selectedState.SelectedComponent = lastImportedComponent;
            _fileCommands.TryAutoSaveProject();
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

            var copyResult = MessageBox.Show("The file " + fileNameWithoutPath + " must be in the Gum project's Components folder. " +
                "Would you like to copy the file?", "Copy?", MessageBoxButtons.YesNo);

            shouldAdd = copyResult == DialogResult.Yes;

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

    private static ImportFileViewModel ShowImportComponentView()
    {
        var view = new ImportFileView();
        view.Title = "Import Component";

        var viewModel = new ImportFileViewModel();

        viewModel.BrowseFileFilter = "Gum Component (*.gucx)|*.gucx";

        var componentFilesNotInProject = FileManager.GetAllFilesInDirectory(
            GumState.Self.ProjectState.ComponentFilePath.FullPath, "gucx")
            .Select(item => new FilePath(item))
            .ToList();

        var componentFilesInProject = GumState.Self.ProjectState.GumProjectSave
            .Components
            .Select(item => new FilePath(GumState.Self.ProjectState.ComponentFilePath + item.Name + ".gucx"))
            .ToArray();

        componentFilesNotInProject = componentFilesNotInProject
            .Except(componentFilesInProject)
            .ToList();

        viewModel.UnfilteredFileList.AddRange(componentFilesNotInProject.Select(item => item.FullPath));
        viewModel.RefreshFilteredList();


        view.DataContext = viewModel;
        var result = view.ShowDialog();
        return viewModel;
    }

    #endregion

    #region Behavior

    public static void ShowImportBehaviorUi()
    {
        if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
        {
            MessageBox.Show("You must first save the project before adding a new component");
            return;
        }

        var viewModel = ShowImportBehaviorView();

        BehaviorSave lastImportedBehavior = null;

        string desiredDirectory = FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Behaviors/";
        for(int i = 0; i < viewModel.SelectedFiles.Count; ++i)
        {
            lastImportedBehavior = ImportBehavior(viewModel.SelectedFiles[i], desiredDirectory, saveProject:false);
        }

        if (lastImportedBehavior != null)
        {
            _guiCommands.RefreshElementTreeView();
            _selectedState.SelectedBehavior = lastImportedBehavior;
            _fileCommands.TryAutoSaveProject();
        }
    }

    public static BehaviorSave ImportBehavior(FilePath filePath, string desiredDirectory = null, bool saveProject = false)
    {
        var shouldAdd = true;

        desiredDirectory = desiredDirectory ?? FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Behaviors/";

        if (!FileManager.IsRelativeTo(filePath.FullPath, desiredDirectory))
        {
            string fileNameWithoutPath = filePath.FileNameNoPath;

            var copyResult = MessageBox.Show("The file " + fileNameWithoutPath + " must be in the Gum project's Behaviors folder. " +
                "Would you like to copy the file?", "Copy?", MessageBoxButtons.YesNo);

            shouldAdd = copyResult == DialogResult.Yes;

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

    private static ImportFileViewModel ShowImportBehaviorView()
    {
        var view = new ImportFileView();
        view.Title = "Import Behavior";

        var viewModel = new ImportFileViewModel();

        viewModel.BrowseFileFilter = "Gum Behavior (*.behx)|*.behx";

        var behaviorFilesNotInProject = FileManager.GetAllFilesInDirectory(
            GumState.Self.ProjectState.BehaviorFilePath.FullPath, "behx")
            .Select(item => new FilePath(item))
            .ToList();

        var behaviorFilesInProject = GumState.Self.ProjectState.GumProjectSave
            .Behaviors
            .Select(item => new FilePath(GumState.Self.ProjectState.BehaviorFilePath + item.Name + ".behx"))
            .ToArray();

        behaviorFilesNotInProject = behaviorFilesNotInProject
            .Except(behaviorFilesInProject)
            .ToList();

        viewModel.UnfilteredFileList.AddRange(behaviorFilesNotInProject.Select(item => item.FullPath));
        viewModel.RefreshFilteredList();

        view.DataContext = viewModel;
        var result = view.ShowDialog();
        return viewModel;
    }

    #endregion
}


