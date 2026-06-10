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

#if XNALIKE
namespace MonoGameGum;
#elif RAYLIB
namespace RaylibGum;
#endif

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
    /// This is not typically called directly — use <see cref="GumService.EnableHotReload"/> instead.
    /// </summary>
    /// <param name="absoluteGumxSourcePath">The absolute path to the .gumx project file.</param>
    void Start(string absoluteGumxSourcePath);

    /// <summary>
    /// Stops watching for file changes and releases the underlying file system watcher.
    /// </summary>
    void Stop();

    /// <summary>
    /// Checks for a pending reload and, if enough time has elapsed since the last file change, performs the reload.
    /// This is not typically called directly — <see cref="GumService"/> calls this automatically each frame.
    /// </summary>
    /// <param name="roots">The roots whose direct children will be rebuilt from the reloaded project.
    /// Typically the same collection passed to <see cref="GumService.Update(GameTime, IEnumerable{GraphicalUiElement})"/>.</param>
    void Update(IEnumerable<GraphicalUiElement> roots);
}

/// <summary>
/// Default implementation of <see cref="IGumHotReloadManager"/>. Watches Gum project files on disk
/// and hot-reloads the element tree when changes are detected.
/// </summary>
public class GumHotReloadManager : IGumHotReloadManager
{
    private string _projectSourcePath = "";
    private string _binGumDirectory = "";
    private FileSystemWatcher? _watcher;
    private volatile bool _pendingReload;
    private DateTime _lastChangeTime;
    private readonly List<string> _changedFontFiles = new List<string>();
    private readonly object _fontFileLock = new object();

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
        if (_pendingReload && (DateTime.UtcNow - _lastChangeTime) >= TimeSpan.FromMilliseconds(200))
        {
            System.Diagnostics.Debug.WriteLine(
                $"[HotReload] PerformReload firing, elapsedSinceLastChange=" +
                $"{(DateTime.UtcNow - _lastChangeTime).TotalMilliseconds:0}ms");
            _pendingReload = false;
            PerformReload(roots);
        }
    }

    private void HandleFileChange(object sender, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();

        long size = -1;
        string mtime = "-";
        try
        {
            if (File.Exists(e.FullPath))
            {
                size = new FileInfo(e.FullPath).Length;
                mtime = File.GetLastWriteTimeUtc(e.FullPath).ToString("HH:mm:ss.fff");
            }
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(
            $"[HotReload] event={e.ChangeType} file={Path.GetFileName(e.FullPath)} ext={extension} " +
            $"size={size} mtime={mtime} now={DateTime.UtcNow:HH:mm:ss.fff}");

        if (extension == ".gumx" || extension == ".gucx" || extension == ".gusx" || extension == ".gutx"
            || extension == ".fnt" || extension == ".ganx")
        {
            if (extension == ".fnt")
            {
                lock (_fontFileLock)
                {
                    _changedFontFiles.Add(e.FullPath);
                }
            }

            _pendingReload = true;
            _lastChangeTime = DateTime.UtcNow;
        }
    }

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

        var loaderManager = RenderingLibrary.Content.LoaderManager.Self;

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
            loaderManager.Dispose(standardizedFont);

            // Unload the cached texture pages so they get reloaded too
            foreach (var pngPath in copiedPngs)
            {
                var standardizedPng = ToolsUtilities.FileManager.Standardize(pngPath, preserveCase: true, makeAbsolute: true);
                loaderManager.Dispose(standardizedPng);
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

        var gumxMtime = File.Exists(_projectSourcePath)
            ? File.GetLastWriteTimeUtc(_projectSourcePath).ToString("HH:mm:ss.fff")
            : "-";
        var gumxSize = File.Exists(_projectSourcePath) ? new FileInfo(_projectSourcePath).Length : -1;
        System.Diagnostics.Debug.WriteLine(
            $"[HotReload] loading gumx={Path.GetFileName(_projectSourcePath)} " +
            $"size={gumxSize} mtime={gumxMtime}");

        GumProjectSave newProject = GumProjectSave.Load(_projectSourcePath);
        newProject.Initialize();
        ObjectFinder.Self.GumProjectSave = newProject;

        foreach (var element in newProject.AllElements)
        {
            var defaultState = element.DefaultState;
            if (defaultState?.Variables == null) continue;
            foreach (var v in defaultState.Variables)
            {
                if (v.Name == "BackInnerBorder.Width")
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[HotReload]   {element.Name}.{v.Name} = {v.Value}");
                }
            }
        }

        // Reload animations from the source project directory (loose files on disk) by enumerating
        // its *Animations.ganx files. Hot reload only ever runs against a real filesystem, so a
        // LooseFileGumFileProvider rooted at the source directory is the right seam — no need to
        // mutate FileManager.RelativeDirectory the way the old per-element probe did.
        string? sourceDirectory = Path.GetDirectoryName(_projectSourcePath);
        Gum.Bundle.LooseFileGumFileProvider animationProvider = new Gum.Bundle.LooseFileGumFileProvider(
            string.IsNullOrEmpty(sourceDirectory) ? "." : sourceDirectory);
        GumService.LoadAnimationsFromProvider(newProject, animationProvider);

        System.Diagnostics.Debug.WriteLine(
            $"[HotReload] rootList.Count = {rootList.Count}");

        ApplyDiff(rootList, newProject, SystemManagers.Default);

        System.Diagnostics.Debug.WriteLine("[HotReload] DONE");

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
    /// </summary>
    private static void DiffDesignTimeChildren(
        GraphicalUiElement parent,
        ElementSave newEs,
        ISystemManagers systemManagers)
    {
        // Two lookup tables: one for design-time children (Tag is InstanceSave) so we can do
        // typed operations like retype/remove, and one keyed by Name across ALL children so
        // we don't duplicate a runtime-claimed child (e.g. one whose Tag was nulled by user
        // code — the documented limitation in issue #2848).
        Dictionary<string, GraphicalUiElement> designTimeByName =
            new Dictionary<string, GraphicalUiElement>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, GraphicalUiElement> anyByName =
            new Dictionary<string, GraphicalUiElement>(StringComparer.OrdinalIgnoreCase);
        foreach (GraphicalUiElement child in parent.Children.ToList())
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

        ReorderDesignTimeChildren(parent, newInstances);
    }

    private static void DetachAndRemove(GraphicalUiElement child)
    {
        child.Parent = null;
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
    /// Reorders the design-time children of <paramref name="parent"/> so that they appear in the
    /// same relative order as <paramref name="newInstances"/>. Runtime-added children keep their
    /// existing slots — only the positions occupied by design-time children are reshuffled.
    /// </summary>
    private static void ReorderDesignTimeChildren(
        GraphicalUiElement parent,
        IList<InstanceSave> newInstances)
    {
        ObservableCollection<GraphicalUiElement> children = parent.Children;
        if (children.Count == 0)
        {
            return;
        }

        List<int> designTimeSlots = new List<int>();
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].Tag is InstanceSave)
            {
                designTimeSlots.Add(i);
            }
        }

        if (designTimeSlots.Count == 0)
        {
            return;
        }

        Dictionary<string, GraphicalUiElement> designTimeByName =
            new Dictionary<string, GraphicalUiElement>(StringComparer.OrdinalIgnoreCase);
        foreach (int slot in designTimeSlots)
        {
            InstanceSave inst = (InstanceSave)children[slot].Tag!;
            if (inst.Name != null)
            {
                designTimeByName[inst.Name] = children[slot];
            }
        }

        List<GraphicalUiElement> desired = new List<GraphicalUiElement>();
        foreach (InstanceSave inst in newInstances)
        {
            if (inst.Name != null
                && designTimeByName.TryGetValue(inst.Name, out GraphicalUiElement? c))
            {
                desired.Add(c);
            }
        }

        for (int i = 0; i < designTimeSlots.Count && i < desired.Count; i++)
        {
            int slot = designTimeSlots[i];
            GraphicalUiElement want = desired[i];
            if (children[slot] == want)
            {
                continue;
            }
            int currentIndex = children.IndexOf(want);
            if (currentIndex < 0)
            {
                continue;
            }
            children.Move(currentIndex, slot);
        }
    }
}
#endif // !IOS && !ANDROID
