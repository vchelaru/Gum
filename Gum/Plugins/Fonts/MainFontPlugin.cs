using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.Fonts
{
    [Export(typeof(PluginBase))]
    public class MainFontPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            // todo - I'd like to move the 
            // Clear Fonts command from the
            // main window to be part of the
            // plugin. I'd also like to have it
            // handle opening the font cache folder
            // (since people don't know where it is)
            // and a command which will go through the
            // entire proejct and re-create all fonts which
            // are needed, so users don't have to go through
            // it themselves if the font cache is deleted.
        }
    }
}
