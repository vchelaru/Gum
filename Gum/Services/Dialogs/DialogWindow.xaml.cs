using System;
using Gum.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Application = System.Windows.Application;
using System.Windows.Media;

namespace Gum.Services.Dialogs;

public partial class DialogWindow : Window
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
        SizeToContent = SizeToContent.WidthAndHeight;
        Left = Application.Current.MainWindow.Left + ((Application.Current.MainWindow.ActualWidth / 2) - Width / 2);
        Top = Application.Current.MainWindow.Top + ((Application.Current.MainWindow.ActualHeight / 2) - Height / 2);
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