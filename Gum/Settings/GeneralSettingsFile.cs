using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ToolsUtilities;

namespace Gum.Settings
{
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

        public List<string> RecentProjects
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

            RecentProjects = new List<string>();
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
            if (!RecentProjects.Contains(file))
                RecentProjects.Add(file);

            const int maxFileCount = 20;

            if (RecentProjects.Count > maxFileCount)
                RecentProjects.RemoveRange(0, RecentProjects.Count - maxFileCount);
        }

        #endregion
    }
}
