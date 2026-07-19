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
using ToolsUtilities;

namespace Gum.Managers
{
    public class FileChangeReactionLogic
    {
        private readonly ISelectedState _selectedState;
        private readonly IWireframeCommands _wireframeCommands;
        private readonly IGuiCommands _guiCommands;
        private readonly IFileCommands _fileCommands;
        private readonly IOutputManager _outputManager;
        private readonly IWireframeObjectManager _wireframeObjectManager;
        private readonly IProjectState _projectState;
        private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;
        private readonly IPluginManager _pluginManager;

        public FileChangeReactionLogic(
            ISelectedState selectedState,
            IWireframeCommands wireframeCommands,
            IGuiCommands guiCommands,
            IFileCommands fileCommands,
            IOutputManager outputManager,
            IWireframeObjectManager wireframeObjectManager,
            IProjectState projectState,
            IStandardElementsManagerGumTool standardElementsManagerGumTool,
            IPluginManager pluginManager)
        {
            _selectedState = selectedState;
            _wireframeCommands = wireframeCommands;
            _guiCommands = guiCommands;
            _fileCommands = fileCommands;
            _outputManager = outputManager;
            _wireframeObjectManager = wireframeObjectManager;
            _projectState = projectState;
            _standardElementsManagerGumTool = standardElementsManagerGumTool;
            _pluginManager = pluginManager;
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
            // .ganx (animation collection) reload is handled by the StateAnimationPlugin via the
            // _pluginManager.ReactToFileChanged call below. The animation data is a per-element
            // sidecar owned by that plugin (not part of GumProjectSave), so there is no core-side
            // reload to perform here (issue #3410).
            else if(extension == "behx")
            {
                ReactToBehaviorChanged(file);
            }
            else if(extension == "csv" || extension == "resx")
            {
                ReactToLocalizationFileChanged(file);
            }

            _pluginManager.ReactToFileChanged(file);
        }

        /// <summary>
        /// Reacts to an element file (.gusx/.gucx/.gutx) that was deleted on disk while the tool
        /// is running. The flush routes here (instead of the change path) only once the file is
        /// confirmed gone, so a delete-then-recreate within the debounce window is handled as a
        /// normal reload instead. See <see cref="ReactToElementSaveDeleted"/> for why the element
        /// is flagged rather than reloaded.
        /// </summary>
        public void ReactToFileDeleted(FilePath file)
        {
            var extension = file.Extension;
            if (extension == GumProjectSave.ScreenExtension || extension == GumProjectSave.ComponentExtension ||
                extension == GumProjectSave.StandardExtension)
            {
                ReactToElementSaveDeleted(file);
            }
        }

        private void ReactToElementSaveDeleted(FilePath file)
        {
            // Defensive: the flush already gates on the file being gone, but if a delete event was
            // really a transient delete-then-recreate (atomic save) the file is back by now - in
            // which case the change path, not this one, should handle it.
            if (file.Exists()) return;

            var element = FlagElementForDeletedFile(file, _fileCommands.ProjectDirectory);
            if (element == null) return;

            _guiCommands.RefreshElementTreeView();
            _pluginManager.ElementReloaded(element);
        }

        /// <summary>
        /// Maps an element file path (.gusx/.gucx/.gutx) to the name ObjectFinder uses - the path
        /// relative to its type folder, e.g. "MyButton" or "Sub/MyButton". Returns null when the
        /// file isn't under the project directory (the same early-out the change path applied).
        /// </summary>
        internal static string? GetElementNameForElementFile(FilePath file, FilePath projectDirectory)
        {
            if (projectDirectory == null) return null;
            if (!projectDirectory.IsRootOf(file)) return null;

            FilePath standardized = file.RemoveExtension().StandardizedCaseSensitive;
            var relativeToFolderForType = standardized.RelativeTo(projectDirectory).Replace("\\", "/");

            if (relativeToFolderForType.StartsWith("Screens/"))
            {
                relativeToFolderForType = relativeToFolderForType.Substring("Screens/".Length);
            }
            else if (relativeToFolderForType.StartsWith("Components/"))
            {
                relativeToFolderForType = relativeToFolderForType.Substring("Components/".Length);
            }
            else if (relativeToFolderForType.StartsWith("Standards/"))
            {
                relativeToFolderForType = relativeToFolderForType.Substring("Standards/".Length);
            }
            return relativeToFolderForType;
        }

        /// <summary>
        /// Finds the loaded element whose source file was deleted and flags it
        /// <see cref="ElementSave.IsSourceFileMissing"/> so the tree shows the red "!" and the
        /// Errors tab lists GUM0004 (issue #3367). The element is deliberately NOT reloaded or
        /// removed: the in-memory copy is authoritative and may hold unsaved edits, and saving it
        /// re-creates the file (clearing the flag). Returns the flagged element, or null when the
        /// path maps to no loaded element or it was already flagged (so the caller can skip the
        /// UI refresh).
        /// </summary>
        public static ElementSave? FlagElementForDeletedFile(FilePath deletedFile, FilePath projectDirectory)
        {
            var elementName = GetElementNameForElementFile(deletedFile, projectDirectory);
            if (elementName == null) return null;

            var element = ObjectFinder.Self.GetElementSave(elementName);
            if (element != null && !element.IsSourceFileMissing)
            {
                element.IsSourceFileMissing = true;
                return element;
            }
            return null;
        }

