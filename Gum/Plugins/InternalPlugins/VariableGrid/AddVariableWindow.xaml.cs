using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Gum.Services.Dialogs;

namespace Gum.Plugins.VariableGrid;

/// <summary>
/// Interaction logic for AddVariableWindow.xaml
/// </summary>
[Dialog(typeof(AddVariableViewModel))]
public partial class AddVariableWindow : UserControl
{
    public AddVariableWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => TextBox.Focus();
    }
}
