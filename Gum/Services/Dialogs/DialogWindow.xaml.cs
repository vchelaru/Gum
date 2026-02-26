using Gum.Controls;
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
    /// <summary>
    /// When true, the window keeps its explicit Width/Height and does not reset
    /// SizeToContent to WidthAndHeight on load. Use this for dialogs whose content
    /// contains a fill-height layout (e.g. a ListBox in a star row) that must
    /// receive a finite measure constraint to scroll correctly.
    /// </summary>
    public bool UseExplicitSize { get; set; }

    public DialogWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += OnLoaded;
    }

    // This hacks around some artifacts that present when using custom WindowChrome
    // when you want your window to size to content and center itself
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var mainWindow = Application.Current.MainWindow;
        if (!UseExplicitSize)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
        }
        if(mainWindow != null)
        {
            Left = mainWindow.Left + ((mainWindow.ActualWidth / 2) - Width / 2);
            Top = mainWindow.Top + ((mainWindow.ActualHeight / 2) - Height / 2);
        }
        Loaded -= OnLoaded;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
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
        else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (DataContext is MessageDialogViewModel messageVm && !string.IsNullOrEmpty(messageVm.Message))
            {
                TrySetClipboardText(messageVm.Message);
                e.Handled = true;
            }
        }
    }

    private static void TrySetClipboardText(string text, int retries = 3)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                if (i == retries - 1)
                    throw;
                System.Threading.Thread.Sleep(10);
            }
        }
    }

    private void ScrollViewer_PreviewMouseWheel(object? sender, MouseWheelEventArgs e)
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