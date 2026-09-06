using System;

namespace Gum.Plugins.BaseClasses
{
    // This can't be done on the base class - it must be done on the
    // class that inherits from PriorityPlugin...oh well
    //[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public abstract class PriorityPlugin : WpfPluginBase
    {
        // Default so subclasses don't each have to write one. No subclass overrides it, so this is
        // the name shown in the "Manage Plugins" dialog and in "Error in plugin ..." messages -
        // keep it to the plugin's own name. ("Priority" describes event dispatch order, which is
        // not something the user needs to read on every row.)
        public override string FriendlyName => GetType().Name;

        public override Version Version
        {
            get
            {
                return new Version();
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return false;
        }
    }
}
