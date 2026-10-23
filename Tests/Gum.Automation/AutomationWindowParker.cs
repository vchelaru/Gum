using System.Reflection;
using System.Runtime.InteropServices;

namespace Gum.Automation;

/// <summary>The native window operations <see cref="AutomationWindowParker"/> needs. Faked in tests.</summary>
public interface IWindowPlacer
{
    /// <summary>The app's main window, or <see cref="IntPtr.Zero"/> if it does not exist yet.</summary>
    IntPtr MainWindow { get; }

    /// <summary>Whether <paramref name="window"/> currently owns keyboard focus.</summary>
    bool IsForeground(IntPtr window);

    /// <summary>The nearest visible, titled window beneath <paramref name="window"/> in z-order, or zero.</summary>
    IntPtr WindowBelow(IntPtr window);

    /// <summary>Moves <paramref name="window"/> off every monitor and to the bottom of the z-order, without activating it.</summary>
    void MoveOffScreenToBottom(IntPtr window);

    /// <summary>Gives keyboard focus to <paramref name="window"/>.</summary>
    void Focus(IntPtr window);
}

/// <summary>
/// Keeps an automation run's window off-screen and at the back of the z-order so it never covers the
/// user's work, and hands focus back once to the window that lost it. Ported from FlatRedBall2's
/// <c>AutomationWindowParker</c>.
/// </summary>
/// <remarks>
/// The window is created (and focused) before any app code runs, so this can only react afterwards;
/// call <see cref="Tick"/> every frame because MonoGame re-centers the window when it resizes it.
/// Input never comes from the OS: the scripted cursor and keyboard are installed in-process.
/// </remarks>
public sealed class AutomationWindowParker
{
    private readonly IWindowPlacer _placer;
    private bool _focusHandled;

    /// <summary>Creates a parker over <paramref name="placer"/>.</summary>
    public AutomationWindowParker(IWindowPlacer placer) => _placer = placer;

    /// <summary>A parker using Win32 on Windows, otherwise null (parking is Windows-only).</summary>
    public static AutomationWindowParker? CreateForCurrentPlatform() =>
        OperatingSystem.IsWindows() ? new AutomationWindowParker(new Win32WindowPlacer()) : null;

    /// <summary>The window that owns keyboard focus (Windows), otherwise zero.</summary>
    public static IntPtr ForegroundWindow() =>
        OperatingSystem.IsWindows() ? Win32WindowPlacer.Foreground() : IntPtr.Zero;

    /// <summary>Parks the window; the first call with a window also returns focus to the window below.</summary>
    public void Tick()
    {
        IntPtr window = _placer.MainWindow;
        if (window == IntPtr.Zero)
        {
            return;
        }

        if (!_focusHandled)
        {
            _focusHandled = true;
            // Read what is beneath before moving: moving to the bottom changes the answer.
            if (_placer.IsForeground(window))
            {
                IntPtr below = _placer.WindowBelow(window);
                _placer.MoveOffScreenToBottom(window);
                if (below != IntPtr.Zero)
                {
                    _placer.Focus(below);
                }
                return;
            }
        }

        _placer.MoveOffScreenToBottom(window);
    }

    /// <summary>
    /// Marks a MonoGame or KNI <c>Game</c> active although its window is unfocused. Forms skips input
    /// while <c>Game.IsActive</c> is false, and handing focus back to the user makes it false. The
    /// setter is internal (on the game or its Platform/Strategy object), hence reflection. Returns
    /// whether a setter was found.
    /// </summary>
    public static bool ForceActive(object? game)
    {
        if (game == null)
        {
            return false;
        }
        if (TrySetIsActive(game))
        {
            return true;
        }
        object? platform = GetNonPublicMember(game, "Platform") ?? GetNonPublicMember(game, "Strategy");
        return platform != null && TrySetIsActive(platform);
    }

    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // Each level is searched on its own: reflecting through the most-derived type hides a base
    // class's internal members when the base lives in another assembly.
    private static bool TrySetIsActive(object target)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            MethodInfo? setter = type.GetProperty("IsActive", Declared)?.GetSetMethod(nonPublic: true);
            if (setter != null)
            {
                setter.Invoke(target, new object[] { true });
                return true;
            }
            FieldInfo? field = type.GetField("_isActive", Declared);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(target, true);
                return true;
            }
        }
        return false;
    }

    private static object? GetNonPublicMember(object target, string name)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, Declared);
            if (field != null)
            {
                return field.GetValue(target);
            }
            PropertyInfo? property = type.GetProperty(name, Declared);
            if (property != null)
            {
                return property.GetValue(target);
            }
        }
        return null;
    }
}

internal sealed class Win32WindowPlacer : IWindowPlacer
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint GwHwndNext = 2;
    private const int OffScreen = -32000;
    private static readonly IntPtr HwndBottom = (IntPtr)1;

    public IntPtr MainWindow => System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

    public bool IsForeground(IntPtr window) => GetForegroundWindow() == window;

    internal static IntPtr Foreground() => GetForegroundWindow();

    public IntPtr WindowBelow(IntPtr window)
    {
        for (IntPtr next = GetWindow(window, GwHwndNext); next != IntPtr.Zero; next = GetWindow(next, GwHwndNext))
        {
            // Skip invisible, minimized and untitled windows: focusing one would not give the user their app back.
            if (IsWindowVisible(next) && !IsIconic(next) && GetWindowTextLength(next) > 0)
            {
                return next;
            }
        }
        return IntPtr.Zero;
    }

    public void MoveOffScreenToBottom(IntPtr window) =>
        SetWindowPos(window, HwndBottom, OffScreen, OffScreen, 0, 0, SwpNoSize | SwpNoActivate);

    public void Focus(IntPtr window) => SetForegroundWindow(window);

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
