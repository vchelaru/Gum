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
        #region Fields

        bool mAutoSave;

        #endregion


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
            get { return mAutoSave; }
            set { mAutoSave = value; }
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
            mAutoSave = true;

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
            if (this.RecentProjects.Contains(file) == false)
            {
                this.RecentProjects.Add(file);
            }

            const int maxFileCount = 20;

            while (this.RecentProjects.Count > 20)
            {
                int lastIndex = RecentProjects.Count - 1;

                RecentProjects.RemoveAt(lastIndex);
            }
        }

        #endregion
    }
}
