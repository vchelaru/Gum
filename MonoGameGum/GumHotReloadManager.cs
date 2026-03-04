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
    /// <param name="root">The root <see cref="GraphicalUiElement"/> whose children will be reloaded.</param>
    void Update(GraphicalUiElement root);
}

/// <summary>
/// Default implementation of <see cref="IGumHotReloadManager"/>. Watches Gum project files on disk
/// and hot-reloads the element tree when changes are detected.
/// </summary>
public class GumHotReloadManager : IGumHotReloadManager
{
    private string _projectSourcePath = "";
    private FileSystemWatcher? _watcher;
    private volatile bool _pendingReload;
    private DateTime _lastChangeTime;

    /// <inheritdoc/>
    public event Action? ReloadCompleted;

    /// <inheritdoc/>
    public void Start(string absoluteGumxSourcePath)
    {
        _projectSourcePath = absoluteGumxSourcePath;

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
    public void Update(GraphicalUiElement root)
    {
        if (_pendingReload && (DateTime.UtcNow - _lastChangeTime) >= TimeSpan.FromMilliseconds(200))
        {
            _pendingReload = false;
            PerformReload(root);
        }
    }

    private void HandleFileChange(object sender, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
        if (extension == ".gumx" || extension == ".gucx" || extension == ".gusx" || extension == ".gutx")
        {
            _pendingReload = true;
            _lastChangeTime = DateTime.UtcNow;
        }
    }

    private void PerformReload(GraphicalUiElement root)
    {
        GumProjectSave newProject = GumProjectSave.Load(_projectSourcePath);
        newProject.Initialize();
        ObjectFinder.Self.GumProjectSave = newProject;

        var byName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in newProject.AllElements)
        {
            byName[element.Name] = element;
        }

        // Snapshot element names before modifying the collection
        var childElementNames = root.Children
            .Select(c => c.ElementSave?.Name)
            .ToList();

        // Remove existing children
        foreach (var child in root.Children.ToList())
        {
            child.RemoveFromManagers();
            child.Parent = null;
        }

        // Recreate each child from the updated ElementSave, preserving order
        foreach (var name in childElementNames)
        {
            if (name != null && byName.TryGetValue(name, out var newEs))
            {
                var newChild = newEs.ToGraphicalUiElement(ISystemManagers.Default, addToManagers: false);
                root.Children.Add(newChild);
            }
        }

        ReloadCompleted?.Invoke();
    }
}
#endif // !IOS && !ANDROID
