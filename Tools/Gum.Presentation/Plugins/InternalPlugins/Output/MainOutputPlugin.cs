using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.Output
{
    [Export(typeof(PluginBase))]
    class MainOutputPlugin : CorePriorityPlugin
    {
        private readonly MainOutputViewModel _mainOutputViewModel;

        [ImportingConstructor]
        public MainOutputPlugin(MainOutputViewModel mainOutputViewModel)
        {
            _mainOutputViewModel = mainOutputViewModel;
        }

        public override void StartUp()
        {
            // Each head resolves the view model to its own view (TabViewRegistry).
            IPluginTab tab = _tabManager.AddControl(_mainOutputViewModel, "Output", TabLocation.RightBottom);

            // Errors written to Output are silent otherwise, so bring the tab forward rather than
            // interrupting with a dialog. Selecting a hidden tab deselects the visible one in this
            // dock area without showing anything, so show it first.
            _mainOutputViewModel.ErrorAdded += () =>
            {
                tab.Show();
                tab.IsSelected = true;
            };
        }
    }
}
