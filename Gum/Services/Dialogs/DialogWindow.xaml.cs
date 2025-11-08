using Gum.Controls;
using SharpDX.XInput;
using System;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ControlzEx;
using Application = System.Windows.Application;

namespace Gum.Services.Dialogs;

public partial class DialogWindow : WindowChromeWindow
{
    public DialogWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += OnLoaded;
    }

    // This hacks around some artifacts that present when using custom WindowChrome
    // when you want your window to size to content and center itself
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = Application.Current.MainWindow;
        if(mainWindow != null)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            Left = mainWindow.Left + ((mainWindow.ActualWidth / 2) - Width / 2);
            Top = mainWindow.Top + ((mainWindow.ActualHeight / 2) - Height / 2);
        }
        Loaded -= OnLoaded;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is DialogViewModel vm)
            {
                vm.NegativeCommand.Execute(null);
            }
            else
            {
                Close();
            }
        }
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        DependencyObject? origin = e.OriginalSource as DependencyObject;

        if (TreeHelpers.FindVisualAncestor<ScrollViewer>(origin) is { } inner)
        {
            bool innerCanScroll =
                e.Delta < 0 ? inner.VerticalOffset < inner.ScrollableHeight
                              : inner.VerticalOffset > 0;

            if (innerCanScroll) return;
        }
       
        ScrollViewer outer = (ScrollViewer)sender;
        outer.ScrollToVerticalOffset(
            outer.VerticalOffset - Math.Sign(e.Delta) * SystemParameters.WheelScrollLines);
        e.Handled = true;
    }
}