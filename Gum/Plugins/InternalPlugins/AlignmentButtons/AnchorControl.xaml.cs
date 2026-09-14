using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// The anchor buttons; each forwards to the matching <see cref="AlignmentViewModel"/> method.
    /// </summary>
    public partial class AnchorControl : UserControl
    {
        AlignmentViewModel ViewModel => (AlignmentViewModel)DataContext;

        public AnchorControl()
        {
            InitializeComponent();
        }

        private void TopLeftButton_Click(object? sender, RoutedEventArgs e) => ViewModel.TopLeftButton_Click();

        private void TopButton_Click(object? sender, RoutedEventArgs e) => ViewModel.TopButton_Click();

        private void TopRightButton_Click(object? sender, RoutedEventArgs e) => ViewModel.TopRightButton_Click();

        private void MiddleLeftButton_Click(object? sender, RoutedEventArgs e) => ViewModel.MiddleLeftButton_Click();

        private void MiddleMiddleButton_Click(object? sender, RoutedEventArgs e) => ViewModel.MiddleMiddleButton_Click();

        private void MiddleRightButton_Click(object? sender, RoutedEventArgs e) => ViewModel.MiddleRightButton_Click();

        private void BottomLeftButton_Click(object? sender, RoutedEventArgs e) => ViewModel.BottomLeftButton_Click();

        private void BottomMiddleButton_Click(object? sender, RoutedEventArgs e) => ViewModel.BottomMiddleButton_Click();

        private void BottomRightButton_Click(object? sender, RoutedEventArgs e) => ViewModel.BottomRightButton_Click();

        private void AnchorCenterHorizontally_Click(object? sender, RoutedEventArgs e) => ViewModel.AnchorCenterHorizontally_Click();

        private void AnchorCenterVertically_Click(object? sender, RoutedEventArgs e) => ViewModel.AnchorCenterVertically_Click();
    }
}
