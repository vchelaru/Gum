using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.ScreenshotPlugin
{
    [Export(typeof(PluginBase))]
    internal class MainScreenshotPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            var item = this.AddMenuItem("View", "Take Screenshot");
            item.Click += HandleTakeScreenshotClicked;
        }

        private void HandleTakeScreenshotClicked(object sender, EventArgs e)
        {

        }
    }
}
