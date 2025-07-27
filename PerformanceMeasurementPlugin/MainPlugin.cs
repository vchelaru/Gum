using Gum;
using Gum.Plugins.BaseClasses;
using PerformanceMeasurementPlugin.ViewModels;
using PerformanceMeasurementPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Services;

namespace PerformanceMeasurementPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        PerformanceView view;
        private readonly GuiCommands _guiCommands;

        public override string FriendlyName
        {
            get { return "Performance Measurement Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1, 1); }
        }

        public MainPlugin()
        {
            _guiCommands = Locator.GetRequiredService<GuiCommands>();
        }

        public override void StartUp()
        {
            view = new PerformanceView();
            view.DataContext = new PerformanceViewModel();

            // This doesn't work currently so we're not going to show it, it just gets in the way.
            //_guiCommands.AddControl(view, "Performance");

        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            _guiCommands.RemoveControl(view);
            return true;
        }
    }
}
