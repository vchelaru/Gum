using GumFormsPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GumFormsPlugin.Views
{
    /// <summary>
    /// Interaction logic for AddFormsWindow.xaml
    /// </summary>
    public partial class AddFormsWindow : Window
    {
        AddFormsViewModel ViewModel => DataContext as AddFormsViewModel;
        public AddFormsWindow(AddFormsViewModel addFormsViewModel)
        {
            InitializeComponent();

            DataContext = addFormsViewModel;
        }

        private void OkButtonClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.DoIt();
            this.DialogResult = true;

        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
