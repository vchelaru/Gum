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


        static string GeneralSettingsFileName
        {
            get
            {
                return FileManager.UserApplicationDataForThisApplication + "GeneralSettings.xml";
            }
        }

        #endregion

        public GeneralSettingsFile()
        {
            ShowTextOutlines = false;

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
    }
}
