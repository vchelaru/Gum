using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Gum.Plugins.InternalPlugins.Output;

namespace Gum.Plugins.Output
{
    [Export(typeof(PluginBase))]
    class MainOutputPlugin : PriorityPlugin
    {
        private readonly MainOutputViewModel _mainOutputViewModel;

        [ImportingConstructor]
        public MainOutputPlugin(MainOutputViewModel mainOutputViewModel)
        {
            _mainOutputViewModel = mainOutputViewModel;
        }

        public override void StartUp()
        {
            MainOutputPluginView view = new() { DataContext = _mainOutputViewModel, Margin = new(4)};
            _tabManager.AddControl(view, "Output", TabLocation.RightBottom);
        }
    }
}
