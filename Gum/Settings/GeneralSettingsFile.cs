using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ToolsUtilities;
using System.Windows.Forms;
using System.Drawing;

namespace Gum.Settings
{
    public class RecentProjectReference
    {
        public DateTime LastTimeOpened;
        public string AbsoluteFileName;
    }


    public class GeneralSettingsFile
    {
        #region Properties

        public string LastProject 
        {
            get;
            set;
        }

        public bool ShowTextOutlines
        {
            get;
            set;
        }

        public bool AutoSave
        {
            get;
            set;
        }

        public int FrameRate
        {
            get;
            set;
        }

        static string GeneralSettingsFileName
        {
            get
            {
                return FileManager.UserApplicationDataForThisApplication + "GeneralSettings.xml";
            }
        }

        public List<RecentProjectReference> RecentProjects
        {
            get;
            set;
        }

        public Rectangle MainWindowBounds
        {
            get;
            set;
        }

        public FormWindowState MainWindowState
        {
            get;
            set;
        }

        public int LeftAndEverythingSplitterDistance
        {
            get;
            set;
        }

        public int PreviewSplitterDistance
        {
            get;
            set;
        }

        public int StatesAndVariablesSplitterDistance
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public GeneralSettingsFile()
        {
            ShowTextOutlines = false;
            AutoSave = true;
            FrameRate = 30;
            LeftAndEverythingSplitterDistance = 196;
            PreviewSplitterDistance = 558;
            StatesAndVariablesSplitterDistance = 119;
            RecentProjects = new List<RecentProjectReference>();
        }

        public static GeneralSettingsFile LoadOrCreateNew()
        {
            GeneralSettingsFile toReturn;
            if (File.Exists(GeneralSettingsFileName))
            {
                try
                {
                    toReturn = FileManager.XmlDeserialize<GeneralSettingsFile>(GeneralSettingsFileName);
                }
                catch
                {
                    toReturn = new GeneralSettingsFile();
                }
            }
            else
            {
                toReturn = new GeneralSettingsFile();
            }

            return toReturn;

        }

        public void Save()
        {
            FileManager.XmlSerialize(this, GeneralSettingsFileName);
        }

        public void AddToRecentFilesIfNew(string file)
        {
            var item = RecentProjects.FirstOrDefault(candidate => candidate.AbsoluteFileName == file);

            if(item == null)
            {
                item = new RecentProjectReference();
                item.AbsoluteFileName = file;
                RecentProjects.Add(item);
            }

            item.LastTimeOpened = DateTime.Now;

            const int maxFileCount = 20;
            RecentProjects = RecentProjects.OrderByDescending(contained => contained.LastTimeOpened).Take(maxFileCount).ToList();
        }

        #endregion
    }
}
