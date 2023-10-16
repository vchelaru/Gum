using Gum.ToolStates;
using System.Windows;
using System.Windows.Controls;

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
            var component = GumState.Self.SelectedState.SelectedComponent;
            if(component != null)
            {
                ViewModel.UpdateTo(component);
            }
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
