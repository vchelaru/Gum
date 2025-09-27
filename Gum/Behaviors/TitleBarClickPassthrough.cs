using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

namespace Gum.Behaviors;

public static class TitleBarClickPassthrough
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(TitleBarClickPassthrough),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement el)
        {
            if ((bool)e.NewValue)
            {
                el.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                el.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;
            }
            else
            {
                el.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                el.PreviewMouseRightButtonUp -= OnPreviewMouseRightButtonUp;
            }
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var el = sender as UIElement;
        var win = Window.GetWindow(el);
        if (win is null) return;

        // Handle double-click first: toggle maximize/restore (like caption)
        if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
        {
            if (win.WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(win);
            else
                SystemCommands.MaximizeWindow(win);

            e.Handled = true;
            return; // don't start a drag on the double-click
        }

        // Single-click: start window drag (caption behavior)
        if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left)
        {
            try { win.DragMove(); }
            catch { /* ignore transient cases */ }
            e.Handled = true;
        }
    }

    private static void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not UIElement el) return;
        var win = Window.GetWindow(el);
        if (win is null) return;

        // 1) Cursor in physical pixels
        var ptPx = NativeCursor.GetScreenPixels();

        // 2) Convert pixels -> DIPs using this window’s transform (per-monitor DPI safe)
        var src = PresentationSource.FromVisual(win);
        if (src?.CompositionTarget is not null)
        {
            var fromDevice = src.CompositionTarget.TransformFromDevice;
            var ptDip = fromDevice.Transform(ptPx);

            // 3) Show system menu at DIP screen point
            SystemCommands.ShowSystemMenu(win, ptDip);
            e.Handled = true;
            return;
        }

        // Fallback (rare): use WPF’s own conversion path
        var posInWin = e.GetPosition(win);
        var ptScreenDip = win.PointToScreen(posInWin);
        SystemCommands.ShowSystemMenu(win, ptScreenDip);
        e.Handled = true;
    }

    static class NativeCursor
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        public static Point GetScreenPixels()
        {
            GetCursorPos(out var p);
            return new Point(p.X, p.Y); // physical pixels
        }
    }
}
