using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IDialogService _dialogService;
        private readonly IUnsavedChangesTracker _unsavedChangesTracker;
        private readonly IVariableReferenceLogic _variableReferenceLogic;
        private readonly IUndoManager _undoManager;

        /// <summary>
        /// Names of the elements reloaded while a <see cref="DeferDependentUpdates"/> scope is open;
        /// null when no scope is open.
        /// </summary>
        private Dictionary<string, ElementSave>? _reloadedWhileDeferring;

        /// <summary>
        /// The behaviors reloaded in the same scope, by name, each with the copy it replaced (the first, when
        /// reloaded twice); null exactly when <see cref="_reloadedWhileDeferring"/> is.
        /// </summary>
        private Dictionary<string, BehaviorSave>? _behaviorsReloadedWhileDeferring;

        private readonly IProjectReferencedFiles _referencedFiles;

        /// <summary>A referenced file changed while a scope was open, so closing it reloads the canvas once.</summary>
        private bool _contentReloadPending;

        /// <summary>
        /// Files that changed or were deleted while a scope was open, whose check against the project's references
        /// waits until it closes. An element reloaded earlier in the same flush forgets what the project references, so
        /// asking per file would walk the project again after each reload; asking once the whole batch is back in
        /// memory walks it once. Null exactly when <see cref="_reloadedWhileDeferring"/> is.
        /// </summary>
        private List<FilePath>? _referencedFileChecksWhileDeferring;

        public FileChangeReactionLogic(
            ISelectedState selectedState,
            IWireframeCommands wireframeCommands,
            IGuiCommands guiCommands,
            IFileCommands fileCommands,
            IOutputManager outputManager,
            IWireframeObjectManager wireframeObjectManager,
            IProjectState projectState,
            IStandardElementsManagerGumTool standardElementsManagerGumTool,
            IPluginManager pluginManager,
            IDialogService dialogService,
            IUnsavedChangesTracker unsavedChangesTracker,
            IVariableReferenceLogic variableReferenceLogic,
            IUndoManager undoManager,
            IProjectReferencedFiles? referencedFiles = null)
        {
            _referencedFiles = referencedFiles ?? new ProjectReferencedFiles();
            _variableReferenceLogic = variableReferenceLogic;
            _undoManager = undoManager;
            _dialogService = dialogService;
            _unsavedChangesTracker = unsavedChangesTracker;
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

        /// <summary>
        /// While the returned scope is open, an element reloaded from disk does not update the elements whose
        /// variable references read it; disposing the scope does that once for everything reloaded. A flush of
        /// several changed files (a git checkout or merge) opens one so a reader that is itself in the batch
        /// is reloaded before any save of it, instead of being written over the external edit still queued.
        /// </summary>
        public IDisposable DeferDependentUpdates()
        {
            _reloadedWhileDeferring ??= new Dictionary<string, ElementSave>();
            _referencedFileChecksWhileDeferring ??= new List<FilePath>();
            _behaviorsReloadedWhileDeferring ??= new Dictionary<string, BehaviorSave>();
            return new DeferredDependentUpdates(this);
        }

        private sealed class DeferredDependentUpdates : IDisposable
        {
            private readonly FileChangeReactionLogic _owner;

            public DeferredDependentUpdates(FileChangeReactionLogic owner) => _owner = owner;

            public void Dispose()
            {
                Dictionary<string, ElementSave>? reloaded = _owner._reloadedWhileDeferring;
                Dictionary<string, BehaviorSave>? reloadedBehaviors = _owner._behaviorsReloadedWhileDeferring;
                List<FilePath>? filesToCheck = _owner._referencedFileChecksWhileDeferring;
                bool shouldRefresh = _owner._contentReloadPending;
                _owner._reloadedWhileDeferring = null;
                _owner._behaviorsReloadedWhileDeferring = null;
                _owner._referencedFileChecksWhileDeferring = null;
                _owner._contentReloadPending = false;
                try
                {
                    if (filesToCheck != null && _owner.AnyIsReferencedFileAndForgetWhatTheyList(filesToCheck))
                    {
                        shouldRefresh = true;
                    }

                    if (reloaded != null && _owner.UpdateDependentsOf(reloaded, reloadedBehaviors ?? new Dictionary<string, BehaviorSave>()))
                    {
                        shouldRefresh = true;
                    }

                    if (shouldRefresh)
                    {
                        _owner.RefreshCanvasAndVariables();
                    }
                }
                catch (Exception ex)
                {
                    // A flush calls this from a using block; one failure must not skip the rest of the flush.
                    _owner._outputManager.AddError("Could not update the elements that read a file reloaded from disk: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Re-evaluates the references that read the just-reloaded <paramref name="reloaded"/> elements, the same
        /// cascade an in-tool edit of their variables runs: readers (and readers of readers) get the new values
        /// and are saved when Auto Save is on. Elements are looked up by name because a reload replaces the object.
        /// The reloaded elements themselves are not saved, so the watcher has nothing new to see.
        /// The tool-only references of the behaviors involved are re-applied first, to the reloaded elements, the
        /// components implementing a reloaded behavior, and the elements holding instances of them, so the
        /// values the references materialize follow the outside edit.
        /// </summary>
        /// <returns>True when some stored value changed.</returns>
        private bool UpdateDependentsOf(IReadOnlyDictionary<string, ElementSave> reloaded, IReadOnlyDictionary<string, BehaviorSave> reloadedBehaviors)
        {
            List<StoredValueChange> changes = new List<StoredValueChange>();
            foreach (KeyValuePair<string, BehaviorSave> pair in reloadedBehaviors)
            {
                BehaviorSave? behavior = _projectState.GumProjectSave?.Behaviors.FirstOrDefault(item =>
                    string.Equals(item?.Name, pair.Key, StringComparison.OrdinalIgnoreCase));
                if (behavior != null)
                {
                    changes.AddRange(_variableReferenceLogic.ReapplyToolOnlyReferences(behavior, pair.Value, trySave: true));
                }
            }
            foreach (KeyValuePair<string, ElementSave> pair in reloaded)
            {
                ElementSave? element = ObjectFinder.Self.GetElementSave(pair.Key);
                if (element != null)
                {
                    changes.AddRange(_variableReferenceLogic.ReapplyToolOnlyReferences(element, pair.Value, trySave: true));
                    changes.AddRange(_variableReferenceLogic.ApplyReferencesToElement(element, trySave: true));
                }
            }

            // The readers keep their undo history, whose snapshots hold the values just replaced.
            // The outside edit is not an undoable action, so the snapshots take the new values instead.
            _undoManager.RebaseStoredValues(changes);
            return changes.Count > 0;
        }

        private void RefreshCanvasAndVariables()
        {
            _wireframeObjectManager.RefreshAll(true, true);
            _guiCommands.RefreshVariables(force: true);
        }

        public void ReactToFileChanged(FilePath file)
        {
            var extension = file.Extension;

            if(IsElementExtension(extension))
            {
                ReactToElementSaveChanged(file);
            }
            else if(extension == GumProjectSave.ProjectExtension || extension == GumProjectSave.ProjectJsonExtension)
            {
                var isCurrentProject = _projectState.GumProjectSave?.FullFileName is { } projectFileName && file == projectFileName;
                if(isCurrentProject)
                {
                    ReactToProjectChanged(file);
                }
            }
            // .ganx/.ganj (animation collection) reload is handled by the StateAnimationPlugin via the
            // _pluginManager.ReactToFileChanged call below. The animation data is a per-element
            // sidecar owned by that plugin (not part of GumProjectSave), so there is no core-side
            // reload to perform here (issue #3410).
            else if(extension == BehaviorReference.Extension || extension == BehaviorReference.JsonExtension)
            {
                ReactToBehaviorChanged(file);
            }
            else if(extension == "csv" || extension == "resx")
            {
                ReactToLocalizationFileChanged(file);
            }
            else if(ShaderFileExtensions.Contains(extension))
            {
                ReactToShaderFileChanged(file);
            }
            else
            {
                // Images, svg, lottie, animation chains, fonts and any other file a variable names: no list of
                // extensions, the project's references decide (#7027).
                ReactToReferencedFileChanged(file);
            }

            _pluginManager.ReactToFileChanged(file);
        }

        /// <summary>
        /// Reacts to an element file (.gusx/.gucx/.gutx, or their JSON siblings .gusj/.gucj/.gutj)
        /// that was deleted on disk while the tool is running. The flush routes here (instead of the
        /// change path) only once the file is confirmed gone, so a delete-then-recreate within the
        /// debounce window is handled as a normal reload instead. See
        /// <see cref="ReactToElementSaveDeleted"/> for why the element is flagged rather than reloaded.
        /// </summary>
        public void ReactToFileDeleted(FilePath file)
        {
            var extension = file.Extension;
            if (IsElementExtension(extension))
            {
                ReactToElementSaveDeleted(file);
            }
            else if (ShaderFileExtensions.Contains(extension))
            {
                // The refresh makes the canvas look the shader up again; a missing file is reported
                // the way a missing shader is when it is picked.
                ReactToShaderFileChanged(file);
            }
            else
            {
                // The rebuilt canvas reports the missing file the way it does when the file is picked.
                ReactToReferencedFileChanged(file);
            }
        }

        /// <summary>
        /// True for a Screen/Component/StandardElement file's extension, XML or JSON (issue #4182).
        /// </summary>
        private static bool IsElementExtension(string extension) =>
            extension == GumProjectSave.ScreenExtension || extension == GumProjectSave.ScreenJsonExtension ||
            extension == GumProjectSave.ComponentExtension || extension == GumProjectSave.ComponentJsonExtension ||
            extension == GumProjectSave.StandardExtension || extension == GumProjectSave.StandardJsonExtension;

        private static bool IsJsonElementExtension(string extension) =>
            extension == GumProjectSave.ScreenJsonExtension ||
            extension == GumProjectSave.ComponentJsonExtension ||
            extension == GumProjectSave.StandardJsonExtension;

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
        /// file isn't under the project directory (the same early-out the change path applied), or
        /// when the file is in the other format than the open project, e.g. a leftover .gucx next
        /// to a JSON project - that file does not back any loaded element (issue #4926).
        /// </summary>
        internal static string? GetElementNameForElementFile(FilePath file, FilePath projectDirectory)
        {
            if (projectDirectory == null) return null;
            if (!projectDirectory.IsRootOf(file)) return null;
            bool isJsonProject = GumProjectSave.IsJsonFormat(ObjectFinder.Self.GumProjectSave?.FullFileName ?? "");
            if (IsJsonElementExtension(file.Extension) != isJsonProject) return null;

            FilePath standardized = file.RemoveExtension().StandardizedCaseSensitive;
            var relativeToFolderForType = standardized.RelativeTo(projectDirectory).Replace("\\", "/");

            foreach (var subfolder in new[] { ElementReference.ScreenSubfolder, ElementReference.ComponentSubfolder, ElementReference.StandardSubfolder })
            {
                var prefix = subfolder + "/";
                // macOS volumes are case-insensitive, so the folder may be reported as "components/".
                if (relativeToFolderForType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return relativeToFolderForType.Substring(prefix.Length);
                }
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
        /// True when <paramref name="file"/> is a Gum file the open project already contains: a loaded
        /// element's file (or its animation sidecar), a loaded behavior, or the project file itself.
        /// A watcher event for such a file is a real edit even when it arrives as Created alone, which
        /// is how macOS reports an atomic swap over the file. A Gum file nothing has loaded (a brand-new
        /// file dropped into the folder) is not known, and a Created for it is not a reload.
        /// </summary>
        public bool IsKnownToProject(FilePath file)
        {
            string extension = file.Extension;
            FilePath projectDirectory = _fileCommands.ProjectDirectory;

            if (IsElementExtension(extension))
            {
                string? elementName = GetElementNameForElementFile(file, projectDirectory);
                return elementName != null && ObjectFinder.Self.GetElementSave(elementName) != null;
            }

            if (extension == GumProjectSave.ProjectExtension || extension == GumProjectSave.ProjectJsonExtension)
            {
                return _projectState.GumProjectSave?.FullFileName is { } projectFileName && file == projectFileName;
            }

            if (extension == BehaviorReference.Extension || extension == BehaviorReference.JsonExtension)
            {
                return _projectState.GumProjectSave?.Behaviors.Any(behavior =>
                    string.Equals(behavior?.Name, file.CaseSensitiveNoPathNoExtension, StringComparison.OrdinalIgnoreCase)) == true;
            }

            if (extension == "ganx" || extension == "ganj")
            {
                // "<Element>Animations.ganx" sits beside the element it animates.
                const string sidecarSuffix = "Animations";
                string withoutExtension = file.RemoveExtension().FullPath;
                if (!withoutExtension.EndsWith(sidecarSuffix, StringComparison.Ordinal))
                {
                    return false;
                }
                string elementExtension = extension == "ganj" ? GumProjectSave.ComponentJsonExtension : GumProjectSave.ComponentExtension;
                FilePath elementFile = new FilePath(withoutExtension.Substring(0, withoutExtension.Length - sidecarSuffix.Length) + "." + elementExtension);
                string? elementName = GetElementNameForElementFile(elementFile, projectDirectory);
                return elementName != null && ObjectFinder.Self.GetElementSave(elementName) != null;
            }

            return false;
        }


        /// <summary>
        /// True when <paramref name="file"/> maps to a loaded element currently flagged
        /// <see cref="ElementSave.IsSourceFileMissing"/>, i.e. its file has reappeared.
        /// </summary>
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
            var projectFiles = gumProject?.LocalizationFiles;

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
            _referencedFiles.Invalidate();

            // Reloading the project replaces every element and behavior, so any unsaved edit
            // anywhere in it would be lost (#5388).
            if (_projectState.GumProjectSave is { } project && _unsavedChangesTracker.HasAnyUnsavedChanges(project)
                && UserKeepsUnsavedChanges(ProjectPromptName, file))
            {
                return;
            }

            var currentElement = _selectedState.SelectedElement;
            var elementName = currentElement?.Name;

            // Fire-and-forget: this reaction runs from FileWatchManager's synchronous flush loop
            // (a timer tick, not a user-initiated action), which stays synchronous so one changed
            // file's reload doesn't block the rest of the queue. The load itself now runs off the
            // calling thread (#4871), so this needs its own error reporting - a faulted task here
            // wouldn't otherwise surface anywhere.
            _ = ReloadProjectAndReselectAsync(file, elementName);
        }

        private async Task ReloadProjectAndReselectAsync(FilePath file, string? elementName)
        {
            try
            {
                await _fileCommands.LoadProjectAsync(file.FullPath);
            }
            catch (Exception ex)
            {
                _guiCommands.PrintOutput($"Error reloading project after external change to {file}: {ex.Message}");
                return;
            }

            if(!string.IsNullOrEmpty(elementName))
            {
                var elementToSelect = ObjectFinder.Self.GetElementSave(elementName);

                if(elementToSelect != null)
                {
                    _selectedState.SelectedElement = elementToSelect;
                }
            }
        }

        /// <summary>
        /// A file the canvas shows from disk changed, was renamed over or was deleted: an image, svg, lottie
        /// document, animation chain, font or any other file a variable names. Every one of them is cached by
        /// path in <see cref="LoaderManager"/>, so the whole cache is dropped and the shown element rebuilt, even
        /// when it does not use the file itself (it may use it through a nested instance or a base type, and an
        /// element selected later would otherwise draw the cached copy). A file is the tool's business when an
        /// element in the project references it or the canvas cached it (a font the tool generated is cached
        /// without a variable naming it); anything else, such as build output, does nothing.
        /// A flush of several such files reloads once, when its scope closes.
        /// </summary>
        private void ReactToReferencedFileChanged(FilePath file)
        {
            if (_referencedFileChecksWhileDeferring != null)
            {
                _referencedFileChecksWhileDeferring.Add(file);
                return;
            }

            if (!IsReferencedFile(file))
            {
                return;
            }

            if (ListsOtherFiles(file))
            {
                // What the project references includes the pictures this file names, which the edit changed.
                _referencedFiles.Invalidate();
            }

            if (_reloadedWhileDeferring != null)
            {
                _contentReloadPending = true;
            }
            else
            {
                RefreshCanvasAndVariables();
            }
        }

        /// <summary>
        /// The deferred form of <see cref="ReactToReferencedFileChanged"/> for a whole flush: true when some of
        /// <paramref name="files"/> is one the tool refreshes. Files that name other files (an animation chain, a
        /// font) are judged first and then make the project forget what it references, so the pictures they now
        /// name count in the check of the rest; that costs two walks at most however the flush was ordered.
        /// </summary>
        private bool AnyIsReferencedFileAndForgetWhatTheyList(List<FilePath> files)
        {
            bool anyListerChanged = false;
            foreach (FilePath file in files.Where(ListsOtherFiles))
            {
                if (IsReferencedFile(file))
                {
                    anyListerChanged = true;
                }
            }
            if (anyListerChanged)
            {
                _referencedFiles.Invalidate();
            }

            return anyListerChanged || files.Where(file => !ListsOtherFiles(file)).Any(IsReferencedFile);
        }

        /// <summary>
        /// True when <paramref name="file"/> is one the tool refreshes through the referenced-file reaction: an
        /// element in the project references it, or the canvas cached it. A file with a reaction of its own is
        /// never one, whatever the project lists. The answer comes from the kept set of referenced files and
        /// the canvas's cache, so it is cheap to ask for every file event: the project is walked once until
        /// something changes what it references.
        /// </summary>
        public bool IsReferencedFile(FilePath file) =>
            !HasItsOwnReaction(file.Extension) && (IsReferencedByProject(file) || IsCachedByCanvas(file));

        /// <summary>
        /// Files another reaction owns, which the project's walk lists anyway: the State Animation plugin reloads
        /// animation collections, and the Fonts tab and code output plugins read their own files. Reloading the
        /// canvas for them as well would redo that work.
        /// </summary>
        private static bool HasItsOwnReaction(string extension) =>
            extension is "ganx" or "ganj" or "gumfcs" or "codsj";

        /// <summary>Files that name other files the project then references (an animation chain's pictures, a font's pages).</summary>
        private static bool ListsOtherFiles(FilePath file) => file.Extension is "achx" or "achj" or "fnt";

        private static bool IsCachedByCanvas(FilePath file) =>
            LoaderManager.Self.CachedDisposables.Keys.Any(key => IsSameFile(key, file));

        /// <summary>
        /// A shader source file changed or was deleted on disk. The compiled effect is cached by path
        /// (in <see cref="LoaderManager"/>) for every container showing it, and the Variables tab rows come
        /// from that effect and from the defaults in the source text. Dropping the cache entry makes the next
        /// build of any container using the shader, in whatever element is selected later, compile again.
        /// The shown element is rebuilt now, which also rebuilds the selected instance's rows. Images and
        /// fonts stay cached, so the rebuild is cheap. A shader no element references shows nothing, so
        /// nothing is touched.
        /// </summary>
        private void ReactToShaderFileChanged(FilePath file)
        {
            if (!IsReferencedByProject(file))
            {
                return;
            }

            DropCachedEffect(file);

            // Whatever is shown may use the shader through a nested component instance or a base type, which
            // a per-element file walk does not follow, so the shown element is always rebuilt.
            if (_selectedState.SelectedElement != null)
            {
                _wireframeObjectManager.RefreshAll(true);
            }
        }

        private static void DropCachedEffect(FilePath file)
        {
            // The cache key is the path as the canvas built it, so match by file rather than by spelling.
            List<string> keys = LoaderManager.Self.CachedDisposables.Keys
                .Where(key => IsSameFile(key, file))
                .ToList();
            foreach (string key in keys)
            {
                LoaderManager.Self.Dispose(key);
            }
        }

        private static bool IsSameFile(string path, FilePath file)
        {
            // Most keys are other files; comparing the names first avoids building a path for each of them.
            if (!path.EndsWith(System.IO.Path.GetFileName(file.FullPath), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                return new FilePath(path) == file;
            }
            catch
            {
                // An invalid path in a variable cannot be the file that changed.
                return false;
            }
        }

        /// <summary>
        /// True when some element in the project references <paramref name="file"/> (a variable's value, an
        /// animation chain's frame, a font's page), wherever it lives. The walk of the whole project is kept
        /// until <see cref="InvalidateReferencedFiles"/>.
        /// </summary>
        public bool IsReferencedByProject(FilePath file) => _referencedFiles.Contains(file);

        /// <summary>
        /// Forgets the kept set of referenced files. Called whenever what the project references may have
        /// changed: an element, behavior or project reload here, and an edit made in the tool.
        /// </summary>
        public void InvalidateReferencedFiles() => _referencedFiles.Invalidate();

        private void ReactToElementSaveChanged(FilePath file)
        {
            // What the project references may have changed.
            _referencedFiles.Invalidate();

            var projectDirectory = _fileCommands.ProjectDirectory;
            var relativeToFolderForType = GetElementNameForElementFile(file, projectDirectory);
            ////////////////////////Early Out////////////////////////////
            if (relativeToFolderForType == null) return;
            ///////////////////////End Early Out/////////////////////////

            var element = ObjectFinder.Self.GetElementSave(relativeToFolderForType);

            var refreshingSelected = element == _selectedState.SelectedElement;
            bool dependentsUpdated = false;

            if (element != null && _unsavedChangesTracker.HasUnsavedChanges(element)
                && UserKeepsUnsavedChanges(element.Name, file))
            {
                KeepUnsavedElement(element);
                return;
            }

            if(element != null)
            {
                // The copy the reload replaces, to find what its behaviors materialized and no longer do.
                ElementSave previousElement = element;
                // File changes are only watched while a project is open.
                var gumProject = _projectState.GetLoadedProject();
                gumProject.ReloadElement(element);
                gumProject.Initialize();
                _standardElementsManagerGumTool.FixCustomTypeConverters(gumProject);


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
                // The reloaded file can hold an element under another name.
                if (element != null)
                {
                    _pluginManager.ElementReloaded(element);

                    if (_reloadedWhileDeferring != null)
                    {
                        _reloadedWhileDeferring.TryAdd(element.Name, previousElement);
                    }
                    else
                    {
                        dependentsUpdated = UpdateDependentsOf(
                            new Dictionary<string, ElementSave> { [element.Name] = previousElement },
                            new Dictionary<string, BehaviorSave>());
                    }
                }
            }

            // Elements reading the reloaded one changed their stored values, and may be the selected
            // element or shown by instances the check below does not look at.
            bool shouldReloadWireframe = dependentsUpdated;

            var currentElement = _selectedState.SelectedElement;

            if(currentElement != null)
            {
                var currentElementFile = _fileCommands.GetFullPathXmlFile(currentElement, currentElement.Name);
                shouldReloadWireframe |= currentElementFile == file;
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
                // todo - this isn't working if I rename a variable...
                RefreshCanvasAndVariables();
            }
        }

        /// <summary>
        /// Asks whether to reload <paramref name="file"/> over the unsaved edits of
        /// <paramref name="name"/>; true when the user keeps their edits.
        /// </summary>
        private bool UserKeepsUnsavedChanges(string name, FilePath file)
        {
            string relativeFile = file.RelativeTo(_fileCommands.ProjectDirectory).Replace("\\", "/");
            MessageDialogResult answer = _dialogService.ShowMessage(
                BuildUnsavedChangesPromptMessage(name, relativeFile),
                UnsavedChangesPromptTitle,
                CreateUnsavedChangesPromptStyle());
            return answer != MessageDialogResult.Affirmative;
        }

        /// <summary>How the prompt names the project when its file changes under unsaved edits.</summary>
        public const string ProjectPromptName = "the project";

        /// <summary>Title of the prompt shown when a file changes on disk under unsaved edits (#5379).</summary>
        public const string UnsavedChangesPromptTitle = "File changed outside Gum";

        /// <summary>
        /// The prompt shown when <paramref name="relativeFile"/> changes on disk while
        /// <paramref name="name"/> (an element, a behavior or <see cref="ProjectPromptName"/>) has
        /// unsaved edits in the tool (Auto Save off).
        /// </summary>
        public static string BuildUnsavedChangesPromptMessage(string name, string relativeFile) =>
            $"{relativeFile} was changed outside Gum, and {name} has unsaved changes.\n\n" +
            "Reload it from disk and lose your changes, or keep your version? " +
            "If you keep it, your next save overwrites the change made outside Gum.";

        /// <summary>The buttons of the prompt: reload is affirmative, keep is negative.</summary>
        public static MessageDialogStyle CreateUnsavedChangesPromptStyle() => new MessageDialogStyle
        {
            AffirmativeText = "Reload from disk",
            NegativeText = "Keep my changes",
        };

        private void KeepUnsavedElement(ElementSave element)
        {
            // The file is on disk again, so the kept element saves over it instead of being
            // refused as missing.
            if (element.IsSourceFileMissing)
            {
                element.IsSourceFileMissing = false;
                _guiCommands.RefreshElementTreeView();
                _pluginManager.ElementReloaded(element);
            }
        }

        private void ReactToBehaviorChanged(FilePath file)
        {
            _referencedFiles.Invalidate();
            // File changes are only watched while a project is open.
            var gumProject = _projectState.GetLoadedProject();
            var behavior = gumProject.Behaviors.FirstOrDefault(item =>
            // It's somehow possible for behaviors with no name to make it in the project. let's tolerate it
                string.Equals(item?.Name, file.CaseSensitiveNoPathNoExtension, StringComparison.OrdinalIgnoreCase));

            var refreshingSelected = behavior == _selectedState.SelectedBehavior;

            if (behavior != null && _unsavedChangesTracker.HasUnsavedChanges(behavior)
                && UserKeepsUnsavedChanges(behavior.Name, file))
            {
                return;
            }

            if (behavior != null)
            {
                // The copy the reload replaces, to find the variables its removed lines materialized.
                BehaviorSave previousBehavior = behavior;
                gumProject.ReloadBehavior(behavior);
                gumProject.Initialize();
                _standardElementsManagerGumTool.FixCustomTypeConverters(gumProject);

                if (refreshingSelected)
                {
                    _selectedState.SelectedBehavior = null;
                }
                _guiCommands.RefreshElementTreeView();

                if (refreshingSelected)
                {
                    behavior = gumProject.Behaviors.FirstOrDefault(item =>
                        string.Equals(item.Name, file.CaseSensitiveNoPathNoExtension, StringComparison.OrdinalIgnoreCase));
                    _selectedState.SelectedBehavior = behavior;

                    _guiCommands.RefreshVariables(force: true);
                }

                // The behavior itself does not show on the canvas, but the values its tool-only references
                // materialize in the components implementing it, and in their instances, do.
                if (_behaviorsReloadedWhileDeferring != null)
                {
                    _behaviorsReloadedWhileDeferring.TryAdd(file.CaseSensitiveNoPathNoExtension, previousBehavior);
                }
                else if (UpdateDependentsOf(
                    new Dictionary<string, ElementSave>(),
                    new Dictionary<string, BehaviorSave> { [file.CaseSensitiveNoPathNoExtension] = previousBehavior }))
                {
                    RefreshCanvasAndVariables();
                }
            }
        }

    }
}
