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

    private void PerformReload(IEnumerable<GraphicalUiElement> roots)
    {
        // Materialize so we can iterate twice and so the caller's collection
        // is safe from us modifying it via the rebuild step.
        var rootList = roots.ToList();

        CopyAndUnloadChangedFonts();

        var savedRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;

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

        // Point at the source project directory so TryLoadAnimation finds the updated .ganx files
        var sourceDirectory = Path.GetDirectoryName(_projectSourcePath)?.Replace('\\', '/');
        if (sourceDirectory != null && !sourceDirectory.EndsWith("/"))
        {
            sourceDirectory += "/";
        }
        ToolsUtilities.FileManager.RelativeDirectory = sourceDirectory ?? savedRelativeDirectory;

        foreach (var element in newProject.AllElements)
        {
            var animation = GumService.TryLoadAnimation(element);
            if (animation != null)
            {
                newProject.ElementAnimations.Add(animation);
            }
        }

        ToolsUtilities.FileManager.RelativeDirectory = savedRelativeDirectory;

        var byName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in newProject.AllElements)
        {
            byName[element.Name] = element;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[HotReload] rootList.Count = {rootList.Count}");

        int reappliedCount = 0;
        int visitedCount = 0;
        foreach (var root in rootList)
        {
            ReapplyRecursively(root, byName, ref reappliedCount, ref visitedCount);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[HotReload] DONE visited={visitedCount} reapplied={reappliedCount}");

        ReloadCompleted?.Invoke();
    }

    /// <summary>
    /// Walks the visual tree under <paramref name="element"/> and re-applies the new project's
    /// default-state variables in place on each visual whose ElementSave matches a project element.
    /// Preserves runtime-added children and runtime-set state — only the design-time variables are reset.
    /// When re-applying at level N, descent skips children that came from N's design-time InstanceSaves
    /// (those values were already covered by N's qualified-name variables like "BackInnerBorder.Width").
    /// </summary>
    private void ReapplyRecursively(
        GraphicalUiElement element,
        Dictionary<string, ElementSave> byName,
        ref int reappliedCount,
        ref int visitedCount)
    {
        visitedCount++;

        var name = element.ElementSave?.Name;
        HashSet<string>? designTimeChildNames = null;

        if (name != null && byName.TryGetValue(name, out var newEs))
        {
            // Re-point so the visual references the live project's ElementSave.
            element.ElementSave = newEs;

            if (newEs.DefaultState != null)
            {
                element.SetVariablesRecursively(newEs, newEs.DefaultState);
                reappliedCount++;
                System.Diagnostics.Debug.WriteLine(
                    $"[HotReload]   reapplied '{name}' on visual name={element.Name ?? "<null>"}");
            }

            // After re-applying, do not re-enter design-time instance children — their values are
            // owned by the parent's qualified-name variables we just set. Only descend into
            // runtime-added children to find further matches.
            if (newEs.Instances != null)
            {
                designTimeChildNames = new HashSet<string>(
                    newEs.Instances.Select(i => i.Name).Where(n => n != null)!,
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        foreach (var child in element.Children.ToList())
        {
            if (designTimeChildNames != null
                && child.Name != null
                && designTimeChildNames.Contains(child.Name))
            {
                continue;
            }
            ReapplyRecursively(child, byName, ref reappliedCount, ref visitedCount);
        }
    }
}
#endif // !IOS && !ANDROID
