using Gum.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Gum.Services.Dialogs;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
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