using Gum.Managers;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

public class FileWatchLogic : Singleton<FileWatchLogic>
{
    FileWatchManager _fileWatchManager;

    public bool Enabled { get { return _fileWatchManager.Enabled; } }

    public FileWatchLogic()
    {
        _fileWatchManager = FileWatchManager.Self;
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

        if (ProjectManager.Self.GumProjectSave?.FullFileName != null)
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

    private static HashSet<FilePath> GetFileWatchRootDirectories()
    {
        HashSet<FilePath> directories = new HashSet<FilePath>();

        void AddRange(List<string> files)
        {
            var directoriesToAdd = files
                .Select(item =>
                {
                    FilePath filePath = null;
                    try
                    {
                        filePath = ((FilePath)item).GetDirectoryContainingThis();
                    }
                    catch
                    {
                        // This can happen if there's an invalid file path like 
                        // "..\..\..\..\..\" in a root
                        // For info see https://github.com/vchelaru/Gum/issues/200
                        // leave it as null, will filter out later
                    }

                    return filePath;
                })
                .Where(item => item != null)
                // to make it easier to debug:
                .ToHashSet();
            foreach(var directory in directoriesToAdd)
            {
                // check if the root of this directory is already here:
                var isAlreadyHandled = directories
                    .Any(item => item.IsRootOf(directory));

                if(!isAlreadyHandled)
                {
                    directories.Add(directory);
                }

            }
        }

        foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
        {
            var filesReferenced = ObjectFinder.Self.GetFilesReferencedBy(screen);

            AddRange(filesReferenced);
        }
        foreach (var component in ProjectState.Self.GumProjectSave.Components)
        {
            var filesReferenced = ObjectFinder.Self.GetFilesReferencedBy(component);
            AddRange(filesReferenced);
        }
        foreach (var standardElement in ProjectState.Self.GumProjectSave.StandardElements)
        {
            var filesReferenced = ObjectFinder.Self.GetFilesReferencedBy(standardElement);
            AddRange(filesReferenced);
        }

        FilePath gumProjectFilePath = ProjectManager.Self.GumProjectSave.FullFileName;

        if(gumProjectFilePath != null)
        {
            char gumProjectDrive = gumProjectFilePath.Standardized[0];
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis());

            // why are we adding the deep ones, isn't it enough to add the roots?

            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Screens/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Components/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Standards/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Behaviors/");
            //directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "FontCache/");

            var gumProject = GumState.Self.ProjectState.GumProjectSave;
            if (!string.IsNullOrEmpty(gumProject.LocalizationFile))
            {
                var localizationDirectory = new FilePath(
                        GumState.Self.ProjectState.ProjectDirectory + gumProject.LocalizationFile)
                    .GetDirectoryContainingThis();
                directories.Add(localizationDirectory);
            }
            if (gumProject.UseFontCharacterFile)
            {
                var fontCharacterDirectory = new FilePath(
                        GumState.Self.ProjectState.ProjectDirectory + ".gumfcs")
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
