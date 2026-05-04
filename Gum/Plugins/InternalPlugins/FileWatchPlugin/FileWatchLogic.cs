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
    IFileWatchManager _fileWatchManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IProjectState _projectState;

    public bool Enabled => _fileWatchManager.Enabled;

    public FileWatchLogic()
    {
        _fileWatchManager = Locator.GetRequiredService<IFileWatchManager>();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _projectState = Locator.GetRequiredService<IProjectState>();
    }

    public void HandleProjectLoaded()
    {
        // On a project load we always clear ignored files, but if the project
        // is null then we also clear ignored files - see RefreshRootDirectory()
        _fileWatchManager.ClearIgnoredFiles();

        RefreshRootDirectory();
    }

    public void RefreshRootDirectory()
    {

        if (Locator.GetRequiredService<IProjectManager>().GumProjectSave?.FullFileName != null)
        {
            var directories = GetFileWatchRootDirectories();
            _fileWatchManager.EnableWithDirectories(directories);
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
        IEnumerable<string> filesReferenced;
        try
        {
            filesReferenced = ObjectFinder.Self.GetAllFilesInProject();
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

        FilePath gumProjectFilePath = Locator.GetRequiredService<IProjectManager>().GumProjectSave.FullFileName;

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
