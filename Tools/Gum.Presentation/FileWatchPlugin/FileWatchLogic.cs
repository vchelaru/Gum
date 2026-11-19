using Gum.Bundle;
using Gum.Commands;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;
using Gum.DataTypes;

namespace Gum.Logic.FileWatch;

public class FileWatchLogic
{
    private readonly IFileWatchManager _fileWatchManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IProjectState _projectState;
    private readonly IProjectManager _projectManager;
    private readonly IRefreshCoalescer _rootDirectoryRefreshCoalescer;

    private readonly IProjectRestrictions? _projectRestrictions;
    private readonly Gum.ProjectServices.IFontReferenceCacheSource? _fontReferences;
    private readonly IProjectFontWarmUp? _fontWarmUp;
    // Null (tests) runs the whole scan on the UI thread.
    private readonly IProjectChangeVersion? _changeVersion;
    private readonly IDispatcher _dispatcher;
    private readonly Func<Func<HashSet<FilePath>>, Task<HashSet<FilePath>>> _runOffThread;
    private int _scan;

    public bool Enabled => _fileWatchManager.Enabled;

    public FileWatchLogic(
        IFileWatchManager fileWatchManager,
        IGuiCommands guiCommands,
        IProjectState projectState,
        IProjectManager projectManager,
        IDispatcher dispatcher,
        IProjectRestrictions? projectRestrictions = null,
        Gum.ProjectServices.IFontReferenceCacheSource? fontReferences = null,
        IProjectFontWarmUp? fontWarmUp = null,
        IProjectChangeVersion? changeVersion = null,
        Func<Func<HashSet<FilePath>>, Task<HashSet<FilePath>>>? runOffThread = null)
    {
        _fontWarmUp = fontWarmUp;
        _changeVersion = changeVersion;
        _dispatcher = dispatcher;
        _runOffThread = runOffThread ?? (work => Task.Run(work));
        _projectRestrictions = projectRestrictions;
        _fontReferences = fontReferences;
        _fileWatchManager = fileWatchManager;
        _guiCommands = guiCommands;
        _projectState = projectState;
        _projectManager = projectManager;
        // The scan (GetFileWatchRootDirectories) is dominated by File.Exists checks over every
        // project reference and can take 200-500ms+ on a large project (#4873). Deferring it via
        // the dispatcher - rather than a background Task.Run - keeps it on the UI thread: the
        // walk reads ObjectFinder.Self's non-thread-safe cache, which is unsafe to touch from a
        // background thread while the UI thread is free to keep using it. Posting also coalesces
        // bursts of requests (e.g. several IsFile variables changed at once) into a single scan.
        _rootDirectoryRefreshCoalescer = new RefreshCoalescer(dispatcher, PerformRefreshRootDirectory);
    }

    public void HandleProjectLoaded()
    {
        using var _ = Gum.Diagnostics.StartupTiming.Time("FileWatchLogic.HandleProjectLoaded (total, called from MainFileWatchPlugin)");

        // After the load's font collection, which the scan then reuses.
        if (_fontWarmUp != null)
        {
            _fontWarmUp.PostAfterWarmUp(RefreshRootDirectory);
        }
        else
        {
            RefreshRootDirectory();
        }
    }

    public void RefreshRootDirectory()
    {
        Gum.Diagnostics.StartupTiming.Log("FileWatchLogic.RefreshRootDirectory requested");
        _rootDirectoryRefreshCoalescer.RequestRefresh();
    }

