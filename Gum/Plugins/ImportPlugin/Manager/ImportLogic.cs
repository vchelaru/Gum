using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.ImportPlugin.Views;
using Gum.ToolStates;
using System;
using System.Linq;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.Manager
{
    public static class ImportLogic
    {

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

        public static void ImportScreen(FilePath fileName)
        {
            string desiredDirectory =
                FileManager.GetDirectory(
                ProjectManager.Self.GumProjectSave.FullFileName) + "Screens/";

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
                GumCommands.Self.GuiCommands.RefreshElementTreeView();

                SelectedState.Self.SelectedScreen = screenSave;

                GumCommands.Self.FileCommands.TryAutoSaveProject();
                GumCommands.Self.FileCommands.TryAutoSaveElement(screenSave);
            }
        }

        internal static void ShowImportComponentUi()
        {
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new component");
                return;
            }

            ImportFileViewModel viewModel = ShowImportComponentView();

            ComponentSave lastImportedComponent = null;

            for (int i = 0; i < viewModel.SelectedFiles.Count; ++i)
            {
                string fileName = viewModel.SelectedFiles[i];
                string desiredDirectory = FileManager.GetDirectory(
                    ProjectManager.Self.GumProjectSave.FullFileName) + "Components/";

                var shouldAdd = true;

                if (!FileManager.IsRelativeTo(fileName, desiredDirectory))
                {
                    string fileNameWithoutPath = FileManager.RemovePath(fileName);

                    var copyResult = MessageBox.Show("The file " + fileNameWithoutPath + " must be in the Gum project's Components folder. " +
                        "Would you like to copy the file?", "Copy?", MessageBoxButtons.YesNo);

                    shouldAdd = copyResult == DialogResult.Yes;

                    if (shouldAdd)
                    {
                        try
                        {
                            string destination = desiredDirectory + fileNameWithoutPath;
                            System.IO.File.Copy(fileName, destination);

                            fileName = destination;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error copying the file: " + ex.ToString());
                            break;
                        }

                    }
                }

                if (shouldAdd)
                {
                    string strippedName = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

                    var componentSave = FileManager.XmlDeserialize<ComponentSave>(fileName);

                    var componentReferences = ProjectManager.Self.GumProjectSave.ComponentReferences;
                    componentReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
                    componentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

                    var components = ProjectManager.Self.GumProjectSave.Components;
                    components.Add(componentSave);
                    components.Sort((first, second) => first.Name.CompareTo(second.Name));

                    componentSave.InitializeDefaultAndComponentVariables();

                    GumCommands.Self.FileCommands.TryAutoSaveElement(componentSave);

                    lastImportedComponent = componentSave;
                }
            }

            if (lastImportedComponent != null)
            {
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
                SelectedState.Self.SelectedComponent = lastImportedComponent;
                GumCommands.Self.FileCommands.TryAutoSaveProject();
            }
        }

        private static ImportFileViewModel ShowImportScreenView()
        {
            var view = new ImportFileView();

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


        private static ImportFileViewModel ShowImportComponentView()
        {
            var view = new ImportFileView();

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
    }
}
