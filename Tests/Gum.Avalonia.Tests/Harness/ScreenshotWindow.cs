using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// A headless window shown for <see cref="PrScreenshot"/>: pumps frames so the view is laid out and
/// drawn, finds and clicks controls, hovers one for its tooltip, and saves the rendered frame.
/// Nothing reaches the desktop.
/// </summary>
internal sealed class ScreenshotWindow : IDisposable
{
    internal ScreenshotWindow(Window window, ThemeVariant? theme)
    {
        Window = window;
        if (theme != null)
        {
            // On the window, not the application: the variant stays scoped to this capture.
            Window.RequestedThemeVariant = theme;
        }
        Window.Show();
        Pump(Window);
    }

    public Window Window { get; }

    /// <summary>The first control of type <typeparamref name="T"/> in the window matching <paramref name="predicate"/>.</summary>
    public T Find<T>(Func<T, bool>? predicate = null) where T : Control
    {
        Pump(Window);
        return Window.GetVisualDescendants().OfType<T>().FirstOrDefault(control => predicate?.Invoke(control) ?? true)
            ?? throw new InvalidOperationException($"The window shows no matching {typeof(T).Name}.");
    }

    /// <summary>Every control of type <typeparamref name="T"/> in the window.</summary>
    public List<T> FindAll<T>() where T : Control
    {
        Pump(Window);
        return Window.GetVisualDescendants().OfType<T>().ToList();
    }

    /// <summary>Clicks the center of <paramref name="control"/>, for instance to switch tabs before a capture.</summary>
    public void Click(Control control)
    {
        control.BringIntoView();
        Point point = CenterOf(control);
        Window.MouseMove(point);
        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
        Pump(Window);
    }

    /// <summary>
    /// Moves the pointer over <paramref name="control"/>, opens its tooltip and waits in real time
    /// while pumping frames, so the tooltip's fade-in has finished before the capture.
    /// </summary>
    public void HoverForToolTip(Control control, int waitMilliseconds = 500)
    {
        control.BringIntoView();
        Window.MouseMove(CenterOf(control));
        ToolTip.SetIsOpen(control, true);
        Stopwatch elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < waitMilliseconds)
        {
            // A synchronous wait: an awaited delay needs a nested dispatcher frame, which the
            // headless session sometimes refuses.
            Thread.Sleep(10);
            Pump(Window);
        }
    }

    /// <summary>Saves the window as <paramref name="name"/>.png in the output folder; returns the path.</summary>
    public string Save(string name)
    {
        Pump(Window);
        return SaveFrame(Window, name);
    }

    public void Dispose() => Window.Close();

    private Point CenterOf(Control control)
    {
        Pump(Window);
        return control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)
            ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the window.");
    }

    /// <summary>Runs dispatcher work, lays the window out and ticks the render timer, a few times over.</summary>
    internal static void Pump(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    internal static string SaveFrame(Window window, string name)
    {
        UserPathGuard.ThrowIfShown(window);
        string folder = PrScreenshot.OutputDirectory ?? Path.Combine(Path.GetTempPath(), "GumScreenshots");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, name + ".png");
        using WriteableBitmap frame = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("The headless window rendered no frame.");
        frame.Save(path);
        return path;
    }
}
