#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

// Companion types to Gum.GumService (issue #3119) — hot reload is wired via
// GumService.EnableHotReload, so these live in the same namespace.
namespace Gum;

#if !IOS && !ANDROID

/// <summary>
/// Watches a Gum project directory for file changes and triggers a reload when relevant files are modified.
/// </summary>
public interface IGumHotReloadManager
{
    /// <summary>
    /// Raised after a reload has been performed in response to a file change.
    /// </summary>
    event Action? ReloadCompleted;

    /// <summary>
    /// Raised after every reload attempt, successful or not, right after <see cref="ReloadCompleted"/>
    /// when that is raised. The result lists every issue present in that reload, even one an earlier
    /// reload already had; only the console output is once-only.
    /// </summary>
    event Action<HotReloadResult>? ReloadFinished;

    /// <summary>
    /// Starts watching the directory containing the specified .gumx project file for changes.
    /// This is not typically called directly — use <c>GumServiceSkiaBase.EnableHotReload</c>
    /// or the MonoGame/Raylib GumService's equivalent instead.
    /// </summary>
    /// <param name="absoluteGumxSourcePath">The absolute path to the .gumx project file.</param>
    void Start(string absoluteGumxSourcePath);

    /// <summary>
    /// Stops watching for file changes and releases the underlying file system watcher.
    /// </summary>
    void Stop();

    /// <summary>
    /// Checks for a pending reload and, if enough time has elapsed since the last file change, performs the reload.
    /// This is not typically called directly — the owning service calls this automatically each frame.
    /// </summary>
    /// <param name="roots">The roots whose direct children will be rebuilt from the reloaded project.
    /// Typically the same collection passed to the owning service's per-frame Update.</param>
    void Update(IEnumerable<GraphicalUiElement> roots);
}

/// <summary>
/// Default implementation of <see cref="IGumHotReloadManager"/>. Watches Gum project files on disk
/// and hot-reloads the element tree when changes are detected.
/// </summary>
/// <remarks>
/// Lives in GumCommon (not a MonoGame/Raylib-specific project) so any host — including Skia-family
/// hosts via <c>GumServiceSkiaBase</c> — can reuse it (issue #4452). The two platform-specific
/// operations a reload needs to re-run (applying the project's texture filter, and loading
/// *Animations.ganx/.ganj files) are supplied by the owning service via the constructor instead of
/// being hardcoded to a specific concrete GumService type.
/// </remarks>
public class GumHotReloadManager : IGumHotReloadManager
{
    private readonly Action<GumProjectSave> _applyProjectTextureFilter;
    private readonly Func<GumProjectSave, Gum.Bundle.IGumFileProvider, int> _loadAnimationsFromProvider;
    private readonly Action<string> _disposeCachedAsset;
    private readonly Func<DateTime> _utcNow;

    private string _projectSourcePath = "";
    private string _binGumDirectory = "";
    private FileSystemWatcher? _watcher;
    // The watcher thread writes these and the game thread reads and clears them, always together
    // under _pendingReloadLock, so Update never sees a pending change with an older timestamp and
    // a change recorded while Update decides is not cleared unseen.
    private bool _pendingReload;
    private DateTime _lastChangeTime;
    private readonly object _pendingReloadLock = new object();
    // Consecutive reloads that failed to read the project; reset by any new file change.
    private int _failedAttempts;
    // Problems the last reload printed (a visual whose element is gone). A problem is printed once,
    // not on every later save, and printed again if it goes away and comes back; game thread only.
    // It never filters a reload's result, which lists every issue of that reload.
    private readonly HashSet<string> _reportedProblems = new HashSet<string>();
    private readonly List<string> _changedFontFiles = new List<string>();
    private readonly object _fontFileLock = new object();
    // Changed files that are not project/element/font files. Each is checked against what the loaded
    // visuals reference once the debounce passes; guarded by _pendingReloadLock.
    private readonly HashSet<string> _changedOtherFiles = new HashSet<string>();
    // Separate from _lastChangeTime so editor temp files and the like cannot push a pending project
    // reload past its debounce.
    private DateTime _lastOtherChangeTime;

    /// <param name="applyProjectTextureFilter">
    /// Applies the reloaded project's <see cref="GumProjectSave.TextureFilter"/> to the host's
    /// renderer — the same operation the host's own Initialize runs on first load. Each host's
    /// GumService already implements this (e.g. <c>GumService.ApplyProjectTextureFilter</c>).
    /// </param>
    /// <param name="loadAnimationsFromProvider">
    /// Loads *Animations.ganx/.ganj files from the given file provider into the reloaded project.
    /// Each host's GumService already implements this (e.g. <c>GumService.LoadAnimationsFromProvider</c>).
    /// </param>
    /// <param name="disposeCachedAsset">
    /// Evicts a standardized (absolute, case-preserved) file path from the host's content cache, so a
    /// changed font/texture reloads from disk instead of returning the stale cached version. The
    /// concrete cache (<c>RenderingLibrary.Content.LoaderManager</c>) is an engine-specific type not
    /// available in GumCommon, so the host supplies this hook (e.g.
    /// <c>path => RenderingLibrary.Content.LoaderManager.Self.Dispose(path)</c>).
    /// </param>
    public GumHotReloadManager(
        Action<GumProjectSave> applyProjectTextureFilter,
        Func<GumProjectSave, Gum.Bundle.IGumFileProvider, int> loadAnimationsFromProvider,
        Action<string> disposeCachedAsset)
        : this(applyProjectTextureFilter, loadAnimationsFromProvider, disposeCachedAsset, () => DateTime.UtcNow)
    {
    }

    // Takes the clock so tests can pin the debounce without waiting on real time.
    internal GumHotReloadManager(
        Action<GumProjectSave> applyProjectTextureFilter,
        Func<GumProjectSave, Gum.Bundle.IGumFileProvider, int> loadAnimationsFromProvider,
        Action<string> disposeCachedAsset,
        Func<DateTime> utcNow)
    {
        _applyProjectTextureFilter = applyProjectTextureFilter;
        _loadAnimationsFromProvider = loadAnimationsFromProvider;
        _disposeCachedAsset = disposeCachedAsset;
        _utcNow = utcNow;
    }

    /// <inheritdoc/>
    public event Action? ReloadCompleted;