        /// <summary>
        /// True when <paramref name="file"/> is the reappearance on disk of an element whose source
        /// we previously flagged missing (issue #3367) - a loaded element maps to this path and is
        /// currently <see cref="ElementSave.IsSourceFileMissing"/>. The file watcher uses this to
        /// process the lone Created event a restore fires (normally suppressed for Gum XML files, to
        /// avoid double reloads on save) so the element reloads from disk and the flag clears.
        /// </summary>
        public bool IsReappearanceOfMissingSourceElement(FilePath file)
            => IsReappearanceOfMissingSourceElement(file, _fileCommands.ProjectDirectory);

        public static bool IsReappearanceOfMissingSourceElement(FilePath file, FilePath projectDirectory)
        {
            var elementName = GetElementNameForElementFile(file, projectDirectory);
            if (elementName == null) return false;

            var element = ObjectFinder.Self.GetElementSave(elementName);
            return element != null && element.IsSourceFileMissing;
        }

        private void ReactToLocalizationFileChanged(FilePath file)
        {
            var gumProject = _projectState.GumProjectSave;
            var projectFiles = gumProject.LocalizationFiles;

            if(projectFiles == null || projectFiles.Count == 0)
            {
                return;
            }

            var projectDirectory = _projectState.ProjectDirectory;
            var resolvedBaseFiles = projectFiles
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => new FilePath(projectDirectory + path))
                .ToList();

            if(IsLocalizationFileThatShouldTriggerReload(file, resolvedBaseFiles))
            {
                _fileCommands.LoadLocalizationFile();
                // Potential optimization: if the changed file is a satellite (e.g. Strings.es.resx)
                // and CurrentLanguage doesn't map to that satellite's language index, this refresh
                // is unnecessary. The cost is currently acceptable (small XML reload + layout pass),
                // but if flickering becomes noticeable this should be gated on whether the changed
                // satellite matches the active language.
                _wireframeCommands.Refresh();
            }
        }

        /// <summary>
        /// Returns true if the changed file matches any base localization file in the list, or
        /// is a RESX satellite (e.g. <c>Strings.es.resx</c>) of any base RESX entry in the list.
        /// </summary>
        public static bool IsLocalizationFileThatShouldTriggerReload(FilePath changedFile, IEnumerable<FilePath> baseLocalizationFiles)
        {
            if(baseLocalizationFiles == null)
            {
                return false;
            }

            foreach(var baseFile in baseLocalizationFiles)
            {
                if(IsLocalizationFileThatShouldTriggerReload(changedFile, baseFile))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLocalizationFileThatShouldTriggerReload(FilePath changedFile, FilePath baseLocalizationFile)
        {
            if(changedFile == baseLocalizationFile)
                return true;

            // CSV files have no satellites
            if(baseLocalizationFile.Extension != "resx")
                return false;

            if(changedFile.Extension != "resx")
                return false;

            // Must be in the same directory
            var baseDirectory = System.IO.Path.GetDirectoryName(baseLocalizationFile.FullPath);
            var changedDirectory = System.IO.Path.GetDirectoryName(changedFile.FullPath);
            if(!string.Equals(baseDirectory, changedDirectory, StringComparison.OrdinalIgnoreCase))
                return false;

            // Changed file name (without extension) must start with "{BaseName}."
            var baseName = System.IO.Path.GetFileNameWithoutExtension(baseLocalizationFile.FullPath);
            var changedName = System.IO.Path.GetFileNameWithoutExtension(changedFile.FullPath);
            return changedName.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase);
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
            var relativeToFolderForType = GetElementNameForElementFile(file, projectDirectory);
            ////////////////////////Early Out////////////////////////////
            if (relativeToFolderForType == null) return;
            ///////////////////////End Early Out/////////////////////////

            var element = ObjectFinder.Self.GetElementSave(relativeToFolderForType);

            var refreshingSelected = element == _selectedState.SelectedElement;

            if(element != null)
            {
                _projectState.GumProjectSave.ReloadElement(element);
                _projectState.GumProjectSave.Initialize();
                _standardElementsManagerGumTool.FixCustomTypeConverters(_projectState.GumProjectSave);


                if (refreshingSelected)
                {
                    _selectedState.SelectedElement = null;
                }
                _guiCommands.RefreshElementTreeView();

                element = ObjectFinder.Self.GetElementSave(relativeToFolderForType);

                if(refreshingSelected)
                {
                    _selectedState.SelectedElement = element;
                }
                _pluginManager.ElementReloaded(element);
            }

            bool shouldReloadWireframe = false;

            var currentElement = _selectedState.SelectedElement;

            if(currentElement != null)
            {
                var currentElementFile = _fileCommands.GetFullPathXmlFile(currentElement, currentElement.Name);
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
                _standardElementsManagerGumTool.FixCustomTypeConverters(_projectState.GumProjectSave);

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
