using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Plugins
{
    public enum PluginShutDownReason
    {
        UserDisabled,
        PluginException,
        PluginInitiated,
        GluxUnload,
        GlueShutDown
    }

    public interface IPlugin
    {
        string FriendlyName { get; }
        string UniqueId { get; set; }
        Version Version { get; }
        void StartUp();
        bool ShutDown(PluginShutDownReason shutDownReason);
    }
}
