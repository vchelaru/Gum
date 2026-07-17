using Gum;
using Gum.Plugins.BaseClasses;
using Gum.Services;
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
        PerformanceView view;

        public override string FriendlyName
        {
            get { return "Performance Measurement Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1, 1); }
        }

        public override void StartUp()
        {
            view = new PerformanceView();
            view.DataContext = new PerformanceViewModel(new DispatcherUiTimer());

            AddControl(view, "Performance", Gum.TabLocation.RightBottom);
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            //_tabManager.HideTabForControl(view);
            return true;
        }
    }
}
