using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.Undos;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

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

            UndosViewModel viewModel = new ();
            control.DataContext = viewModel;

            PluginTab tab = AddControl(control, "History", TabLocation.RightBottom);
            tab.GotFocus += () =>
            {
                control.ListBoxInstance.SelectedItem = viewModel.HistoryItems.LastOrDefault(x => x.UndoOrRedo is InternalPlugins.Undos.UndoOrRedo.Undo);
                if (control.ListBoxInstance.SelectedItem is UndoItemViewModel selected)
                {
                    UndoItemViewModel? next =
                        viewModel.HistoryItems.IndexOf(selected) is var index and > -1 ?
                        viewModel.HistoryItems.ElementAt(index) : null;

                    control.ListBoxInstance.ScrollIntoView(next ?? selected);
                }
            };

        }
    }
}
