using CommunityToolkit.Mvvm.Messaging;
using Gum.Controls;
using Gum.Dialogs;
using Gum.Mvvm;
using Gum.Properties;
using Gum.Settings;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

namespace Gum.ViewModels;

public class MainWindowViewModel : ViewModel, IRecipient<ThemeChangedMessage>, IRecipient<ApplicationTeardownMessage>
{
    private readonly IWritableOptions<LayoutSettings> _layoutSettings;

    public MainPanelViewModel MainPanelViewModel { get; }

    public string? IconSource
    {
        get => Get<string?>();
        set => Set(value);
    }
    public string? Title
    {
        get => Get<string?>();
        set => Set(value);   
    }

    public double Left
    {
        get => Get<double>();
        set => Set(value);
    }

    public double Top
    {
        get => Get<double>();
        set => Set(value);
    }

    public double? Width
    {
        get => Get<double?>();
        set => Set(value);
    }

    public double? Height
    {
        get => Get<double?>();
        set => Set(value);
    }

    public WindowState WindowState
    {
        get => Get<WindowState>();
        set => Set(value);
    }

    public MainWindowViewModel(MainPanelViewModel mainPanelViewModel, IMessenger messenger, IWritableOptions<LayoutSettings> layoutSettings)
    {
        _layoutSettings = layoutSettings;
        MainPanelViewModel = mainPanelViewModel;
        IconSource = "pack://application:,,,/GumLogo64.png";
        messenger.RegisterAll(this);
    }
    
    // We can't do this from the constructor yet because ProjectManager Initializes after our construction
    // and, we need to ensure ProjectManager.GeneralSettings has been loaded and migrated first.
    public void LoadWindowSettings(WindowSettings settings)
    {
        
        if (settings is { Left: null, Top: null } or {Width: 0} or {Height: 0})
        {
            // nulls should imply this is the first launch -- let wpf center the window
            // width or height 0 is invalid (how'd that happen?), so also just return
            return;
        }


        Rectangle constrained = // monitor setup may have changed since last run
            WindowPlacementHelper.ConstrainIntoWorkingArea(new Rectangle((int)settings.Left!.Value, (int)settings.Top!.Value,
                (int)settings.Width, (int)settings.Height));

        Left = constrained.Left;
        Top = constrained.Top;
        Width = constrained.Width;
        Height = constrained.Height;

        WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;
    }

    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        // todo: we should trigger this in the view instead
        IconSource = message.settings.Mode switch
        {
            ThemeMode.Light => "pack://application:,,,/GumLogo64Light.png",
            _ => "pack://application:,,,/GumLogo64.png"
        };
    }

    private void SaveWindowSettings()
    {
        _layoutSettings.Update(l =>
        {
            l.MainWindow = new WindowSettings()
            {
                Left = this.Left,
                Top = this.Top,
                Width = this.Width ?? 0,
                Height = this.Height ?? 0,
                IsMaximized = this.WindowState == WindowState.Maximized
            };
        });
    }

    void IRecipient<ApplicationTeardownMessage>.Receive(ApplicationTeardownMessage message)
    {
        message.OnTearDown(SaveWindowSettings);
    }
}

file static class WindowPlacementHelper
{
    [DllImport("Shcore.dll", SetLastError = true)]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

    private enum Monitor_DPI_Type : int { MDT_Effective_DPI = 0  }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public static Rectangle ConstrainIntoWorkingArea(Rectangle extents)
    {
        // 1) Convert saved DIPs -> px using the DPI of the monitor that had the most overlap.
        //    First, find the target monitor in device pixels.
        Rectangle savedRectPxGuess = DipToPx(extents, GetSystemDpi()); // guess; we’ll replace DPI below
        RECT rect = ToRECT(savedRectPxGuess);
        IntPtr hMon = MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST);

        double targetDpi = GetMonitorDpi(hMon);
        Rectangle savedRectPx = DipToPx(extents, targetDpi);

        // 2) Get target monitor working area (device pixels)
        MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMon, ref mi))
        {
            return extents;
        }

        RECT wa = mi.rcWork; // device pixels

        // 3) Clamp size to working area
        int widthPx = Math.Min(savedRectPx.Width, wa.Right - wa.Left);
        int heightPx = Math.Min(savedRectPx.Height, wa.Bottom - wa.Top);

        // 4) Clamp position to keep fully inside working area
        int leftPx = Math.Max(wa.Left, Math.Min(savedRectPx.Left, wa.Right - widthPx));
        int topPx = Math.Max(wa.Top, Math.Min(savedRectPx.Top, wa.Bottom - heightPx));

        // 5) Convert px -> DIPs using *target* monitor DPI
        double dpiScale = targetDpi / 96.0;
        double leftDip = leftPx / dpiScale;
        double topDip = topPx / dpiScale;
        double widthDip = widthPx / dpiScale;
        double heightDip = heightPx / dpiScale;

        return new ((int)leftDip, (int)topDip, (int)widthDip, (int)heightDip);
    }

    private static double GetSystemDpi()
    {
        Window? app = Application.Current?.MainWindow;
        if (app != null)
        {
            PresentationSource? src = PresentationSource.FromVisual(app);
            if (src?.CompositionTarget != null)
                return 96.0 * src.CompositionTarget.TransformToDevice.M11;
        }
        return 96.0;
    }

    private static double GetMonitorDpi(IntPtr hMonitor)
    {
        if (hMonitor != IntPtr.Zero && GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Effective_DPI, out uint x, out _) == 0 && x > 0)
            return x;
        return 96.0;
    }

    private static System.Drawing.Rectangle DipToPx(System.Drawing.Rectangle dipRect, double dpi)
    {
        double scale = dpi / 96.0;
        return new System.Drawing.Rectangle(
            (int)Math.Round(dipRect.Left * scale),
            (int)Math.Round(dipRect.Top * scale),
            (int)Math.Round(dipRect.Width * scale),
            (int)Math.Round(dipRect.Height * scale)
        );
    }

    private static RECT ToRECT(System.Drawing.Rectangle r) =>
        new RECT { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
}