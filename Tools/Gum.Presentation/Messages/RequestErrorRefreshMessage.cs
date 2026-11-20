using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Messages;

public class RequestErrorRefreshMessage
{
    public PluginBase? RequestingPlugin { get; set; }

    /// <summary>
    /// True when the change can add or clear a project-level row (a file reference the project stores), so the
    /// project pass runs again too. That pass walks the whole project, so most requests leave this false.
    /// </summary>
    public bool IncludesProject { get; set; }
}
