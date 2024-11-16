using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsPlugin;

[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PluginBase
{
    public override string FriendlyName => "Gum Forms Plugin";

    public override Version Version => throw new NotImplementedException();

    public override bool ShutDown(PluginShutDownReason shutDownReason)
    {
        throw new NotImplementedException();
    }

    public override void StartUp()
    {
        throw new NotImplementedException();
    }
}
