using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System.Windows.Controls;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// Interaction logic for AnchorControl.xaml
    /// </summary>
    public partial class AnchorControl : UserControl
    {
        private readonly ISelectedState _selectedState;
        
        AlignmentViewModel ViewModel => (AlignmentViewModel)DataContext;

        StateSave CurrentState
        {
            get
            {
                if(_selectedState.SelectedStateSave != null)
                {
                    return _selectedState.SelectedStateSave;
                }
                else
                {
                    return _selectedState.SelectedElement?.DefaultState;
                }
            }
        }

        public AnchorControl()
        {
            InitializeComponent();
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }

        private void TopLeftButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.TopLeftButton_Click();
        }


        private void TopButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.TopButton_Click();
        }

        private void TopRightButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.TopRightButton_Click();
        }

        private void MiddleLeftButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.MiddleLeftButton_Click();
        }

        private void MiddleMiddleButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.MiddleMiddleButton_Click();
        }

        private void MiddleRightButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.MiddleRightButton_Click();
        }

        private void BottomLeftButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.BottomLeftButton_Click();
        }

        private void BottomMiddleButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.BottomMiddleButton_Click();
        }

        private void BottomRightButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.BottomRightButton_Click();
        }

        private void AnchorCenterHorizontally_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.AnchorCenterHorizontally_Click();
        }

        private void AnchorCenterVertically_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.AnchorCenterVertically_Click();
        }
    }
}
