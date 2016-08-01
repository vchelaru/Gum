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

namespace Gum.Plugins.Behaviors
{
    /// <summary>
    /// Interaction logic for BehaviorsControl.xaml
    /// </summary>
    public partial class BehaviorsControl : UserControl
    {
        BehaviorsViewModel ViewModel
        {
            get
            {
                return DataContext as BehaviorsViewModel;
            }
        }
        public BehaviorsControl()
        {
            InitializeComponent();
        }

        private void HandleEditClick(object sender, RoutedEventArgs e)
        {
            ViewModel.IsEditing = true;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            ViewModel.HandleOkEditClick();
            ViewModel.IsEditing = false;
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
            ViewModel.IsEditing = false;
        }
    }
}
