using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Undo;
using System.ComponentModel.Composition;

namespace Gum.Plugins.Undos
{
    /// <summary>
    /// The History tab, shared by both heads: the selected element's undo history in an
    /// <see cref="UndosViewModel"/>. Each head supplies the view (TabViewRegistry).
    /// </summary>
    [Export(typeof(PluginBase))]
    public class MainPlugin : CorePriorityPlugin
    {
        private readonly ISelectedState _selectedState;
        private readonly IUndoManager _undoManager;

        [ImportingConstructor]
        public MainPlugin(ISelectedState selectedState, IUndoManager undoManager)
        {
            _selectedState = selectedState;
            _undoManager = undoManager;
        }

        public override void StartUp()
        {
            UndosViewModel viewModel = new(_selectedState, _undoManager);

            // Each head resolves the view model to its own view (TabViewRegistry).
            IPluginTab tab = AddControl(viewModel, "History", TabLocation.RightBottom);
            tab.GotFocus += viewModel.FocusCurrentItem;
        }
    }
}