    /// <inheritdoc/>
    public event Action<HotReloadResult>? ReloadFinished;

    /// <inheritdoc/>
    public void Start(string absoluteGumxSourcePath)
    {
        _projectSourcePath = absoluteGumxSourcePath;
        _binGumDirectory = ToolsUtilities.FileManager.RelativeDirectory;

        var directory = Path.GetDirectoryName(absoluteGumxSourcePath)
            ?? throw new ArgumentException("Cannot determine directory from path.", nameof(absoluteGumxSourcePath));

        _watcher = new FileSystemWatcher(directory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _watcher.Changed += HandleFileChange;
        _watcher.Created += HandleFileChange;
        _watcher.Renamed += (sender, e) => HandleFileChange(sender, e);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    /// <inheritdoc/>
    public void Update(IEnumerable<GraphicalUiElement> roots)
    {
        DateTime now = _utcNow();
        bool reload;
        List<string> otherFiles = new List<string>();
        lock (_pendingReloadLock)
        {
            TimeSpan debounce = TimeSpan.FromMilliseconds(200);
            reload = _pendingReload && (now - _lastChangeTime) >= debounce;
            bool checkOtherFiles = _changedOtherFiles.Count > 0 && (now - _lastOtherChangeTime) >= debounce;
            if (!reload && !checkOtherFiles)
            {
                return;
            }
            if (reload)
            {
                _pendingReload = false;
            }
            if (checkOtherFiles)
            {
                otherFiles = _changedOtherFiles.ToList();
                _changedOtherFiles.Clear();
            }
        }

        List<GraphicalUiElement> rootList = roots.ToList();
        if (otherFiles.Count > 0)
        {
            ReassignShaderFiles(rootList, otherFiles);
        }
        if (reload)
        {
            PerformReload(rootList);
        }
    }

    private void HandleFileChange(object sender, FileSystemEventArgs e) => NotifyFileChanged(e.FullPath);

    // Called from the watcher's thread-pool thread; internal so tests can drive it without a
    // FileSystemWatcher.
    internal void NotifyFileChanged(string fullPath)
    {
        string extension = Path.GetExtension(fullPath).ToLowerInvariant();
        if (IsWatchedExtension(extension))
        {
            if (extension == ".fnt")
            {
                lock (_fontFileLock)
                {
                    _changedFontFiles.Add(fullPath);
                }
            }

            DateTime now = _utcNow();
            lock (_pendingReloadLock)
            {
                _lastChangeTime = now;
                _pendingReload = true;
                _failedAttempts = 0;
            }
        }
        else if (extension.Length > 0)
        {
            // Not a file Gum owns, but an element may reference it (a shader, in any format).
            DateTime now = _utcNow();
            lock (_pendingReloadLock)
            {
                _lastOtherChangeTime = now;
                _changedOtherFiles.Add(fullPath);
            }
        }
    }

    // Re-assigns SourceShaderFile on every container using a changed file, so each looks its effect up
    // again. The tree is not rebuilt. The rule is "an element references the file", so no extension list.
    private void ReassignShaderFiles(List<GraphicalUiElement> roots, List<string> changedFiles)
    {
        HashSet<string> changed = new HashSet<string>(
            changedFiles.Select(file => ToolsUtilities.FileManager.Standardize(file, preserveCase: false, makeAbsolute: true)));

        List<GraphicalUiElement> visited = new List<GraphicalUiElement>();
        foreach (GraphicalUiElement root in roots)
        {
            CollectDescendants(root, visited);
        }

        foreach (GraphicalUiElement element in visited)
        {
            if (element.RenderableComponent is not IRenderTargetRenderable renderTarget)
            {
                continue;
            }

            string? shaderFile = ShaderParameterVariables.GetShaderFile(renderTarget);
            if (shaderFile == null)
            {
                continue;
            }

            string? cacheKey = null;
            try
            {
                cacheKey = ToolsUtilities.FileManager.IsRelative(shaderFile) && !ToolsUtilities.FileManager.IsUrl(shaderFile)
                    ? ToolsUtilities.FileManager.RemoveDotDotSlash(ToolsUtilities.FileManager.RelativeDirectory + shaderFile)
                    : shaderFile;
                string standardized = ToolsUtilities.FileManager.Standardize(cacheKey, preserveCase: false, makeAbsolute: true);
                if (!changed.Contains(standardized))
                {
                    continue;
                }
            }
            catch (ArgumentException)
            {
                // An invalid path in a variable cannot be the file that changed.
                continue;
            }

            // The effect is cached by the path the container resolved; drop it so the assignment compiles again.
            _disposeCachedAsset(cacheKey);
            _disposeCachedAsset(shaderFile);
            element.SetProperty("SourceShaderFile", shaderFile);
        }
    }

    private static void CollectDescendants(GraphicalUiElement element, List<GraphicalUiElement> into)
    {
        into.Add(element);
        foreach (GraphicalUiElement child in ContainedAndChildren(element))
        {
            CollectDescendants(child, into);
        }
    }

    /// <summary>
    /// True when <paramref name="extension"/> (including the leading dot, lower-invariant) is a file
    /// type that should trigger a hot reload - both the XML and JSON forms of the project, element,
    /// animation, and behavior formats, plus bitmap fonts. Internal (not private) so tests can pin
    /// the watched set directly; the actual reload dispatch (<c>GumProjectSave.Load</c>,
    /// the injected animation loader) already handles both XML and JSON content, so
    /// this gate is the only place that needed the JSON siblings added (issue #4182).
    /// </summary>
    internal static bool IsWatchedExtension(string extension) =>
        extension is ".gumx" or ".gumj"
            or ".gucx" or ".gucj"
            or ".gusx" or ".gusj"
            or ".gutx" or ".gutj"
            or ".ganx" or ".ganj"
            or ".behx" or ".behj"
            or ".fnt";

    // Returns a description of the fonts that could not be copied (a file the editor still has open),
    // which stay queued for the next reload; null when every font copied.
    private string? CopyAndUnloadChangedFonts()
    {
        List<string> changedFonts;
        lock (_fontFileLock)
        {
            changedFonts = new List<string>(_changedFontFiles);
            _changedFontFiles.Clear();
        }

        if (changedFonts.Count == 0)
        {
            return null;
        }

        var sourceDirectory = Path.GetDirectoryName(_projectSourcePath);
        if (sourceDirectory == null)
        {
            return null;
        }

        var sourceFontCache = Path.Combine(sourceDirectory, "FontCache");
        if (!Directory.Exists(sourceFontCache))
        {
            return null;
        }

        string? failures = null;
        foreach (var sourceFntPath in changedFonts)
        {
            try
            {
                CopyAndUnloadFont(sourceFntPath, sourceFontCache);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lock (_fontFileLock)
                {
                    _changedFontFiles.Add(sourceFntPath);
                }
                failures += $"\nCould not copy font file {sourceFntPath}: {exception.Message}";
            }
        }

        return failures;
    }

    private void CopyAndUnloadFont(string sourceFntPath, string sourceFontCache)
    {
        var destinationFontCache = Path.Combine(_binGumDirectory, "FontCache");
        Directory.CreateDirectory(destinationFontCache);

        var fileName = Path.GetFileName(sourceFntPath);

        // Copy the .fnt file
        var destinationFile = Path.Combine(destinationFontCache, fileName);
        File.Copy(sourceFntPath, destinationFile, overwrite: true);

        // Copy associated .png texture pages (same name prefix)
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var copiedPngs = new List<string>();
        foreach (var pngFile in Directory.GetFiles(sourceFontCache, baseName + "*.png"))
        {
            var pngDestination = Path.Combine(destinationFontCache, Path.GetFileName(pngFile));
            File.Copy(pngFile, pngDestination, overwrite: true);
            copiedPngs.Add(pngDestination);
        }

        // Unload the cached font so it gets reloaded from the new file
        var absoluteFontPath = Path.Combine(destinationFontCache, fileName);
        var standardizedFont = ToolsUtilities.FileManager.Standardize(absoluteFontPath, preserveCase: true, makeAbsolute: true);
        _disposeCachedAsset(standardizedFont);

        // Unload the cached texture pages so they get reloaded too
        foreach (var pngPath in copiedPngs)
        {
            var standardizedPng = ToolsUtilities.FileManager.Standardize(pngPath, preserveCase: true, makeAbsolute: true);
            _disposeCachedAsset(standardizedPng);
        }
    }

    // internal (not private) so tests can drive a reload deterministically without the
    // FileSystemWatcher + debounce. ApplyDiff is exposed for tests for the same reason.
    internal void PerformReload(IEnumerable<GraphicalUiElement> roots)
    {
        List<HotReloadIssue> issues = new List<HotReloadIssue>();

        // Materialize so we can iterate twice and so the caller's collection
        // is safe from us modifying it via the rebuild step.
        bool applied = TryReload(roots.ToList(), issues);

        if (applied)
        {
            ReloadCompleted?.Invoke();
        }
        ReloadFinished?.Invoke(new HotReloadResult(issues));
    }

    // Returns whether the reload was applied; every issue the attempt had is added to issues.
    private bool TryReload(List<GraphicalUiElement> rootList, List<HotReloadIssue> issues)
    {
        string? fontFailure = CopyAndUnloadChangedFonts();

        // The whole project is read into a temporary first. A file caught half written, locked by the
        // editor or unparsable leaves the running tree and ObjectFinder exactly as they were (#6978).
        GumProjectSave? newProject = TryLoadProject(issues, out string? loadFailure);
        if (newProject == null)
        {
            HandleFailedReload(loadFailure, fontFailure, issues);
            return false;
        }

        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        List<HotReloadIssue> diffIssues = new List<HotReloadIssue>();
        try
        {
            newProject.Initialize();
            ObjectFinder.Self.GumProjectSave = newProject;

            // Re-apply the project's texture filter so editing it in the tool carries over on reload
            // (issue #3199). On XNALIKE this takes effect on the next Draw; on raylib it affects
            // textures loaded after this point (already-cached textures keep their prior filter).
            _applyProjectTextureFilter(newProject);

            // Reload animations from the source project directory (loose files on disk) by enumerating
            // its *Animations.ganx files. Hot reload only ever runs against a real filesystem, so a
            // LooseFileGumFileProvider rooted at the source directory is the right seam - no need to
            // mutate FileManager.RelativeDirectory the way the old per-element probe did.
            string? sourceDirectory = Path.GetDirectoryName(_projectSourcePath);
            Gum.Bundle.LooseFileGumFileProvider animationProvider = new Gum.Bundle.LooseFileGumFileProvider(
                string.IsNullOrEmpty(sourceDirectory) ? "." : sourceDirectory);
            _loadAnimationsFromProvider(newProject, animationProvider);

            ApplyDiff(rootList, newProject, ISystemManagers.Default!, previousProject, _reportedProblems, diffIssues);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
            if (previousProject != null)
            {
                _applyProjectTextureFilter(previousProject);
            }

            // What the walk found before it threw was not printed yet.
            PrintIssues(diffIssues, printedProblems: null);
            issues.AddRange(diffIssues);
            HandleFailedReload(
                $"Hot reload failed while applying {_projectSourcePath}: {exception.Message}\nThe UI may be partially updated; the next complete save reloads.",
                fontFailure,
                issues,
                uiUnchanged: false);
            return false;
        }

        issues.AddRange(diffIssues);
        if (fontFailure != null)
        {
            Report(issues, new HotReloadIssue(HotReloadIssueKind.FontFailure, fontFailure.Trim()));
        }

        return true;
    }

    // Returns null with a description when the project cannot be read. Files the project lists but that
    // are gone (a git checkout between branches) are reported and left out, so the rest still reloads.
    private GumProjectSave? TryLoadProject(List<HotReloadIssue> issues, out string? failure)
    {
        failure = null;
        GumProjectSave? project;
        GumLoadResult result;
        try
        {
            project = GumProjectSave.Load(_projectSourcePath, out result);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            failure = $"Could not read {_projectSourcePath}: {exception.Message}";
            return null;
        }

        if (project == null || !string.IsNullOrEmpty(result.ErrorMessage))
        {
            failure = $"Could not read {_projectSourcePath}: {result.ErrorMessage?.Trim()}";
            if (result.MissingFiles?.Count > 0)
            {
                failure += "\nMissing: " + string.Join(", ", result.MissingFiles);
            }
            return null;
        }

        if (result.MissingFiles?.Count > 0)
        {
            Report(issues, new HotReloadIssue(
                HotReloadIssueKind.MissingFile,
                "Hot reload left out missing file(s): " + string.Join(", ", result.MissingFiles)));
        }

        return project;
    }

    // A failed read is tried once more after the debounce, since the writer may still have had the file
    // open; a second failure is reported and waits for the next save.
    private void HandleFailedReload(
        string? failure, string? fontFailure, List<HotReloadIssue> issues, bool uiUnchanged = true)
    {
        bool retry;
        lock (_pendingReloadLock)
        {
            _failedAttempts++;
            retry = _failedAttempts < 2;
            if (retry)
            {
                _lastChangeTime = _utcNow();
                _pendingReload = true;
            }
        }

        // The result says so on every attempt; the console only on the one that is final.
        string prefix = uiUnchanged ? "Hot reload skipped, the running UI is unchanged. " : "";
        HotReloadIssue issue = new HotReloadIssue(HotReloadIssueKind.ReloadFailed, (prefix + failure + fontFailure).Trim());
        if (retry)
        {
            issues.Add(issue);
        }
        else
        {
            Report(issues, issue);
        }
    }

    // Adds the issue to the reload's result and prints it.
    private static void Report(List<HotReloadIssue> issues, HotReloadIssue issue)
    {
        issues.Add(issue);
        ReportError(issue.Message);
    }

    private static void ReportError(string message)
    {
        try
        {
            if (ElementSaveExtensions.ReportPropertyAssignmentError != null)
            {
                ElementSaveExtensions.ReportPropertyAssignmentError(message);
            }
            else
            {
                Console.Error.WriteLine($"[Gum] {message}");
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"[Gum] A PropertyAssignmentError subscriber threw while reporting a hot reload failure: {exception}");
        }
    }

    /// <summary>
    /// Applies a structural + variable diff from <paramref name="newProject"/> onto the live
    /// visual trees in <paramref name="roots"/>. For every visual whose ElementSave name matches
    /// an element in the project, design-time children (those whose <c>Tag</c> is an
    /// <see cref="InstanceSave"/>) are added, removed, retyped, and reordered to match the
    /// project's <c>Instances</c> list, and the new default-state variables are re-applied.
    /// Runtime-added children (no <c>InstanceSave</c> tag) are left untouched. Primarily
    /// called by <see cref="PerformReload"/>; exposed publicly for tests. While it runs,
    /// <c>ObjectFinder.Self.GumProjectSave</c> is <paramref name="newProject"/>; the previous project is
    /// restored when it returns, so the caller decides afterward which project the finder holds.
    /// </summary>
    public static HotReloadResult ApplyDiff(
        IEnumerable<GraphicalUiElement> roots,
        GumProjectSave newProject,
        ISystemManagers systemManagers)
    {
        return ApplyDiff(roots, newProject, systemManagers, ObjectFinder.Self.GumProjectSave);
    }

    /// <summary>
    /// Same as the three-argument overload, with the project the live visuals were built from.
    /// A reload compares it with <paramref name="newProject"/> to decide which variables the file
    /// changed; a variable it did not change keeps the value the game gave it (#6980). Without it
    /// (null, or the same object as the new project) the comparison uses what the visuals still
    /// hold of the old project, and an element it cannot find an old version of is applied in full.
    /// </summary>
    public static HotReloadResult ApplyDiff(
        IEnumerable<GraphicalUiElement> roots,
        GumProjectSave newProject,
        ISystemManagers systemManagers,
        GumProjectSave? oldProject)
    {
        List<HotReloadIssue> issues = new List<HotReloadIssue>();
        ApplyDiff(roots, newProject, systemManagers, oldProject, printedProblems: null, issues);
        return new HotReloadResult(issues);
    }

    // printedProblems is the manager's memory of what it already printed; null prints every problem.
    // It only decides what is printed: every issue of this walk lands in issues either way.
    private static void ApplyDiff(
        IEnumerable<GraphicalUiElement> roots,
        GumProjectSave newProject,
        ISystemManagers systemManagers,
        GumProjectSave? oldProject,
        HashSet<string>? printedProblems,
        List<HotReloadIssue> issues)
    {
        // References and base types resolve through ObjectFinder, so for this call it holds the new
        // project whatever it held before, and the caller's project is put back afterward.
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = newProject;
        try
        {
            ApplyDiffToNewProject(roots, newProject, systemManagers, oldProject, printedProblems, issues);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }

    private static void ApplyDiffToNewProject(
        IEnumerable<GraphicalUiElement> roots,
        GumProjectSave newProject,
        ISystemManagers systemManagers,
        GumProjectSave? oldProject,
        HashSet<string>? printedProblems,
        List<HotReloadIssue> issues)
    {
        // A file the tool did not save together with the files that read it (a text-editor edit of one
        // swatch) leaves the readers holding the value they were saved with. A reference sets the final
        // value, so the rows are evaluated in dependency order against the new project first, the way
        // a tool save does, and a reference that reads another reference sees the end of the chain (#6853).
        newProject.ApplyAllVariableReferences();

        Dictionary<string, ElementSave> byName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (ElementSave element in newProject.AllElements)
        {
            byName[element.Name] = element;
        }

        List<GraphicalUiElement> rootList = roots.ToList();

        ReloadContext context = new ReloadContext(byName, oldProject, newProject, rootList, issues);

        // The same three passes RefreshStyles runs, so a control keeps what it holds in code and the
        // state it is in: save before anything is re-applied, restore after everything is (#6855).
        foreach (GraphicalUiElement root in rootList)
        {
            root.SaveFormsRuntimePropertiesRecursive();
        }

        foreach (GraphicalUiElement root in rootList)
        {
            ApplyDiffRecursive(root, byName, systemManagers, context);
        }

        // Re-applying a default state overwrites what a categorical state set, so each visual goes
        // back into the state it was in: first the ones code drove by name, then the Forms controls,
        // which work out their own state (disabled, hovered, checked) and restore what code assigned.
        foreach (GraphicalUiElement root in rootList)
        {
            root.ReapplyAppliedCategoryStatesRecursive();
        }

        foreach (GraphicalUiElement root in rootList)
        {
            root.RestoreFormsRuntimePropertiesRecursive();
        }

        PrintIssues(issues, printedProblems);
    }

    // Printing is the only place that remembers what was already said: a shown element that stays gone
    // is printed on the first reload and not on every later save, but it is an issue of each result.
    private static void PrintIssues(List<HotReloadIssue> issues, HashSet<string>? printedProblems)
    {
        HashSet<string> current = new HashSet<string>();
        foreach (HotReloadIssue issue in issues)
        {
            string? key = issue.Kind == HotReloadIssueKind.ShownElementGone ? "gone:" + issue.ElementName : null;
            if (key != null)
            {
                current.Add(key);
            }

            if (key == null || printedProblems == null || !printedProblems.Contains(key))
            {
                ReportError(issue.Message);
            }
        }

        if (printedProblems != null)
        {
            printedProblems.Clear();
            printedProblems.UnionWith(current);
        }
    }

    private static string KindOf(ElementSave element) => element switch
    {
        ScreenSave => "screen",
        ComponentSave => "component",
        _ => "element"
    };

    /// <summary>
    /// Points the visual's state and category dictionaries at the new element's save objects. A
    /// visual keeps the instances it was created with, so without this a state applied after the
    /// reload (a mouse over, then off) would use the values from before the edit. Entries that came
    /// from an element of the replaced project and are gone from the new file are removed; entries
    /// added in code, which belong to no project element, are kept.
    /// </summary>
    private static void RefreshStatesAndCategories(
        GraphicalUiElement element,
        ElementSave newEs,
        Dictionary<string, ElementSave> byName)
    {
        bool IsFromReplacedElement(ElementSave? owner) =>
            owner != null
            && !(byName.TryGetValue(owner.Name, out ElementSave? current) && ReferenceEquals(current, owner));

        foreach (KeyValuePair<string, StateSaveCategory> category in element.Categories.ToList())
        {
            if (IsFromReplacedElement(category.Value.States.FirstOrDefault()?.ParentContainer))
            {
                element.Categories.Remove(category.Key);
            }
        }

        foreach (KeyValuePair<string, StateSave> state in element.States.ToList())
        {
            if (IsFromReplacedElement(state.Value.ParentContainer))
            {
                element.States.Remove(state.Key);
            }
        }

        // Resolves base types through ObjectFinder, which a reload has already pointed at the new project.
        element.AddStatesAndCategoriesRecursivelyToGue(newEs);
    }

    private static void ApplyDiffRecursive(
        GraphicalUiElement element,
        Dictionary<string, ElementSave> byName,
        ISystemManagers systemManagers,
        ReloadContext context)
    {
        string? name = element.ElementSave?.Name;
        HashSet<string>? designTimeChildNames = null;

        if (name != null && byName.TryGetValue(name, out ElementSave? newEs))
        {
            ElementSave? oldEs = element.ElementSave;
            element.ElementSave = newEs;
            RefreshStatesAndCategories(element, newEs, byName);

            DiffDesignTimeChildren(element, newEs, systemManagers, context);

            // An instance is built from its own element, so an edit to that element (a component's
            // default state or its own instances) has to reach it too, however deeply nested.
            // Innermost first: this element's overrides are applied next and must win over the
            // component's defaults.
            foreach (GraphicalUiElement instance in ContainedAndChildren(element))
            {
                if (instance.Tag is InstanceSave && instance.ElementSave is not StandardElementSave)
                {
                    ApplyDiffRecursive(instance, byName, systemManagers, context);
                }
            }

            if (newEs.DefaultState != null)
            {
                if (oldEs != null && !ReferenceEquals(oldEs, newEs))
                {
                    context.OldByName[newEs.Name] = oldEs;
                }
                SetChangedVariablesRecursively(element, newEs, context);
            }

            // After variables, so Parent variables have already moved each instance to its
            // final container.
            ReorderDesignTimeChildren(element, newEs.Instances ?? new List<InstanceSave>());

            if (newEs.Instances != null)
            {
                designTimeChildNames = new HashSet<string>(
                    newEs.Instances.Select(i => i.Name).Where(n => n != null)!,
                    StringComparer.OrdinalIgnoreCase);
            }
        }
        else if (name != null && element.ElementSave is ElementSave goneElement and not StandardElementSave)
        {
            // Nothing in the new project says what this visual should become. There is no identity
            // across a rename or a move to another folder, so it stays as it is and the game is told.
            string kind = KindOf(goneElement);
            context.ReportOnce("gone:" + name, new HotReloadIssue(
                HotReloadIssueKind.ShownElementGone,
                $"Hot reload: the {kind} '{name}' the game is showing is not in the project any more " +
                "(renamed, moved to another folder or deleted). It stays on screen as it was, and edits " +
                $"to it, or to what replaced it, will not reach the game until you restart it.",
                elementName: name));
        }

        foreach (GraphicalUiElement child in element.Children.ToList())
        {
            // Instances are handled by the element that owns them (above, or by the screen that
            // parented them here), so only runtime-added children are left.
            if (child.Tag is InstanceSave
                || (designTimeChildNames != null
                    && child.Name != null
                    && designTimeChildNames.Contains(child.Name)))
            {
                continue;
            }
            ApplyDiffRecursive(child, byName, systemManagers, context);
        }
    }

    /// <summary>
    /// What one reload knows while it walks the tree: the elements of the project the visuals were
    /// built from, the visuals it has rebuilt, and the variables it has already applied to each
    /// visual so a value the file did not change but a lower layer just reset can be put back.
    /// </summary>
    private sealed class ReloadContext
    {
        public Dictionary<string, ElementSave> OldByName { get; } =
            new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);

        public HashSet<GraphicalUiElement> Recreated { get; } = new HashSet<GraphicalUiElement>();

        // What this walk found, for the reload's result. The same problem is added once per walk.
        private readonly List<HotReloadIssue> _issues;
        private readonly HashSet<string> _issueKeys = new HashSet<string>();

        public Dictionary<GraphicalUiElement, HashSet<string>> Applied { get; } =
            new Dictionary<GraphicalUiElement, HashSet<string>>();

        public ReloadContext(
            Dictionary<string, ElementSave> newByName,
            GumProjectSave? oldProject,
            GumProjectSave newProject,
            List<GraphicalUiElement> roots,
            List<HotReloadIssue> issues)
        {
            _issues = issues;
            if (oldProject != null && !ReferenceEquals(oldProject, newProject))
            {
                foreach (ElementSave element in oldProject.AllElements)
                {
                    OldByName[element.Name] = element;
                }
            }

            // What the visuals still hold of the old project fills in when the caller did not give it.
            HashSet<GraphicalUiElement> visited = new HashSet<GraphicalUiElement>();
            foreach (GraphicalUiElement root in roots)
            {
                CollectOldElements(root, newByName, visited);
            }
        }

        private void CollectOldElements(
            GraphicalUiElement element,
            Dictionary<string, ElementSave> newByName,
            HashSet<GraphicalUiElement> visited)
        {
            if (!visited.Add(element))
            {
                return;
            }

            ElementSave? elementSave = element.ElementSave;
            if (elementSave != null
                && !OldByName.ContainsKey(elementSave.Name)
                && newByName.TryGetValue(elementSave.Name, out ElementSave? current)
                && !ReferenceEquals(current, elementSave))
            {
                OldByName[elementSave.Name] = elementSave;
            }

            foreach (GraphicalUiElement child in ContainedAndChildren(element))
            {
                CollectOldElements(child, newByName, visited);
            }
        }

        public void Report(HotReloadIssue issue) => _issues.Add(issue);

        // Adds the issue unless this walk already added one with the same key.
        public void ReportOnce(string key, HotReloadIssue issue)
        {
            if (_issueKeys.Add(key))
            {
                _issues.Add(issue);
            }
        }

        public void RecordApplied(GraphicalUiElement element, VariableSave variable)
        {
            if (!Applied.TryGetValue(element, out HashSet<string>? names))
            {
                names = new HashSet<string>(StringComparer.Ordinal);
                Applied[element] = names;
            }
            names.Add(variable.Name);
            names.Add(variable.GetRootName());
            if (!string.IsNullOrEmpty(variable.ExposedAsName))
            {
                names.Add(variable.ExposedAsName!);
            }
        }
    }

    /// <summary>
    /// Applies <paramref name="es"/>'s default state (and its base types') to <paramref name="element"/>
    /// the way <c>SetVariablesRecursively</c> does, except that a variable is only set when the file
    /// changed it: its value differs from the previous project's, it is new, or something earlier in
    /// this reload reset the live value (a lower layer or a rebuilt instance). Any other variable keeps
    /// what the game gave it. A layer with no previous version is applied in full. References are
    /// evaluated as ever, since a reference's source may have changed (#6980).
    /// </summary>
    private static void SetChangedVariablesRecursively(
        GraphicalUiElement element, ElementSave es, ReloadContext context)
    {
        StateSave? state = es.DefaultState;
        if (state == null)
        {
            return;
        }

        element.BeginReferenceLoad();
        try
        {
            if (!string.IsNullOrEmpty(es.BaseType))
            {
                ElementSave? baseElementSave = ObjectFinder.Self.GetBaseElement(es);
                if (baseElementSave != null)
                {
                    SetChangedVariablesRecursively(element, baseElementSave, context);
                }
            }

            StateSave? oldState = null;
            if (context.OldByName.TryGetValue(es.Name, out ElementSave? oldEs)
                && !ReferenceEquals(oldEs, es))
            {
                oldState = oldEs.DefaultState;
                // Lower layers were applied first, so a variable this layer dropped falls back to
                // what the layers below it say.
                ResetRemovedVariables(element, oldEs, es, context);
            }

            if (oldState == null || ReferenceEquals(oldState, state))
            {
                element.ApplyState(state);
                foreach (VariableSave variable in state.Variables.Where(v => v.SetsValue && v.Value != null))
                {
                    context.RecordApplied(element, variable);
                }
            }
            else
            {
                StateSave changed = CreateChangedState(element, state, oldState, context);
                element.ApplyState(changed);
                foreach (VariableSave variable in changed.Variables)
                {
                    context.RecordApplied(element, variable);
                }
            }

            element.ApplyVariableReferences(state);

            if (es is not StandardElementSave)
            {
                VariableReferenceGraph graph = VariableReferenceGraph.GetFor(es);
                if (graph.HasChains || graph.HasInheritedRows)
                {
                    graph.ApplyAll(es, element);
                }
            }
        }
        finally
        {
            element.EndReferenceLoad();
        }
    }

    private static StateSave CreateChangedState(
        GraphicalUiElement element, StateSave state, StateSave oldState, ReloadContext context)
    {
        Dictionary<string, VariableSave> oldByName = new Dictionary<string, VariableSave>(StringComparer.Ordinal);
        foreach (VariableSave oldVariable in oldState.Variables)
        {
            if (AppliesAValue(oldVariable))
            {
                oldByName[oldVariable.Name] = oldVariable;
            }
        }

        context.Applied.TryGetValue(element, out HashSet<string>? appliedToElement);

        StateSave changed = new StateSave { Name = state.Name, ParentContainer = state.ParentContainer };
        foreach (VariableSave variable in state.Variables)
        {
            if (!variable.SetsValue)
            {
                continue;
            }

            // A reparent is cheap and keeps the tree right after instances were rebuilt or moved.
            bool applies = !oldByName.TryGetValue(variable.Name, out VariableSave? oldVariable)
                || !ValuesMatch(oldVariable.Value, variable.Value)
                || variable.GetRootName() == "Parent";

            if (!applies)
            {
                string rootName = variable.GetRootName();
                if (string.IsNullOrEmpty(variable.SourceObject))
                {
                    // A lower layer (the base type) set this on the element earlier in this reload.
                    applies = appliedToElement != null && appliedToElement.Contains(rootName);
                }
                else
                {
                    GraphicalUiElement? target = element.GetGraphicalUiElementByName(variable.SourceObject);
                    applies = target != null
                        && (context.Recreated.Contains(target)
                            || (context.Applied.TryGetValue(target, out HashSet<string>? appliedToTarget)
                                && appliedToTarget.Contains(rootName)));
                }
            }

            if (applies)
            {
                changed.Variables.Add(variable);
            }
        }

        // Lists (variable references) are applied as ever.
        changed.VariableLists.AddRange(state.VariableLists);
        return changed;
    }

    private static bool ValuesMatch(object? oldValue, object? newValue)
    {
        if (Equals(oldValue, newValue))
        {
            return true;
        }
        if (oldValue == null || newValue == null || oldValue is string || newValue is string)
        {
            return false;
        }
        // A number read back from a file can arrive as another numeric type.
        return oldValue is IConvertible && newValue is IConvertible
            && string.Equals(
                Convert.ToString(oldValue, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(newValue, System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
    }

    /// <summary>
    /// Puts back the default of every variable the previous default state set and the new one no
    /// longer does. A reload only applies the variables the new file has, so without this a
    /// variable the file dropped (an instance detached from its parent, a width set back to its
    /// default) keeps its old value on the live visual. It compares the two save objects rather
    /// than the visuals, so a value assigned in code, which is in neither, is never touched.
    /// Runs before the new state is applied, so the new file's values win.
    /// </summary>
    private static void ResetRemovedVariables(
        GraphicalUiElement element, ElementSave oldEs, ElementSave newEs, ReloadContext context)
    {
        StateSave? oldState = oldEs.DefaultState;
        StateSave? newState = newEs.DefaultState;

        // The same state object means the project was edited in place, so the old values are gone.
        if (oldState == null || newState == null || ReferenceEquals(oldState, newState))
        {
            return;
        }

        // Only a variable that applies a value counts. The tool saves a detached instance's Parent
        // as a variable that is still marked as set but has no value, and applying a state skips
        // a variable with no value, so it changes nothing on the visual.
        HashSet<string> stillSet = new HashSet<string>(
            newState.Variables.Where(AppliesAValue).Select(v => v.Name),
            StringComparer.Ordinal);

        foreach (VariableSave variable in oldState.Variables)
        {
            if (!AppliesAValue(variable) || stillSet.Contains(variable.Name))
            {
                continue;
            }

            if (variable.GetRootName() == "Parent")
            {
                // An empty Parent value is a no-op, so detaching has to be done directly: an
                // instance with no Parent variable belongs to the element that contains it.
                if (!string.IsNullOrEmpty(variable.SourceObject))
                {
                    GraphicalUiElement? instance = element.GetGraphicalUiElementByName(variable.SourceObject);
                    if (instance != null)
                    {
                        instance.Parent = element;
                    }
                }
                continue;
            }

            // Un-applying a categorized state would mean undoing everything it set.
            if (variable.IsState(oldEs))
            {
                continue;
            }

            object? defaultValue = newState.GetValueRecursive(variable.Name);
            if (defaultValue != null)
            {
                element.SetProperty(variable.Name, defaultValue);

                // Layers above this one may still set the variable; they apply it again.
                GraphicalUiElement? owner = string.IsNullOrEmpty(variable.SourceObject)
                    ? element
                    : element.GetGraphicalUiElementByName(variable.SourceObject);
                if (owner != null)
                {
                    context.RecordApplied(owner, variable);
                }
            }
        }
    }

    // Whether applying a state would set this variable. An empty Parent sets nothing either, since
    // GraphicalUiElement ignores it.
    private static bool AppliesAValue(VariableSave variable)
    {
        if (!variable.SetsValue || variable.Value == null)
        {
            return false;
        }
        return !(variable.GetRootName() == "Parent" && variable.Value is string { Length: 0 });
    }

    /// <summary>
    /// Brings <paramref name="parent"/>'s design-time children (those tagged with an
    /// <see cref="InstanceSave"/>) into structural alignment with <paramref name="newEs"/>'s
    /// <c>Instances</c> list: removes missing instances, creates added ones, replaces visuals
    /// whose <c>BaseType</c> changed, and reorders the design-time slice to match the new
    /// order. Runtime-added children (no <c>InstanceSave</c> tag) are left in place.
    /// Reordering happens later, in <see cref="ReorderDesignTimeChildren"/>.
    /// </summary>
    private static void DiffDesignTimeChildren(
        GraphicalUiElement parent,
        ElementSave newEs,
        ISystemManagers systemManagers,
        ReloadContext context)
    {
        // Two lookup tables: one for design-time children (Tag is InstanceSave) so we can do
        // typed operations like retype/remove, and one keyed by Name across ALL children so
        // we don't duplicate a runtime-claimed child (e.g. one whose Tag was nulled by user
        // code — the documented limitation in issue #2848). ContainedElements is included
        // because an instance with a Parent variable (e.g. a ListBoxItem inside a ComboBox)
        // belongs to this element without being one of its direct Children.
        Dictionary<string, GraphicalUiElement> designTimeByName =
            new Dictionary<string, GraphicalUiElement>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, GraphicalUiElement> anyByName =
            new Dictionary<string, GraphicalUiElement>(StringComparer.OrdinalIgnoreCase);
        foreach (GraphicalUiElement child in ContainedAndChildren(parent))
        {
            // Only this element's own instances: a child that the screen parented under a
            // component instance carries the screen's InstanceSave, not the component's.
            if (child.Tag is InstanceSave existingInstance
                && existingInstance.Name != null
                && string.Equals(existingInstance.ParentContainer?.Name, newEs.Name, StringComparison.OrdinalIgnoreCase))
            {
                designTimeByName[existingInstance.Name] = child;
            }
            if (child.Name != null && !anyByName.ContainsKey(child.Name))
            {
                anyByName[child.Name] = child;
            }
        }

        List<InstanceSave> newInstances = newEs.Instances ?? new List<InstanceSave>();
        HashSet<string> newNames = new HashSet<string>(
            newInstances.Where(i => i.Name != null).Select(i => i.Name!),
            StringComparer.OrdinalIgnoreCase);

        void Forget(string name)
        {
            designTimeByName.Remove(name);
            anyByName.Remove(name);
        }

        GraphicalUiElement? CreateAndTrack(InstanceSave instance)
        {
            GraphicalUiElement? created = CreateAndAttach(parent, instance, systemManagers);
            if (created != null)
            {
                context.Recreated.Add(created);
                designTimeByName[instance.Name!] = created;
                anyByName[instance.Name!] = created;
            }
            return created;
        }

        foreach (KeyValuePair<string, GraphicalUiElement> kvp in designTimeByName.ToList())
        {
            if (!newNames.Contains(kvp.Key))
            {
                DetachAndRemove(kvp.Value);
                Forget(kvp.Key);
            }
        }

        foreach (InstanceSave newInstance in newInstances)
        {
            if (newInstance.Name == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(newInstance.BaseType)
                && ObjectFinder.Self.GetElementSave(newInstance.BaseType) == null)
            {
                // The file keeps an instance of a component that was deleted, and a restart cannot
                // build it. Without this the visual matches itself by its old element's name and stays.
                if (designTimeByName.TryGetValue(newInstance.Name, out GraphicalUiElement? orphan))
                {
                    DetachAndRemove(orphan);
                    Forget(newInstance.Name);
                    context.Report(new HotReloadIssue(
                        HotReloadIssueKind.InstanceRemoved,
                        $"Hot reload: '{newInstance.Name}' in '{newEs.Name}' is a '{newInstance.BaseType}', " +
                        "which is not in the project any more. It was removed from the running UI, as a restart would.",
                        newEs.Name,
                        newInstance.Name));
                }
                continue;
            }

            if (designTimeByName.TryGetValue(newInstance.Name, out GraphicalUiElement? existingChild))
            {
                // The existing visual's ElementSave reflects what it was actually built from;
                // comparing the new BaseType against that catches retypes regardless of whether
                // the caller passed a fresh project or mutated the existing one in place.
                string? existingBaseTypeName = existingChild.ElementSave?.Name;
                if (!string.Equals(existingBaseTypeName, newInstance.BaseType, StringComparison.Ordinal))
                {
                    DetachAndRemove(existingChild);
                    Forget(newInstance.Name);
                    if (CreateAndTrack(newInstance) != null)
                    {
                        ReportRebuilt(context, newEs, newInstance, existingBaseTypeName);
                    }
                }
                else
                {
                    existingChild.Tag = newInstance;
                }
            }
            else if (anyByName.ContainsKey(newInstance.Name))
            {
                // A non-design-time child already owns this name. User code is in control of
                // that visual (or its Tag was nulled). Don't create a duplicate — the parent's
                // qualified-name variable application will still hit it by Name during the
                // subsequent SetVariablesRecursively call.
                continue;
            }
            else
            {
                CreateAndTrack(newInstance);
            }
        }
    }

    // A component rename or a retype rebuilds the instance's visual. The tree is right, but code
    // references and Forms handlers on the old visual are stale, so the game is told once per pair
    // of components however many instances changed.
    private static void ReportRebuilt(
        ReloadContext context, ElementSave owner, InstanceSave instance, string? oldBaseType)
    {
        string was = string.IsNullOrEmpty(oldBaseType) ? "" : $" (was '{oldBaseType}')";
        context.ReportOnce(
            $"rebuilt:{oldBaseType}->{instance.BaseType}",
            new HotReloadIssue(
                HotReloadIssueKind.InstanceRebuilt,
                $"Hot reload: '{instance.Name}' in '{owner.Name}' is now a '{instance.BaseType}'{was}. " +
                "Its visual was rebuilt, so code references and Forms handlers on it are stale.",
                owner.Name,
                instance.Name));
    }

    // An instance with a Parent variable is in ContainedElements without being a direct child.
    private static List<GraphicalUiElement> ContainedAndChildren(GraphicalUiElement element) =>
        element.ContainedElements.Concat(element.Children).Distinct().ToList();

    private static void DetachAndRemove(GraphicalUiElement child)
    {
        child.Parent = null;
        child.ElementGueContainingThis = null;
        child.RemoveFromManagers();
    }

    private static GraphicalUiElement? CreateAndAttach(
        GraphicalUiElement parent,
        InstanceSave newInstance,
        ISystemManagers systemManagers)
    {
        GraphicalUiElement? newChild = newInstance.ToGraphicalUiElement(systemManagers);
        if (newChild == null)
        {
            return null;
        }

        newChild.Parent = parent;
        newChild.ElementGueContainingThis = parent;
        return newChild;
    }

    /// <summary>
    /// Reorders the design-time instances of <paramref name="element"/> so that, within each
    /// container they sit in, they appear in the same relative order as
    /// <paramref name="newInstances"/>. A container is the element itself or, for an instance
    /// with a Parent variable, another instance at any depth. Only the slots held by this
    /// element's instances are reshuffled: runtime-added children and instances that belong
    /// to a nested component keep their slots.
    /// </summary>
    private static void ReorderDesignTimeChildren(
        GraphicalUiElement element,
        IList<InstanceSave> newInstances)
    {
        Dictionary<InstanceSave, int> orderByInstance = new Dictionary<InstanceSave, int>();
        for (int i = 0; i < newInstances.Count; i++)
        {
            orderByInstance[newInstances[i]] = i;
        }

        HashSet<GraphicalUiElement> containers = new HashSet<GraphicalUiElement>();
        foreach (GraphicalUiElement child in ContainedAndChildren(element))
        {
            if (child.Tag is InstanceSave instance
                && orderByInstance.ContainsKey(instance)
                && child.Parent is GraphicalUiElement container)
            {
                containers.Add(container);
            }
        }

        foreach (GraphicalUiElement container in containers)
        {
            ReorderWithinContainer(container.Children, orderByInstance);
        }
    }

    private static void ReorderWithinContainer(
        ObservableCollection<GraphicalUiElement> children,
        Dictionary<InstanceSave, int> orderByInstance)
    {
        List<int> slots = new List<int>();
        List<GraphicalUiElement> desired = new List<GraphicalUiElement>();
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].Tag is InstanceSave instance && orderByInstance.ContainsKey(instance))
            {
                slots.Add(i);
                desired.Add(children[i]);
            }
        }

        desired.Sort((a, b) =>
            orderByInstance[(InstanceSave)a.Tag!].CompareTo(orderByInstance[(InstanceSave)b.Tag!]));

        List<GraphicalUiElement> final = children.ToList();
        for (int i = 0; i < slots.Count; i++)
        {
            final[slots[i]] = desired[i];
        }

        // Fill the prefix left to right; every Move pulls the wanted child back from a later index.
        for (int i = 0; i < final.Count; i++)
        {
            if (children[i] != final[i])
            {
                children.Move(children.IndexOf(final[i]), i);
            }
        }
    }
}
#endif // !IOS && !ANDROID
