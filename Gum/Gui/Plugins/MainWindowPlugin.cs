using System;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.ViewModels;

namespace Gum.Gui.Plugins
{
    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class MainWindowPlugin : PluginBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

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

        [ImportingConstructor]
        public MainWindowPlugin(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
        }

        public override void StartUp()
        {
            ProjectLoad += new Action<DataTypes.GumProjectSave>(OnProjectLoad);
            AfterProjectSave += new Action<DataTypes.GumProjectSave>(OnProjectLoad);
        }

        void OnProjectLoad(DataTypes.GumProjectSave obj)
        {
            if (obj != null && !string.IsNullOrEmpty(obj.FullFileName))
            {
                string fileName = obj.FullFileName;

                _mainWindowViewModel.Title = fileName;
            }
            else
            {
                _mainWindowViewModel.Title = "Gum";
            }
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            // can't be shut down
            return false;
        }
    }
}
