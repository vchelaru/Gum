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

namespace Gum.Plugins.VariableGrid
{
    /// <summary>
    /// Interaction logic for AddVariableWindow.xaml
    /// </summary>
    public partial class AddVariableWindow : Window
    {
        public string SelectedType
        {
            get
            {
                return (ListBox.SelectedItem as ListBoxItem)?.Content as string;
            }
        }

        public string EnteredName
        {
            get
            {
                return TextBox.Text;
            }
        }

        public AddVariableWindow()
        {
            InitializeComponent();
        }

        private void HandleOkClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void HandleCancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
