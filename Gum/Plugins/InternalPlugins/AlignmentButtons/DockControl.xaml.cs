using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.ToolStates;
using Gum.Undo;
using System.Windows.Controls;

namespace Gum.Plugins.AlignmentButtons
{
    
    /// <summary>
    /// Interaction logic for AlignmentControl.xaml
    /// </summary>
    public partial class DockControl : UserControl
    {
        AlignmentViewModel ViewModel => (AlignmentViewModel)DataContext;
        public DockControl()
        {
            InitializeComponent();
        }

        private void TopButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockTopButton_Click();
        }

        private void SizeToChildren_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.SizeToChildren_Click();
        }

        private void LeftButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockLeftButton_Click();
        }

        private void FillButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockFillButton_Click();
        }

        private void RightButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockRightButton_Click();
        }

        private void BottomButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockBottomButton_Click();
        }

        private void FillVerticallyButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockFillVerticallyButton_Click();
        }

        private void FillHorizontallyButton_Click(object? sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.DockFillHorizontallyButton_Click();
        }
    }
}
