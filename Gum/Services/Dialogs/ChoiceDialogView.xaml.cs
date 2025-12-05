using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Services.Dialogs;
/// <summary>
/// Interaction logic for ChoiceDialogView.xaml
/// </summary>
public partial class ChoiceDialogView : UserControl
{
    public ChoiceDialogView()
    {
        InitializeComponent();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Content: string content, IsChecked: true } &&
            DataContext is ChoiceDialogViewModel vm)
        {
            vm.SelectedValue = content;
        }
    }
}
