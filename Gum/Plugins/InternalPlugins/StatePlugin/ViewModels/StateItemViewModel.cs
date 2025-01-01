using Gum.DataTypes.Variables;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;

public class StateViewModel : StateTreeViewItem
{
    public StateSave Data { get; set; }
    public bool IncludesVariablesForSelectedInstance
    {
        get => Get<bool>();
        set => Set(value);
    }


    public override object DataAsObject => Data;
    public override string Title => Data?.Name;

}
