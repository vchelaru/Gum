using Gum.Managers;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Logic.FileWatch
{
    public class FileWatchLogic : Singleton<FileWatchLogic>
    {
        FileWatchManager fileWatchManager;

        public FileWatchLogic()
        {
            fileWatchManager = FileWatchManager.Self;
        }

        public void HandleProjectLoaded()
        {
            // When we do this, we're going to clear out the ignored files
            fileWatchManager.ClearIgnoredFiles();

            var directories = GetFileWatchRootDirectories();

            fileWatchManager.EnableWithDirectories(directories);
        }

        private static HashSet<FilePath> GetFileWatchRootDirectories()
        {
            HashSet<FilePath> directories = new HashSet<FilePath>();

            void AddRange(IEnumerable<FilePath> directoriesToAdd)
            {
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

            //var allReferencedFiles = new List<FilePath>();

            foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
            {
                var screenPaths = ObjectFinder.Self.GetFilesReferencedBy(screen)
                    .Select(item => ((FilePath)item).GetDirectoryContainingThis())
                    // to make it easier to debug:
                    .ToHashSet();

                AddRange(screenPaths);

            }
            foreach (var component in ProjectState.Self.GumProjectSave.Components)
            {
                var componentPaths = ObjectFinder.Self.GetFilesReferencedBy(component)
                    .Select(item => ((FilePath)item).GetDirectoryContainingThis())
                    .ToHashSet();

                AddRange(componentPaths);
            }
            foreach (var standardElement in ProjectState.Self.GumProjectSave.StandardElements)
            {
                var standardElementPaths = ObjectFinder.Self.GetFilesReferencedBy(standardElement)
                    .Select(item => ((FilePath)item).GetDirectoryContainingThis())
                    .ToHashSet();

                AddRange(standardElementPaths);
            }

            FilePath gumProjectFilePath = ProjectManager.Self.GumProjectSave.FullFileName;

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

            return directories;
        }

        public void HandleProjectUnloaded()
        {
            fileWatchManager.Disable();
        }

        public void IgnoreNextChangeOn(string fileName)
        {
            fileWatchManager.IgnoreNextChangeOn(fileName);
        }

    }
}
