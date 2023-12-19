using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Plugins;
using Gum.ToolStates;
using System.Linq;
using ToolsUtilities;

namespace Gum.Managers
{
    public class FileChangeReactionLogic : Singleton<FileChangeReactionLogic>
    {
        public void ReactToFileChanged(FilePath file)
        {
            var extension = file.Extension;

            if(extension == "png" || extension == "gif" || extension == "tga" || extension == "bmp")
            {
                ReactToImageFileChanged(file);
            }
            else if(extension == "achx")
            {
                ReactToAnimationChainChanged(file);
            }
            else if(extension == "fnt")
            {
                ReactToFontFileChanged(file);
            }
            else if(extension == GumProjectSave.ScreenExtension || extension == GumProjectSave.ComponentExtension || 
                extension == GumProjectSave.StandardExtension)
            {
                ReactToElementSaveChanged(file);
            }
            else if(extension == GumProjectSave.ProjectExtension)
            {
                var isCurrentProject = file == ProjectState.Self.GumProjectSave.FullFileName;
                if(isCurrentProject)
                {
                    ReactToProjectChanged(file);
                }
            }
            else if(extension == "ganx")
            {
                OutputManager.Self.AddOutput($"Gum detected a changed animation file: \n{file}" +
                    $"Gum does not currently support reloading animation files, so you should restart Gum to reload this file.");
            }
            else if(extension == "behx")
            {
                ReactToBehaviorChanged(file);
            }
            else if(extension == "csv")
            {
                ReactToCsvChanged(file);
            }

            PluginManager.Self.ReactToFileChanged(file);
        }

        private void ReactToCsvChanged(FilePath file)
        {
            var gumProject = GumState.Self.ProjectState.GumProjectSave;

            if(!string.IsNullOrEmpty(gumProject.LocalizationFile))
            {
                FilePath localizationFile = GumState.Self.ProjectState.ProjectDirectory + gumProject.LocalizationFile;

                if(localizationFile == file)
                {
                    GumCommands.Self.FileCommands.LoadLocalizationFile();
                    GumCommands.Self.WireframeCommands.Refresh();
                }
            }
        }

        private void ReactToProjectChanged(FilePath file)
        {
            GumCommands.Self.FileCommands.LoadProject(file.Standardized);
        }

        private void ReactToImageFileChanged(FilePath file)
        {
            var currentElement = SelectedState.Self.SelectedElement;
            string relativeDirectory = ProjectState.Self.ProjectDirectory;

            if (currentElement != null)
            {
                var referencedFiles = ObjectFinder.Self
                    .GetFilesReferencedBy(currentElement)
                    .Select(item => new FilePath(item))
                    .ToList()
                    ;

                if(referencedFiles.Contains(file))
                {
                    Wireframe.WireframeObjectManager.Self.RefreshAll(true, true);
                }
            }
        }

        private void ReactToAnimationChainChanged(FilePath file)
        {
            var currentElement = SelectedState.Self.SelectedElement;
            string relativeDirectory = ProjectState.Self.ProjectDirectory;
            if (currentElement != null)
            {
                var referencedFiles = ObjectFinder.Self
                    .GetFilesReferencedBy(currentElement)
                    .Select(item => new FilePath(item))
                    .ToList()
                    ;
                if (referencedFiles.Contains(file))
                {
                    Wireframe.WireframeObjectManager.Self.RefreshAll(true, true);
                }
            }
        }

        private void ReactToFontFileChanged(FilePath file)
        {
            var currentElement = SelectedState.Self.SelectedElement;
            string relativeDirectory = ProjectState.Self.ProjectDirectory;

            if (currentElement != null)
            {
                var referencedFiles = ObjectFinder.Self
                    .GetFilesReferencedBy(currentElement)
                    .Select(item => new FilePath(item))
                    .ToList()
                    ;

                if (referencedFiles.Contains(file))
                {
                    Wireframe.WireframeObjectManager.Self.RefreshAll(true, true);
                }
            }
        }


        private void ReactToElementSaveChanged(FilePath file)
        {
            var element = ObjectFinder.Self.GetElementSave(file.StandardizedNoPathNoExtension);

            var refreshingSelected = element == SelectedState.Self.SelectedElement;

            if(element != null)
            {
                ProjectState.Self.GumProjectSave.ReloadElement(element);
                ProjectState.Self.GumProjectSave.Initialize();
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(ProjectState.Self.GumProjectSave);


                if (refreshingSelected)
                {
                    ElementTreeViewManager.Self.Select((ElementSave)null);
                }
                GumCommands.Self.GuiCommands.RefreshElementTreeView();

                if(refreshingSelected)
                {
                    element = ObjectFinder.Self.GetElementSave(file.StandardizedNoPathNoExtension);
                    ElementTreeViewManager.Self.Select(element);
                }

            }

            bool shouldReloadWireframe = false;

            var currentElement = SelectedState.Self.SelectedElement;

            if(currentElement != null)
            {
                var currentElementFile = currentElement.GetFullPathXmlFile();
                shouldReloadWireframe = currentElementFile == file;
            }

            if(element != null && !shouldReloadWireframe)
            {
                // Update - we should also refresh if the element is referenced by any visual object
                var hasMatchingRepresentation = Wireframe.WireframeObjectManager.Self.AllIpsos
                    .Any(item => item.Tag is InstanceSave asInstance && asInstance.BaseType == element.Name);

                shouldReloadWireframe = hasMatchingRepresentation;
            }



            if(shouldReloadWireframe)
            {
                // reload wireframe
                Wireframe.WireframeObjectManager.Self.RefreshAll(true, true);

                // todo - this isn't working if I rename a variable...
                PropertyGridManager.Self.RefreshUI(force: true);
            }
        }

        private void ReactToBehaviorChanged(FilePath file)
        {
            var behavior = ProjectState.Self.GumProjectSave.Behaviors.FirstOrDefault(item =>
                item.Name.ToLowerInvariant() == file.StandardizedNoPathNoExtension.ToLowerInvariant());

            var refreshingSelected = behavior == SelectedState.Self.SelectedBehavior;

            if (behavior != null)
            {
                ProjectState.Self.GumProjectSave.ReloadBehavior(behavior);
                ProjectState.Self.GumProjectSave.Initialize();
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(ProjectState.Self.GumProjectSave);

                if (refreshingSelected)
                {
                    ElementTreeViewManager.Self.Select((BehaviorSave)null);
                }
                GumCommands.Self.GuiCommands.RefreshElementTreeView();

                if (refreshingSelected)
                {
                    behavior = ProjectState.Self.GumProjectSave.Behaviors.FirstOrDefault(item =>
                        item.Name.ToLowerInvariant() == file.StandardizedNoPathNoExtension.ToLowerInvariant());
                    ElementTreeViewManager.Self.Select(behavior);

                    PropertyGridManager.Self.RefreshUI(force: true);
                }

                // no need to reload wirefreame like we do for elements because they don't show up visually...
            }
        }

    }
}