    private void PerformRefreshRootDirectory()
    {
        using var _ = Gum.Diagnostics.StartupTiming.Time("FileWatchLogic.RefreshRootDirectory (total)");

        int scan = ++_scan;
        if (_projectManager.GumProjectSave is { FullFileName: not null } project)
        {
            Func<HashSet<FilePath>> directoriesOf;
            using (Gum.Diagnostics.StartupTiming.Time("  PrepareFileWatchRootDirectories"))
            {
                directoriesOf = PrepareFileWatchRootDirectories(project);
            }

            if (_changeVersion == null)
            {
                EnableWithDirectories(directoriesOf());
                return;
            }

            // The disk half runs off the UI thread; the watchers change only if the project is unchanged by
            // then, and a change in between scans again.
            long version = _changeVersion.Version;
            _runOffThread(directoriesOf).ContinueWith(finished => _dispatcher.Post(() =>
            {
                if (scan != _scan)
                {
                    return;
                }
                if (version != _changeVersion.Version || !ReferenceEquals(_projectManager.GumProjectSave, project))
                {
                    RefreshRootDirectory();
                    return;
                }
                if (finished.IsFaulted || finished.IsCanceled)
                {
                    // The watchers stay as they are; the next scan request tries again.
                    Exception error = finished.Exception?.GetBaseException() ?? new TaskCanceledException();
                    Gum.Diagnostics.ToolLog.WriteException("File watch scan", error);
                    _guiCommands.PrintOutput("Could not work out which folders to watch: " + error.Message);
                    return;
                }
                EnableWithDirectories(finished.Result);
            }), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        else
        {
            _fileWatchManager.Disable();
        }
    }

    private void EnableWithDirectories(HashSet<FilePath> directories)
    {
        using (Gum.Diagnostics.StartupTiming.Time("  EnableWithDirectories (create FileSystemWatchers)"))
        {
            _fileWatchManager.EnableWithDirectories(directories);
        }
    }

    // Reads the project here and returns the rest, which uses only strings and the disk.
    private Func<HashSet<FilePath>> PrepareFileWatchRootDirectories(GumProjectSave gumProject)
    {

        // One walk over the entire project graph. The walker is shared with
        // bundling/codegen, so it covers every file the project references.
        //
        // Font-cache files are deliberately excluded: enumerating them means resolving every text
        // instance's effective font, which dominates the walk, and it buys no coverage here. Every
        // font-cache path is relative to the project directory, which is added unconditionally
        // below and watched with IncludeSubdirectories - so .fnt/.png changes are still detected.
        Func<List<string>> filesReferencedOf;
        try
        {
            filesReferencedOf = ObjectFinder.Self.PrepareAllFilesInProject(
                GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles,
                _fontReferences?.For(gumProject));
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput(e.ToString());
            filesReferencedOf = () => new List<string>();
        }

        string? projectFileName = gumProject.FullFileName;
        string? localizationFile = !string.IsNullOrEmpty(gumProject.LocalizationFile)
            ? _projectState.ProjectDirectory + gumProject.LocalizationFile
            : null;
        string? fontCharacterFile = gumProject.UseFontCharacterFile ? _projectState.ProjectDirectory + ".gumfcs" : null;
        FilePath? profileFile = _projectRestrictions?.ProfileFilePath;

        return () =>
        {
            IEnumerable<string> filesReferenced;
            try
            {
                filesReferenced = filesReferencedOf();
            }
            catch (Exception e)
            {
                string message = e.ToString();
                _dispatcher.Post(() => _guiCommands.PrintOutput(message));
                filesReferenced = Array.Empty<string>();
            }
            return GetFileWatchRootDirectories(filesReferenced, projectFileName, localizationFile, fontCharacterFile, profileFile);
        };
    }

    private static HashSet<FilePath> GetFileWatchRootDirectories(IEnumerable<string> filesReferenced, string? projectFileName,
        string? localizationFile, string? fontCharacterFile, FilePath? profileFile)
    {
        HashSet<FilePath> directories = new HashSet<FilePath>();

        foreach (var item in filesReferenced)
        {
            FilePath? directory;
            try
            {
                directory = ((FilePath)item).GetDirectoryContainingThis();
            }
            catch
            {
                // Invalid paths like "..\..\..\..\..\" in a root. See issue #200.
                continue;
            }

            // Skip if any already-tracked directory is a root of this one.
            if (directory != null && !directories.Any(existing => existing.IsRootOf(directory)))
            {
                directories.Add(directory);
            }
        }

        if (projectFileName != null)
        {
            FilePath gumProjectFilePath = projectFileName;
            if (gumProjectFilePath.GetDirectoryContainingThis() is { } projectDirectory)
            {
                directories.Add(projectDirectory);
            }

            // why are we adding the deep ones, isn't it enough to add the roots?

            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Screens/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Components/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Standards/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Behaviors/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "FontCache/");

            if (localizationFile != null &&
                new FilePath(localizationFile).GetDirectoryContainingThis() is { } localizationDirectory)
            {
                directories.Add(localizationDirectory);
            }
            if (fontCharacterFile != null &&
                new FilePath(fontCharacterFile).GetDirectoryContainingThis() is { } fontCharacterDirectory)
            {
                directories.Add(fontCharacterDirectory);
            }
        }

        // The engine profile may live outside the project folder. A folder that does not exist is left
        // out, since the manager would create it.
        if (profileFile?.GetDirectoryContainingThis() is { } profileDirectory &&
            System.IO.Directory.Exists(profileDirectory.FullPath))
        {
            directories.Add(profileDirectory);
        }

        // The project directory goes in last, so folders added before it are still nested under it.
        // Each nested root is another FileSystemWatcher raising a copy of every event the root raises.
        List<FilePath> nested = directories.Where(directory => directories.Any(other => other.IsRootOf(directory))).ToList();
        foreach (FilePath directory in nested)
        {
            directories.Remove(directory);
        }

        return directories;
    }

    public void HandleProjectUnloaded()
    {
        _fileWatchManager.Disable();
    }

}
