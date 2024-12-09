using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;

internal class StateItemViewModel : ViewModel
{
    public string Title
    {
        get => Get<string>();
        set => Set(value);
    }

    public ObservableCollection<StateItemViewModel> Items { get; set; } = new ObservableCollection<StateItemViewModel>();
}
