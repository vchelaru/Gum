using System;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.Services;
using Gum.ViewModels;

namespace Gum.Gui.Plugins
{
    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class MainWindowPlugin : PluginBase
    {
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
            AfterProjectSave += new Action<DataTypes.GumProjectSave>(OnProjectLoad);
        }

        void OnProjectLoad(DataTypes.GumProjectSave obj)
        {
            MainWindowViewModel vm = Locator.GetRequiredService<MainWindowViewModel>();
            
            if (obj != null && !string.IsNullOrEmpty(obj.FullFileName))
            {
                string fileName = obj.FullFileName;

                vm.Title = "Gum: " + fileName;
            }
            else
            {
                vm.Title = "Gum";
            }
        }

        

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            // can't be shut down
            return false;
        }
    }
}
