#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
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
    private readonly List<string> _changedFontFiles = new List<string>();
    private readonly object _fontFileLock = new object();

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
        lock (_pendingReloadLock)
        {
            if (!_pendingReload || (now - _lastChangeTime) < TimeSpan.FromMilliseconds(200))
            {
                return;
            }
            _pendingReload = false;
        }
        PerformReload(roots);
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
            }
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

    private void CopyAndUnloadChangedFonts()
    {
        List<string> changedFonts;
        lock (_fontFileLock)
        {
            changedFonts = new List<string>(_changedFontFiles);
            _changedFontFiles.Clear();
        }

        if (changedFonts.Count == 0)
        {
            return;
        }

        var sourceDirectory = Path.GetDirectoryName(_projectSourcePath);
        if (sourceDirectory == null)
        {
            return;
        }

        var sourceFontCache = Path.Combine(sourceDirectory, "FontCache");
        if (!Directory.Exists(sourceFontCache))
        {
            return;
        }

        var destinationFontCache = Path.Combine(_binGumDirectory, "FontCache");
        Directory.CreateDirectory(destinationFontCache);

        foreach (var sourceFntPath in changedFonts)
        {
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
    }

    // internal (not private) so tests can drive a reload deterministically without the
    // FileSystemWatcher + debounce. ApplyDiff is exposed for tests for the same reason.
    internal void PerformReload(IEnumerable<GraphicalUiElement> roots)
    {
        // Materialize so we can iterate twice and so the caller's collection
        // is safe from us modifying it via the rebuild step.
        var rootList = roots.ToList();

        CopyAndUnloadChangedFonts();

        GumProjectSave newProject = GumProjectSave.Load(_projectSourcePath);
        newProject.Initialize();
        ObjectFinder.Self.GumProjectSave = newProject;

        // Re-apply the project's texture filter so editing it in the tool carries over on reload
        // (issue #3199). On XNALIKE this takes effect on the next Draw; on raylib it affects
        // textures loaded after this point (already-cached textures keep their prior filter).
        _applyProjectTextureFilter(newProject);

        // Reload animations from the source project directory (loose files on disk) by enumerating
        // its *Animations.ganx files. Hot reload only ever runs against a real filesystem, so a
        // LooseFileGumFileProvider rooted at the source directory is the right seam — no need to
        // mutate FileManager.RelativeDirectory the way the old per-element probe did.
        string? sourceDirectory = Path.GetDirectoryName(_projectSourcePath);
        Gum.Bundle.LooseFileGumFileProvider animationProvider = new Gum.Bundle.LooseFileGumFileProvider(
            string.IsNullOrEmpty(sourceDirectory) ? "." : sourceDirectory);
        _loadAnimationsFromProvider(newProject, animationProvider);

        ApplyDiff(rootList, newProject, ISystemManagers.Default!);

        ReloadCompleted?.Invoke();
    }

    /// <summary>
    /// Applies a structural + variable diff from <paramref name="newProject"/> onto the live
    /// visual trees in <paramref name="roots"/>. For every visual whose ElementSave name matches
    /// an element in the project, design-time children (those whose <c>Tag</c> is an
    /// <see cref="InstanceSave"/>) are added, removed, retyped, and reordered to match the
    /// project's <c>Instances</c> list, and the new default-state variables are re-applied.
    /// Runtime-added children (no <c>InstanceSave</c> tag) are left untouched. Primarily
    /// called by <see cref="PerformReload"/>; exposed publicly for tests.
    /// </summary>
    public static void ApplyDiff(
        IEnumerable<GraphicalUiElement> roots,
        GumProjectSave newProject,
        ISystemManagers systemManagers)
    {
        Dictionary<string, ElementSave> byName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (ElementSave element in newProject.AllElements)
        {
            byName[element.Name] = element;
        }

        foreach (GraphicalUiElement root in roots.ToList())
        {
            ApplyDiffRecursive(root, byName, systemManagers);
        }
    }

    private static void ApplyDiffRecursive(
        GraphicalUiElement element,
        Dictionary<string, ElementSave> byName,
        ISystemManagers systemManagers)
    {
        string? name = element.ElementSave?.Name;
        HashSet<string>? designTimeChildNames = null;

        if (name != null && byName.TryGetValue(name, out ElementSave? newEs))
        {
            element.ElementSave = newEs;

            DiffDesignTimeChildren(element, newEs, systemManagers);

            if (newEs.DefaultState != null)
            {
                element.SetVariablesRecursively(newEs, newEs.DefaultState);
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

        foreach (GraphicalUiElement child in element.Children.ToList())
        {
            if (designTimeChildNames != null
                && child.Name != null
                && designTimeChildNames.Contains(child.Name))
            {
                continue;
            }
            ApplyDiffRecursive(child, byName, systemManagers);
        }
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
        ISystemManagers systemManagers)
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
        foreach (GraphicalUiElement child in parent.ContainedElements.Concat(parent.Children).ToList())
        {
            if (child.Tag is InstanceSave existingInstance && existingInstance.Name != null)
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

        foreach (KeyValuePair<string, GraphicalUiElement> kvp in designTimeByName.ToList())
        {
            if (!newNames.Contains(kvp.Key))
            {
                DetachAndRemove(kvp.Value);
                designTimeByName.Remove(kvp.Key);
                anyByName.Remove(kvp.Key);
            }
        }

        foreach (InstanceSave newInstance in newInstances)
        {
            if (newInstance.Name == null)
            {
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
                    designTimeByName.Remove(newInstance.Name);
                    anyByName.Remove(newInstance.Name);
                    GraphicalUiElement? created = CreateAndAttach(parent, newInstance, systemManagers);
                    if (created != null)
                    {
                        designTimeByName[newInstance.Name] = created;
                        anyByName[newInstance.Name] = created;
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
                GraphicalUiElement? created = CreateAndAttach(parent, newInstance, systemManagers);
                if (created != null)
                {
                    designTimeByName[newInstance.Name] = created;
                    anyByName[newInstance.Name] = created;
                }
            }
        }
    }

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
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children))
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
