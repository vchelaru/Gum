using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MonoGameGum;

public interface IGumHotReloadManager
{
    event Action? ReloadCompleted;
    void Start(string absoluteGumxSourcePath);
    void Stop();
    void Update(GraphicalUiElement root);
}

public class GumHotReloadManager : IGumHotReloadManager
{
    private string _projectSourcePath = "";
    private FileSystemWatcher? _watcher;
    private volatile bool _pendingReload;
    private DateTime _lastChangeTime;

    public event Action? ReloadCompleted;

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

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

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
