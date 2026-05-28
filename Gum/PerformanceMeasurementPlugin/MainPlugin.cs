using Gum;
using Gum.Plugins;
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
            view.DataContext = new PerformanceViewModel();

            // This is a diagnostic tab, so it should not claim the default RightBottom
            // selection. The tab manager auto-selects the first tab added at a location when
            // none is selected yet; deselecting here lets another tab (e.g. Errors) hold focus
            // regardless of plugin load order.
            PluginTab tab = AddControl(view, "Performance", Gum.TabLocation.RightBottom);
            tab.IsSelected = false;
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            //_tabManager.HideTabForControl(view);
            return true;
        }
    }
}
