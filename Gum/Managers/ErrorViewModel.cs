using Gum.Mvvm;
using Gum.Plugins.BaseClasses;

namespace Gum.Managers;

public class ErrorViewModel : ViewModel
{
    public PluginBase? OwnerPlugin { get; set; }

    public string Message
    {
        get; set;
    } = string.Empty;
}
