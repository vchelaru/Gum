using Gum.Bundle;
using Gum.Commands;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

public class FileWatchLogic
{
    private readonly IFileWatchManager _fileWatchManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IProjectState _projectState;
    private readonly IProjectManager _projectManager;
    private readonly IRefreshCoalescer _rootDirectoryRefreshCoalescer;

    public bool Enabled => _fileWatchManager.Enabled;

    public FileWatchLogic(
        IFileWatchManager fileWatchManager,
        IGuiCommands guiCommands,
        IProjectState projectState,
        IProjectManager projectManager,
        IDispatcher dispatcher)
    {
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

        // On a project load we always clear ignored files, but if the project
        // is null then we also clear ignored files - see RefreshRootDirectory()
        _fileWatchManager.ClearIgnoredFiles();

        RefreshRootDirectory();
    }

    public void RefreshRootDirectory()
    {
        Gum.Diagnostics.StartupTiming.Log("FileWatchLogic.RefreshRootDirectory requested");
        _rootDirectoryRefreshCoalescer.RequestRefresh();
    }

    private void PerformRefreshRootDirectory()
    {
        using var _ = Gum.Diagnostics.StartupTiming.Time("FileWatchLogic.RefreshRootDirectory (total)");

        if (_projectManager.GumProjectSave?.FullFileName != null)
        {
            HashSet<FilePath> directories;
            using (Gum.Diagnostics.StartupTiming.Time("  GetFileWatchRootDirectories"))
            {
                directories = GetFileWatchRootDirectories();
            }

            using (Gum.Diagnostics.StartupTiming.Time("  EnableWithDirectories (create FileSystemWatchers)"))
            {
                _fileWatchManager.EnableWithDirectories(directories);
            }
        }
        else
        {

            _fileWatchManager.ClearIgnoredFiles();
            _fileWatchManager.Disable();
        }
    }

    private HashSet<FilePath> GetFileWatchRootDirectories()
    {
        HashSet<FilePath> directories = new HashSet<FilePath>();

        // One walk over the entire project graph. The walker is shared with
        // bundling/codegen, so it covers every file the project references.
        //
        // Font-cache files are deliberately excluded: enumerating them means resolving every text
        // instance's effective font, which dominates the walk, and it buys no coverage here. Every
        // font-cache path is relative to the project directory, which is added unconditionally
        // below and watched with IncludeSubdirectories - so .fnt/.png changes are still detected.
        IEnumerable<string> filesReferenced;
        try
        {
            using var _ = Gum.Diagnostics.StartupTiming.Time("    ObjectFinder.GetAllFilesInProject");
            filesReferenced = ObjectFinder.Self.GetAllFilesInProject(
                GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);
        }
        catch (Exception e)
        {
            _guiCommands.PrintOutput(e.ToString());
            filesReferenced = Array.Empty<string>();
        }

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
            if (!directories.Any(existing => existing.IsRootOf(directory)))
            {
                directories.Add(directory);
            }
        }

        FilePath gumProjectFilePath = _projectManager.GumProjectSave.FullFileName;

        if (gumProjectFilePath != null)
        {
            char gumProjectDrive = gumProjectFilePath.Standardized[0];
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis());

            // why are we adding the deep ones, isn't it enough to add the roots?

            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Screens/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Components/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Standards/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Behaviors/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "FontCache/");

            var gumProject = _projectState.GumProjectSave;
            if (!string.IsNullOrEmpty(gumProject.LocalizationFile))
            {
                var localizationDirectory = new FilePath(
                        _projectState.ProjectDirectory + gumProject.LocalizationFile)
                    .GetDirectoryContainingThis();
                directories.Add(localizationDirectory);
            }
            if (gumProject.UseFontCharacterFile)
            {
                var fontCharacterDirectory = new FilePath(
                        _projectState.ProjectDirectory + ".gumfcs")
                    .GetDirectoryContainingThis();
                directories.Add(fontCharacterDirectory);
            }
        }

        return directories;
    }

    public void HandleProjectUnloaded()
    {
        _fileWatchManager.Disable();
    }

}
