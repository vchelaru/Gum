using Gum.Services;
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
        private readonly ISelectedState _selectedState;
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
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }

        private void HandleEditClick(object? sender, RoutedEventArgs e)
        {
            var component = _selectedState.SelectedComponent;
            if(component != null)
            {
                ViewModel.UpdateTo(component);
            }
            ViewModel.IsEditing = true;
        }

        private void OkClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.HandleOkEditClick();
            ViewModel.IsEditing = false;
        }


        private void CancelClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.IsEditing = false;
        }
    }
}
