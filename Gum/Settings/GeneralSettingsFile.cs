using Gum;
using Gum.Dialogs;
using Gum.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.Messaging;
using ToolsUtilities;

namespace Gum.Settings
{
    public class RecentProjectReference
    {
        public DateTime LastTimeOpened;
        public string AbsoluteFileName;

        [XmlIgnore]
        [JsonIgnore]
        public FilePath FilePath => AbsoluteFileName;

        public bool IsFavorite;
    }

    /// <summary>
    /// Global settings for Glue, not project specific
    /// </summary>
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


        public byte CheckerColor1R { get; set; } = 150;
        public byte CheckerColor1G { get; set; } = 150;
        public byte CheckerColor1B { get; set; } = 150;

        public byte CheckerColor2R { get; set; } = 170;
        public byte CheckerColor2G { get; set; } = 170;
        public byte CheckerColor2B { get; set; } = 170;

        public byte OutlineColorR { get; set; } = 255;
        public byte OutlineColorG { get; set; } = 255;
        public byte OutlineColorB { get; set; } = 255;

        public byte GuideLineColorR { get; set; } = 255;
        public byte GuideLineColorG { get; set; } = 255;
        public byte GuideLineColorB { get; set; } = 255;

        public byte GuideTextColorR { get; set; } = 255;
        public byte GuideTextColorG { get; set; } = 255;
        public byte GuideTextColorB { get; set; } = 255;
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

        public void AddToRecentFilesIfNew(FilePath file)
        {
            var item = RecentProjects.FirstOrDefault(candidate => candidate.FilePath == file);

            if(item == null)
            {
                item = new RecentProjectReference();
                item.AbsoluteFileName = file.FullPath;
                RecentProjects.Add(item);
            }

            item.LastTimeOpened = DateTime.Now;

            const int maxFileCount = 20;
            RecentProjects = RecentProjects.OrderByDescending(contained => contained.LastTimeOpened).Take(maxFileCount).ToList();
        }

        #endregion
    }
}