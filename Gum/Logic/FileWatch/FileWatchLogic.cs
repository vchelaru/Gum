using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var directory = GetFileWatchRootDirectory();

            fileWatchManager.EnableWithDirectory(directory);
        }

        private static HashSet<FilePath> GetFileWatchRootDirectory()
        {
            HashSet<FilePath> directories = new HashSet<FilePath>();

            void AddRange(IEnumerable<FilePath> directoriesToAdd)
            {
                foreach(var directory in directoriesToAdd)
                {
                    directories.Add(directory);
                }
            }

            //var allReferencedFiles = new List<FilePath>();

            foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
            {
                var screenPaths = ObjectFinder.Self.GetFilesReferencedBy(screen)
                    .Select(item => ((FilePath)item).GetDirectoryContainingThis());

                AddRange(screenPaths);

            }
            foreach (var component in ProjectState.Self.GumProjectSave.Components)
            {
                var componentPaths = ObjectFinder.Self.GetFilesReferencedBy(component)
                    .Select(item => ((FilePath)item).GetDirectoryContainingThis());

                AddRange(componentPaths);
            }
            foreach (var standardElement in ProjectState.Self.GumProjectSave.StandardElements)
            {
                var standardElementPaths = ObjectFinder.Self.GetFilesReferencedBy(standardElement)
                    .Select(item => ((FilePath)item).GetDirectoryContainingThis());

                AddRange(standardElementPaths);
            }

            FilePath gumProjectFilePath = ProjectManager.Self.GumProjectSave.FullFileName;

            char gumProjectDrive = gumProjectFilePath.Standardized[0];

            directories.Add(gumProjectFilePath.GetDirectoryContainingThis());
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Screens/");
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Components/");
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Standards/");
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "Behaviors/");
            directories.Add(gumProjectFilePath.GetDirectoryContainingThis() + "FontCache/");
            

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
