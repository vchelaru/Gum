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

        List<FilePath> changedFilesWaitingForFlush = new List<FilePath>();
        List<FilePath> filesCurrentlyFlushing = new List<FilePath>();

        FileSystemWatcher fileSystemWatcher;

        DateTime lastFileChange;

        object LockObject=new object();

        bool IsFlushing;

        #endregion

        public FileWatchManager()
        {
            fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Filter = "*.*";
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.NotifyFilter =
                NotifyFilters.LastWrite |
                NotifyFilters.DirectoryName;

            // todo - turn it on once a project is loaded

            fileSystemWatcher.Deleted += new FileSystemEventHandler(HandleFileSystemDelete);
            fileSystemWatcher.Changed += new FileSystemEventHandler(HandleFileSystemChange);
        }

        public void EnableWithDirectory(FilePath directoryFilePath)
        {
            var filePathAsString = directoryFilePath.Standardized;
            // Gum standard is to have a trailing slash, 
            // but FileSystemWatcher expects no trailing slash:
            fileSystemWatcher.Path = filePathAsString.Substring(0, filePathAsString.Length - 1);
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Disable()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        private void HandleFileSystemDelete(object sender, FileSystemEventArgs e)
        {
            // do anything?
        }

        private void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            var fileName = new FilePath(e.FullPath);
            
            lock (LockObject)
            {
                bool wasIgnored = TryIgnoreFileChange(fileName);
                if (!wasIgnored)
                {
                    changedFilesWaitingForFlush.Add(fileName);

                }
                lastFileChange = DateTime.Now;
            }
        }
    
        bool TryIgnoreFileChange(FilePath fileName)
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

        public void Flush()
        {
            // early out
            if(IsFlushing || DateTime.Now - lastFileChange < TimeSpan.FromSeconds(2))
            {
                return;
            }
            // endif
            lock (LockObject)
            {
                IsFlushing = true;

                filesCurrentlyFlushing.AddRange(changedFilesWaitingForFlush);
                changedFilesWaitingForFlush.Clear();

                bool anyFlushed = false;

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
