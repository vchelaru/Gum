using Gum.Managers;
using Gum.Services;
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

namespace Gum.Gui.Windows
{
    public partial class CreateComponentWindow : Window
    {
        private readonly INameVerifier _nameVerifier;
        
        public string Result
        {
            get => componentName.Text;
            set => componentName.Text = value;
        }

        public CreateComponentWindow(INameVerifier nameVerifier)
        {
            _nameVerifier = nameVerifier;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void componentName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_nameVerifier.IsComponentNameAlreadyUsed(componentName.Text))
            {
                okButton.IsEnabled = false;
                errorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                okButton.IsEnabled = true;
                errorLabel.Visibility = Visibility.Hidden;
            }
        }
    }
}
