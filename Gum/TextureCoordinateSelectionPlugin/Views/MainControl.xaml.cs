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
using TextureCoordinateSelectionPlugin.ViewModels;

namespace TextureCoordinateSelectionPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        public event EventHandler<KeyEventArgs> ImageRegionKeyDown;

        MainControlViewModel ViewModel => DataContext as MainControlViewModel;

        public MainControl()
        {
            InitializeComponent();

            // we are going to do our own handling of events
            InnerControl.DisableHotkeyPanning();
        }

        private void HandleKeyDown(object? sender, KeyEventArgs e)
        {
            ImageRegionKeyDown?.Invoke(null, e);
        }

        private void HandleMinusClicked(object? sender, RoutedEventArgs e)
        {
            ViewModel.ZoomOut();
        }

        private void HandlePlusClicked(object? sender, RoutedEventArgs e)
        {
            ViewModel.ZoomIn();
        }

        private const double DefaultBaseFontSize = 12.0;
        private const double DefaultMinWidth = 24.0;

        internal void UpdateButtonSizes(double baseFontSize)
        {
            double scale = baseFontSize / DefaultBaseFontSize;
            double minWidth = DefaultMinWidth * scale;

            MinusButton.MinWidth = minWidth;
            MinusButton.FontSize = baseFontSize;

            PlusButton.MinWidth = minWidth;
            PlusButton.FontSize = baseFontSize;
        }
    }
}
