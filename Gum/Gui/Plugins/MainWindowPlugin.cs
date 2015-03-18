using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Gui.Plugins
{
    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class MainWindowPlugin : PluginBase
    {
        public MainWindow MainWindow
        {
            get;
            set;
        }

        public override string FriendlyName
        {
            get
            {
                return "Main Window Plugins";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version();
            }
        }

        public override void StartUp()
        {
            ProjectLoad += new Action<DataTypes.GumProjectSave>(OnProjectLoad);
            ProjectSave += new Action<DataTypes.GumProjectSave>(OnProjectLoad);
        }

        void OnProjectLoad(DataTypes.GumProjectSave obj)
        {
            if (obj != null && !string.IsNullOrEmpty(obj.FullFileName))
            {
                string fileName = obj.FullFileName;

                MainWindow.Text = "Gum: " + fileName;
            }
            else
            {
                MainWindow.Text = "Gum";
            }
        }

        

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            // can't be shut down
            return false;
        }
    }
}
