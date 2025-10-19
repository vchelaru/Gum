using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Gum.Plugins.InternalPlugins.Output;
using Gum.Services;

namespace Gum.Plugins.Output
{
    [Export(typeof(PluginBase))]
    class MainOutputPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            MainOutputViewModel viewmodel = Locator.GetRequiredService<MainOutputViewModel>();
            MainOutputPluginView view = new() { DataContext = viewmodel, Margin = new(4)};
            _tabManager.AddControl(view, "Output", TabLocation.RightBottom);
        }
    }
}
