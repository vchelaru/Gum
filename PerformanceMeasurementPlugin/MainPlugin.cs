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

namespace PerformanceMeasurementPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {

        public override string FriendlyName
        {
            get { return "Performance Measurement Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override void StartUp()
        {
            PerformanceView view = new PerformanceView();
            view.DataContext = new PerformanceViewModel();
            GumCommands.Self.GuiCommands.AddControl(view, "Performance");
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
