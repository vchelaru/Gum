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

        private static FilePath GetFileWatchRootDirectory()
        {
            var allReferencedFiles = new List<FilePath>();

            foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
            {
                var screenPaths = ObjectFinder.Self.GetFilesReferencedBy(screen)
                    .Select(item => (FilePath)item);

                allReferencedFiles.AddRange(screenPaths);

            }
            foreach (var component in ProjectState.Self.GumProjectSave.Components)
            {
                var componentPaths = ObjectFinder.Self.GetFilesReferencedBy(component)
                    .Select(item => (FilePath)item);

                allReferencedFiles.AddRange(componentPaths);
            }
            foreach (var standardElement in ProjectState.Self.GumProjectSave.StandardElements)
            {
                var standardElementPaths = ObjectFinder.Self.GetFilesReferencedBy(standardElement)
                    .Select(item => (FilePath)item);

                allReferencedFiles.AddRange(standardElementPaths);
            }

            FilePath gumProjectFilePath = ProjectManager.Self.GumProjectSave.FullFileName;

            char gumProjectDrive = gumProjectFilePath.Standardized[0];

            allReferencedFiles.Add(gumProjectFilePath);


            allReferencedFiles = allReferencedFiles.Distinct().ToList();

            var rootmostFile = allReferencedFiles.OrderBy(item => item.Standardized.Split('/').Length).FirstOrDefault();
            var rootmostDirectory = rootmostFile.GetDirectoryContainingThis();

            foreach (var path in allReferencedFiles)
            {
                // make sure this is on the same drive as the gum project. If not, don't include it:
                if (path.Standardized.StartsWith(gumProjectDrive.ToString()))
                {
                    while (rootmostDirectory.IsRootOf(path) == false)
                    {
                        rootmostDirectory = rootmostDirectory.GetDirectoryContainingThis();
                    }
                }
            }

            return rootmostDirectory;
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
