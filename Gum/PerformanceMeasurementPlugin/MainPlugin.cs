using Gum;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Microsoft.Extensions.Logging.Abstractions;
using PerformanceMeasurementPlugin.Services;
using PerformanceMeasurementPlugin.ViewModels;
using System;
using System.ComponentModel.Composition;

namespace PerformanceMeasurementPlugin
{
    /// <summary>
    /// The Performance tab, under both heads: a <see cref="PerformanceViewModel"/> polling the active
    /// renderer. Each head supplies the view (TabViewRegistry).
    /// </summary>
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        private readonly IDispatcher _dispatcher;

        [ImportingConstructor]
        public MainPlugin(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

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
            // The plugin owns its timer (not the shared bridged one), so its interval is its own.
            PeriodicUiTimer timer = new PeriodicUiTimer(_dispatcher, NullLogger<PeriodicUiTimer>.Instance);
            PerformanceViewModel viewModel = new PerformanceViewModel(timer, new RenderDiagnosticsService());

            AddControl(viewModel, "Performance", Gum.TabLocation.RightBottom);
        }

        public override bool ShutDown(Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
