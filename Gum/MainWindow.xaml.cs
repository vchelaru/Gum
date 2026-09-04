using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Input;
using Gum.Dialogs;
using Gum.Managers;
using Gum.SelectionHistory;
using Gum.Settings;
using Gum.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using ControlzEx;

namespace Gum;

public partial class MainWindow : WindowChromeWindow, IRecipient<CloseMainWindowMessage>
{
    private readonly IMessenger _messenger;
    private readonly ISelectionHistory _selectionHistory;

    public MainWindow(
        MainWindowViewModel mainWindowViewModel,
        MenuStripManager menuStripManager,
        IMessenger messenger,
        IHotkeyManager hotkeyManager,
        ISelectionHistory selectionHistory,
        IProjectFileDropLogic projectFileDropLogic,
        IWritableOptions<LayoutSettings> layoutSettings
        )
    {
        DataContext = mainWindowViewModel;

        _messenger = messenger;
        _selectionHistory = selectionHistory;
        messenger.RegisterAll(this);

        InitializeComponent();
        menuStripManager.PopulateMenu(this.MainMenu);

        this.PreviewKeyDown += (_, e) =>
        {
            GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
            // Tunneling reaches the window before the focused element, so a render canvas that owns
            // Ctrl+=/Ctrl+- for its own camera has to opt out here rather than handle them itself.
            hotkeyManager.PreviewKeyDownAppWide(
                keyArgs,
                CameraZoomScope.IsEntireAppZoomEnabledFor(e.OriginalSource));
            e.Handled = keyArgs.Handled;
        };
        this.PreviewMouseDown += OnPreviewMouseDown;

        // Dropping a project file anywhere in the window opens it, the same as File > Load Project.
        // Tunneling handlers, so the drop wins over the wireframe canvas and the element tree, which
        // both accept file drops of their own but reject a .gumx/.gumj.
        this.AllowDrop = true;
        DragEventHandler acceptProjectDrag = (_, e) =>
        {
            if (projectFileDropLogic.GetProjectFileToOpen(GetDroppedFiles(e)) != null)
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        };
        // DragEnter as well as DragOver, so the wireframe never gets to report the drag as rejected.
        this.PreviewDragEnter += acceptProjectDrag;
        this.PreviewDragOver += acceptProjectDrag;
        this.PreviewDrop += (_, e) =>
        {
            if (projectFileDropLogic.TryOpenDroppedProject(GetDroppedFiles(e)))
            {
                e.Handled = true;
            }
        };

        this.Loaded += (_, _) =>
        {
            mainWindowViewModel.LoadWindowSettings(layoutSettings.CurrentValue.MainWindow);
        };
    }

    private static string[]? GetDroppedFiles(DragEventArgs e) =>
        e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? e.Data.GetData(DataFormats.FileDrop) as string[]
            : null;

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Mouse "back"/"forward" side buttons. Every surface in the tool is WPF, so this one
        // handler covers all of them.
        if (e.ChangedButton == MouseButton.XButton1)
        {
            _selectionHistory.NavigateBack();
            e.Handled = true;
            return;
        }
        if (e.ChangedButton == MouseButton.XButton2)
        {
            _selectionHistory.NavigateForward();
            e.Handled = true;
        }
    }

    void IRecipient<CloseMainWindowMessage>.Receive(CloseMainWindowMessage message)
    {
        Close();
    }

    private void OnMinimizeButtonClick(object? sender, System.Windows.RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeButtonClick(object? sender, System.Windows.RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseButtonClick(object? sender, System.Windows.RoutedEventArgs e)
    {
        _messenger.Send(new CloseMainWindowMessage());
    }
}

public record CloseMainWindowMessage;
public record ThemeChangedMessage(IEffectiveThemeSettings settings);


//// Need this to respect taskbar in maximized state while using custom window chrome
//public partial class MainWindow : Window
//{
//    protected override void OnSourceInitialized(EventArgs e)
//    {
//        base.OnSourceInitialized(e);
//        HwndSource source = (HwndSource)PresentationSource.FromVisual(this)!;
//        source.AddHook(WndProc);
//    }

//    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
//    {
//        const int WM_GETMINMAXINFO = 0x0024;
//        if (msg == WM_GETMINMAXINFO)
//        {
//            AdjustMaximizedSizeAndPosition(hwnd, lParam);
//            handled = true;
//        }
//        return IntPtr.Zero;
//    }

//    private static void AdjustMaximizedSizeAndPosition(IntPtr hwnd, IntPtr lParam)
//    {
//        const int MONITOR_DEFAULTTONEAREST = 0x00000002;
//        IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

//        MONITORINFO monitorInfo = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
//        GetMonitorInfo(monitor, ref monitorInfo);

//        MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
//        RECT rcWork = monitorInfo.rcWork;
//        RECT rcMonitor = monitorInfo.rcMonitor;

//        mmi.ptMaxPosition.x = rcWork.Left - rcMonitor.Left;
//        mmi.ptMaxPosition.y = rcWork.Top - rcMonitor.Top;
//        mmi.ptMaxSize.x = rcWork.Right - rcWork.Left;
//        mmi.ptMaxSize.y = rcWork.Bottom - rcWork.Top;
//        mmi.ptMaxTrackSize = mmi.ptMaxSize;

//        Marshal.StructureToPtr(mmi, lParam, true);
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    private struct POINT { public int x, y; }
//    [StructLayout(LayoutKind.Sequential)]
//    private struct MINMAXINFO
//    {
//        public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
//    }
//    [StructLayout(LayoutKind.Sequential)]
//    private struct RECT { public int Left, Top, Right, Bottom; }
//    [StructLayout(LayoutKind.Sequential)]
//    private struct MONITORINFO
//    {
//        public uint cbSize;
//        public RECT rcMonitor, rcWork;
//        public uint dwFlags;
//    }
//    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);
//    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
//}