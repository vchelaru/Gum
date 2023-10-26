using System;

namespace Gum.Plugins.BaseClasses
{
    // This can't be done on the base class - it must be done on the
    // class that inherits from InternalPlugin...oh well
    //[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public abstract class InternalPlugin : PluginBase
    {
        public override string FriendlyName
        {
            get
            {
                return "Internal Plugin: " + this.GetType().Name;
            }
        }

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
