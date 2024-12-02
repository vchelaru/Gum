using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.Undos;

public enum UndoOrRedo
{
    Undo,
    Redo
}

public class UndoItemViewModel : ViewModel
{
    public string Display
    {
        get => Get<string>();
        set => Set(value);
    }

    public UndoOrRedo UndoOrRedo
    {
        get => Get<UndoOrRedo>();
        set => Set(value);
    }
}
