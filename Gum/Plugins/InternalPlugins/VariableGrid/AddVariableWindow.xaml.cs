using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Gum.Plugins.VariableGrid;

/// <summary>
/// Interaction logic for AddVariableWindow.xaml
/// </summary>
public partial class AddVariableWindow : Window
{
    public AddVariableViewModel ViewModel { get; }

    public AddVariableWindow(AddVariableViewModel viewModel)
    {
        InitializeComponent();

        this.DataContext = viewModel;
        this.ViewModel = viewModel;

        this.Loaded += AddVariableWindow_Loaded;
    }

    private void AddVariableWindow_Loaded(object sender, RoutedEventArgs e)
    {
        GumCommands.Self.GuiCommands.MoveToCursor(this);

        this.TextBox.Focus();
    }

    private void HandleOkClicked(object sender, RoutedEventArgs e)
    {
        var response = ViewModel.Validate();
        if(!response.Succeeded)
        {
            MessageBox.Show(response.Message);
            return;
        }
        else
        {
            DialogResult = true;
        }

    }

    private void HandleCancelClicked(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }

    private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if(e.Key == System.Windows.Input.Key.Enter)
        {
            HandleOkClicked(null, null);
        }
        if(e.Key == System.Windows.Input.Key.Escape)
        {
            HandleCancelClicked(null, null);
        }
    }
}
