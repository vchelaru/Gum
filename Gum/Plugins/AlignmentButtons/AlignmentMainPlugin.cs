using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.AlignmentButtons
{
    [Export(typeof(PluginBase))]
    public class AlignmentMainPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            GumCommands.Self.GuiCommands.AddControl(
                new Gum.Plugins.AlignmentButtons.AlignmentPluginControl(), "Alignment");

        }
    }
}
