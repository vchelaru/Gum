using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FlatRedBall.SpecializedXnaControls;
using Gum.Input;
using TextureCoordinateSelectionPlugin.ViewModels;

namespace TextureCoordinateSelectionPlugin.Views
{
    /// <summary>
    /// The WPF texture-coordinates tab: the XAML toolbar, the WPF canvas, and its scroll bars,
    /// presented to the display controller as <see cref="ITextureCoordinateView"/>.
    /// </summary>
    public partial class MainControl : UserControl, ITextureCoordinateView
    {
        private const double DefaultBaseFontSize = 12.0;
        private const double DefaultMinWidth = 24.0;

        MainControlViewModel? ViewModel => DataContext as MainControlViewModel;

        public MainControl()
        {
            InitializeComponent();

            // we are going to do our own handling of events
            InnerControl.Core.DisableHotkeyPanning();
            VerticalScrollBar = new WpfCameraScrollBar(VerticalScrollBarElement);
            HorizontalScrollBar = new WpfCameraScrollBar(HorizontalScrollBarElement);
            InnerControl.SizeChanged += (_, _) => CanvasResized?.Invoke();
            InnerControl.KeyDown += HandleInnerKeyDown;
        }

        /// <inheritdoc/>
        public ImageRegionSelectionCore Canvas => InnerControl.Core;

        /// <inheritdoc/>
        public object View => this;

        /// <inheritdoc/>
        public ICameraScrollBar VerticalScrollBar { get; }

        /// <inheritdoc/>
        public ICameraScrollBar HorizontalScrollBar { get; }

        /// <inheritdoc/>
        public event Action? CanvasResized;

        /// <inheritdoc/>
        public new event Action<GumKeyEventArgs>? KeyDown;

        /// <inheritdoc/>
        public void InvokeWhenLoaded(Action action) => Dispatcher.BeginInvoke(action, DispatcherPriority.Loaded);

        private void HandleInnerKeyDown(object? sender, KeyEventArgs e)
        {
            GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
            KeyDown?.Invoke(keyArgs);
            e.Handled = keyArgs.Handled;
        }

        private void HandleMinusClicked(object? sender, RoutedEventArgs e)
        {
            ViewModel?.ZoomOut();
        }

        private void HandlePlusClicked(object? sender, RoutedEventArgs e)
        {
            ViewModel?.ZoomIn();
        }

        /// <inheritdoc/>
        public void UpdateButtonSizes(double baseFontSize)
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
