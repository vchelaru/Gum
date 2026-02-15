using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ToolsUtilities;

namespace Gum.Managers
{
    public class FileChangeReactionLogic : Singleton<FileChangeReactionLogic>
    {
        private readonly ISelectedState _selectedState;
        private readonly WireframeCommands _wireframeCommands;
        private readonly IGuiCommands _guiCommands;
        private readonly IFileCommands _fileCommands;
        private readonly IOutputManager _outputManager;
        private readonly IWireframeObjectManager _wireframeObjectManager;
        private readonly IProjectState _projectState;

        public FileChangeReactionLogic()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
            _guiCommands = Locator.GetRequiredService<IGuiCommands>();
            _fileCommands = Locator.GetRequiredService<IFileCommands>();
            _outputManager = Locator.GetRequiredService<IOutputManager>();
            _wireframeObjectManager = Locator.GetRequiredService<IWireframeObjectManager>();
            _projectState = Locator.GetRequiredService<IProjectState>();
        }
        
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
                var isCurrentProject = file == _projectState.GumProjectSave.FullFileName;
                if(isCurrentProject)
                {
                    ReactToProjectChanged(file);
                }
            }
            else if(extension == "ganx")
            {
                _outputManager.AddOutput($"Gum detected a changed animation file: \n{file}" +
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
            var gumProject = _projectState.GumProjectSave;

            if(!string.IsNullOrEmpty(gumProject.LocalizationFile))
            {
                FilePath localizationFile = _projectState.ProjectDirectory + gumProject.LocalizationFile;

                if(localizationFile == file)
                {
                    _fileCommands.LoadLocalizationFile();
                    _wireframeCommands.Refresh();
                }
            }
        }

        private void ReactToProjectChanged(FilePath file)
        {
            var currentElement = _selectedState.SelectedElement;
            var elementName = currentElement?.Name;

            _fileCommands.LoadProject(file.Standardized);

            if(!string.IsNullOrEmpty(elementName))
            {
                var elementToSelect = ObjectFinder.Self.GetElementSave(elementName);

                if(elementToSelect != null)
                {
                    _selectedState.SelectedElement = elementToSelect;
                }
            }
        }

        private void ReactToImageFileChanged(FilePath file)
        {
            var currentElement = _selectedState.SelectedElement;
            string relativeDirectory = _projectState.ProjectDirectory;

            if (currentElement != null)
            {
                var referencedFiles = ObjectFinder.Self
                    .GetFilesReferencedBy(currentElement)
                    .Select(item => new FilePath(item))
                    .ToList()
                    ;

                // prevents duplication 
                var hashSet = referencedFiles.ToHashSet();

                foreach (var innerFile in hashSet)
                {
                    FillWithRecursiveReferences(innerFile, referencedFiles);
                }

                if(referencedFiles.Contains(file))
                {
                    _wireframeObjectManager.RefreshAll(true, true);
                }
            }
        }

        private void FillWithRecursiveReferences(FilePath innerFile, List<FilePath> referencedFiles)
        {
            var extension = innerFile.Extension;
            if(extension == "fnt")
            {
                try
                {
                    var contents = FileManager.FromFileText(innerFile.Standardized);

                    var font = new ParsedFontFile(contents);

                    var pages = font.Pages;

                    var fontDirectory = innerFile.GetDirectoryContainingThis();

                    foreach(var page in pages)
                    {
                        var pageFile = fontDirectory + page.File;
                        var filePath = new FilePath(pageFile);
                        if(!referencedFiles.Contains(filePath))
                        {
                            referencedFiles.Add(filePath);
                            FillWithRecursiveReferences(filePath, referencedFiles);
                        }
                    }
                }
                catch
                {
                    // bad file, do anything?
                }
            }
        }

        private void ReactToAnimationChainChanged(FilePath file)
        {
            var currentElement = _selectedState.SelectedElement;
            string relativeDirectory = _projectState.ProjectDirectory;
            if (currentElement != null)
            {
                var referencedFiles = ObjectFinder.Self
                    .GetFilesReferencedBy(currentElement)
                    .Select(item => new FilePath(item))
                    .ToList()
                    ;
                if (referencedFiles.Contains(file))
                {
                    _wireframeObjectManager.RefreshAll(true, true);
                }
            }
        }

        private void ReactToFontFileChanged(FilePath file)
        {
            var currentElement = _selectedState.SelectedElement;
            string relativeDirectory = _projectState.ProjectDirectory;

            if (currentElement != null)
            {
                var referencedFiles = ObjectFinder.Self
                    .GetFilesReferencedBy(currentElement)
                    .Select(item => new FilePath(item))
                    .ToList()
                    ;

                if (referencedFiles.Contains(file))
                {
                    _wireframeObjectManager.RefreshAll(true, true);
                }
            }
        }


        private void ReactToElementSaveChanged(FilePath file)
        {
            var projectDirectory = _fileCommands.ProjectDirectory;
            ////////////////////////Early Out////////////////////////////
            if (projectDirectory == null) return;
            if (!projectDirectory.IsRootOf(file)) return;
            ///////////////////////End Early Out/////////////////////////

            FilePath standardized = file.RemoveExtension().StandardizedCaseSensitive;
            var relativeToFolderForType = standardized.RelativeTo(projectDirectory).Replace("\\", "/");

            if(relativeToFolderForType.StartsWith("Screens/"))
            {
                relativeToFolderForType = relativeToFolderForType.Substring("Screens/".Length);
            }
            else if(relativeToFolderForType.StartsWith("Components/"))
            {
                relativeToFolderForType = relativeToFolderForType.Substring("Components/".Length);
            }
            else if(relativeToFolderForType.StartsWith("Standards/"))
            {
                relativeToFolderForType = relativeToFolderForType.Substring("Standards/".Length);
            }


            var element = ObjectFinder.Self.GetElementSave(relativeToFolderForType);

            var refreshingSelected = element == _selectedState.SelectedElement;

            if(element != null)
            {
                _projectState.GumProjectSave.ReloadElement(element);
                _projectState.GumProjectSave.Initialize();
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(_projectState.GumProjectSave);


                if (refreshingSelected)
                {
                    _selectedState.SelectedElement = null;
                }
                _guiCommands.RefreshElementTreeView();

                if(refreshingSelected)
                {
                    element = ObjectFinder.Self.GetElementSave(file.StandardizedNoPathNoExtension);
                    _selectedState.SelectedElement = element;
                }

            }

            bool shouldReloadWireframe = false;

            var currentElement = _selectedState.SelectedElement;

            if(currentElement != null)
            {
                var currentElementFile = currentElement.GetFullPathXmlFile();
                shouldReloadWireframe = currentElementFile == file;
            }

            if(element != null && !shouldReloadWireframe)
            {
                // Update - we should also refresh if the element is referenced by any visual object
                var hasMatchingRepresentation = _wireframeObjectManager.AllIpsos
                    .Any(item => item.Tag is InstanceSave asInstance && asInstance.BaseType == element.Name);

                shouldReloadWireframe = hasMatchingRepresentation;
            }



            if(shouldReloadWireframe)
            {
                // reload wireframe
                _wireframeObjectManager.RefreshAll(true, true);

                // todo - this isn't working if I rename a variable...
                _guiCommands.RefreshVariables(force: true);
            }
        }

        private void ReactToBehaviorChanged(FilePath file)
        {
            var behavior = _projectState.GumProjectSave.Behaviors.FirstOrDefault(item =>
            // It's somehow possible for behaviors with no name to make it in the project. let's tolerate it
                item?.Name.ToLowerInvariant() == file.StandardizedNoPathNoExtension.ToLowerInvariant());

            var refreshingSelected = behavior == _selectedState.SelectedBehavior;

            if (behavior != null)
            {
                _projectState.GumProjectSave.ReloadBehavior(behavior);
                _projectState.GumProjectSave.Initialize();
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(_projectState.GumProjectSave);

                if (refreshingSelected)
                {
                    _selectedState.SelectedBehavior = null;
                }
                _guiCommands.RefreshElementTreeView();

                if (refreshingSelected)
                {
                    behavior = _projectState.GumProjectSave.Behaviors.FirstOrDefault(item =>
                        item.Name.ToLowerInvariant() == file.StandardizedNoPathNoExtension.ToLowerInvariant());
                    _selectedState.SelectedBehavior = behavior;

                    _guiCommands.RefreshVariables(force: true);
                }

                // no need to reload wirefreame like we do for elements because they don't show up visually...
            }
        }

    }
}
