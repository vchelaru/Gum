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
}
