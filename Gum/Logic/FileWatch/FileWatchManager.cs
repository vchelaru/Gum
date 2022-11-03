using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Logic.FileWatch
{
    public class FileWatchManager : Singleton<FileWatchManager>
    {
        #region Fields/Properties

        Dictionary<FilePath, int> changesToIgnore = new Dictionary<FilePath, int>();

        public List<FilePath> ChangedFilesWaitingForFlush { get; private set; } = new List<FilePath>();
        List<FilePath> filesCurrentlyFlushing = new List<FilePath>();

        FileSystemWatcher fileSystemWatcher;

        DateTime LastFileChange;

        object LockObject=new object();

        bool IsFlushing;

        HashSet<FilePath> filePathsToWatch;

        #endregion

        public FileWatchManager()
        {
            fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Filter = "*.*";
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.NotifyFilter =
                NotifyFilters.LastWrite |
                NotifyFilters.DirectoryName
                // This causes 2 events to fire for changes on files like screens
                // ... but it's needed for file names on PNG
                |NotifyFilters.FileName;



            fileSystemWatcher.Deleted += new FileSystemEventHandler(HandleFileSystemDelete);
            fileSystemWatcher.Changed += new FileSystemEventHandler(HandleFileSystemChange);
            // Gum files get deleted and then created, rather than changed
            fileSystemWatcher.Created += new FileSystemEventHandler(HandleFileSystemChange);
            fileSystemWatcher.Renamed += HandleRename;
        }

        public void EnableWithDirectories(HashSet<FilePath> directories)
        {
            filePathsToWatch = directories;

            FilePath gumProjectFilePath = ProjectManager.Self.GumProjectSave.FullFileName;

            char gumProjectDrive = gumProjectFilePath.Standardized[0];

            var rootmostDirectory = directories.OrderBy(item => item.FullPath.Length).FirstOrDefault();

            foreach (var path in directories)
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

            var filePathAsString = rootmostDirectory.Standardized;
            // Gum standard is to have a trailing slash, 
            // but FileSystemWatcher expects no trailing slash:
            fileSystemWatcher.Path = filePathAsString.Substring(0, filePathAsString.Length - 1);
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Disable()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        private void HandleRename(object sender, RenamedEventArgs e)
        {
            var fileName = new FilePath(e.FullPath);
            // for now only do texture files like PNG:
            var extension = fileName.Extension;
            if(extension == "png")
            {
                HandleFileSystemChange(fileName);
            }
        }

        private void HandleFileSystemDelete(object sender, FileSystemEventArgs e)
        {
            var fileName = new FilePath(e.FullPath);

            if(fileName.Extension == "png")
            {
                int m = 3;
            }
            // do anything?
        }

        private void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            // for some reason if we include created here, we'll get double-adds for XML files like screens...
            if(e.ChangeType != WatcherChangeTypes.Created)
            {
                var fileName = new FilePath(e.FullPath);
                HandleFileSystemChange(fileName);
            }
        }

        private void HandleFileSystemChange(FilePath fileName)
        {
            lock (LockObject)
            {
                bool wasIgnored = TryGetIgnoreFileChange(fileName);

                if(!wasIgnored)
                {
                    var directoryContainingThis = fileName.GetDirectoryContainingThis();
                    var isFolderConsidered = filePathsToWatch.Contains(directoryContainingThis);

                    if(!isFolderConsidered)
                    {
                        wasIgnored = true;
                    }
                }

                if (!wasIgnored)
                {
                    ChangedFilesWaitingForFlush.Add(fileName);

                    LastFileChange = DateTime.Now;
                }
            }
        }

        bool TryGetIgnoreFileChange(FilePath fileName)
        {
            int timesToIgnore = 0;

            if (changesToIgnore.ContainsKey(fileName))
            {
                timesToIgnore = changesToIgnore[fileName];

                changesToIgnore[fileName] = System.Math.Max(0, timesToIgnore - 1);
            }

            return timesToIgnore > 0;
        }

        public void IgnoreNextChangeOn(string fileName)
        {
            lock (LockObject)
            {
                if (FileManager.IsRelative(fileName))
                {
                    throw new Exception("File name should be absolute");
                }
                string standardized = FileManager.Standardize(fileName, preserveCase:false, makeAbsolute:true).ToLower();
                if (changesToIgnore.ContainsKey(standardized))
                {
                    changesToIgnore[standardized] = 1 + changesToIgnore[standardized];
                }
                else
                {
                    changesToIgnore[standardized] = 1;
                }
            }
        }

        public void ClearIgnoredFiles()
        {
            changesToIgnore.Clear();
        }

        public TimeSpan TimeToNextFlush => (LastFileChange + TimeSpan.FromSeconds(2)) - DateTime.Now;

        public void Flush()
        {
            // early out
            if(IsFlushing || TimeToNextFlush.TotalSeconds > 0)
            {
                return;
            }
            // endif
            lock (LockObject)
            {
                IsFlushing = true;

                filesCurrentlyFlushing.AddRange(ChangedFilesWaitingForFlush);
                ChangedFilesWaitingForFlush.Clear();

                foreach (var file in filesCurrentlyFlushing)
                {
                    FileChangeReactionLogic.Self.ReactToFileChanged(file);
                }
                filesCurrentlyFlushing.Clear();
                IsFlushing = false;
            }
        }

    }
}
