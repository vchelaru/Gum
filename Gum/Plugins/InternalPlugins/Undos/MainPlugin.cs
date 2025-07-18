using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.Undos
{
    // I started working on this but then realized that the undos don't store off just the variables that changed, but the entire object...
    // So it's a pain to identify exactly what has changed to list it.

    [Export(typeof(PluginBase))]
    public class MainPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            var control = new UndoDisplay();

            control.DataContext = new UndosViewModel();

            _guiCommands.AddControl(control, "History", TabLocation.RightBottom);

        }
    }
}
