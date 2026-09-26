using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// A headless window hosting one tool view, driven the way a user drives it: pointer and key input
/// (nothing reaches the desktop), context menus, and pixel reads of what was drawn. Shared by the
/// tab harnesses; each one owns the view it hosts and what it asserts on.
/// </summary>
internal sealed class HeadlessWindowDriver : IDisposable
{
    private readonly string _framesFolder;
    private readonly bool _contentOutlivesTest;

    /// <summary>Shows <paramref name="content"/> in a new headless window of the given size.</summary>
    /// <param name="framesFolderName">Folder under the temp folder that <see cref="SaveFrame"/> writes to, unless GUM_HEADLESS_FRAMES names one.</param>
    /// <param name="contentOutlivesTest">
    /// True for a view built once for the whole run (a singleton tab's content), which an earlier
    /// test's window may have hosted; see <see cref="ReleaseTemplatedContent"/>.
    /// </param>
    public HeadlessWindowDriver(Control content, double width, double height, string framesFolderName, bool contentOutlivesTest = false)
    {
        _framesFolder = Environment.GetEnvironmentVariable("GUM_HEADLESS_FRAMES") is { Length: > 0 } folder
            ? folder
            : Path.Combine(Path.GetTempPath(), framesFolderName, "frames");
        _contentOutlivesTest = contentOutlivesTest;
        List<ContentPresenter> released = contentOutlivesTest ? ReleaseTemplatedContent(content) : new List<ContentPresenter>();
        Window = new Window { Content = content, Width = width, Height = height };
        Window.Show();
        Layout();
        foreach (ContentPresenter presenter in released.Where(presenter => presenter.GetVisualRoot() == Window))
        {
            // The template was kept rather than rebuilt; hand the presenter its content back.
            presenter.ClearValue(ContentPresenter.ContentProperty);
            ReleasedPresenters.Remove(presenter);
        }
        Layout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        if (Window.InputHitTest(new Point(2, 2)) == null)
        {
            // Every gesture would land on nothing; see Animations/README.md, "Gotchas". Only a
            // fresh process gets an independent roll, so fail fast rather than retry here.
            Window.Content = null;
            Window.Close();
            throw new InvalidOperationException("The window hit-tests nothing after a render tick: this test's Avalonia session bound its compositor to a dispatcher that is not the current one.");
        }
    }

    /// <summary>The headless window; input goes through it.</summary>
    public Window Window { get; }

    /// <summary>The context menu a right-click opened, or null when none is open.</summary>
    public ContextMenu? OpenContextMenu =>
        Window.GetSelfAndVisualDescendants().OfType<Control>().Select(control => control.ContextMenu).FirstOrDefault(menu => menu?.IsOpen == true);

    /// <summary>Runs pending dispatcher work and lays the window out, so the view reflects the model.</summary>
    public void Layout()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public Point CenterOf(Control control)
    {
        Layout();
        return control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)
            ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the window.");
    }

    /// <summary>Scrolls <paramref name="control"/> into view, then clicks its center.</summary>
    public void Click(Control control, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        control.BringIntoView();
        ClickAt(CenterOf(control), modifiers);
    }

    public void ClickAt(Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.MouseMove(point, modifiers);
        Window.MouseDown(point, MouseButton.Left, modifiers);
        Window.MouseUp(point, MouseButton.Left, modifiers);
        Layout();
    }

    /// <summary>Right-clicks <paramref name="control"/>, which opens its context menu.</summary>
    public void RightClick(Control control)
    {
        control.BringIntoView();
        Point point = CenterOf(control);
        Window.MouseMove(point, RawInputModifiers.None);
        Window.MouseDown(point, MouseButton.Right, RawInputModifiers.None);
        Window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);
        Layout();
    }

    /// <summary>The headers of the open context menu's items.</summary>
    public List<string> ContextMenuHeaders() =>
        (OpenContextMenu ?? throw new InvalidOperationException("No context menu is open."))
            .Items.OfType<MenuItem>().Select(item => item.Header?.ToString() ?? "").ToList();

    /// <summary>
    /// Picks <paramref name="header"/> from the open context menu; a click when the popup laid the
    /// item out, else the item's own click event.
    /// </summary>
    public void PickContextMenuItem(string header)
    {
        ContextMenu menu = OpenContextMenu ?? throw new InvalidOperationException("No context menu is open.");
        MenuItem item = menu.Items.OfType<MenuItem>().SingleOrDefault(candidate => candidate.Header?.ToString() is string text && (text == header || text.StartsWith(header + " (", StringComparison.Ordinal)))
            ?? throw new InvalidOperationException($"The context menu has no \"{header}\"; it has [{string.Join(", ", ContextMenuHeaders())}].");
        if (!item.IsEnabled)
        {
            throw new InvalidOperationException($"The context menu item \"{header}\" is disabled.");
        }
        if (item.IsEffectivelyVisible && item.Bounds.Width > 0)
        {
            ClickAt(CenterOf(item));
        }
        else
        {
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        menu.Close();
        Layout();
    }

    public void Hover(Point point)
    {
        Window.MouseMove(point, RawInputModifiers.None);
        Layout();
    }

    /// <summary>Presses the mouse at <paramref name="from"/>, moves it to <paramref name="to"/> in <paramref name="steps"/> moves and releases.</summary>
    public void Drag(Point from, Point to, int steps = 2)
    {
        Window.MouseMove(from, RawInputModifiers.None);
        Window.MouseDown(from, MouseButton.Left, RawInputModifiers.None);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            Window.MouseMove(new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t), RawInputModifiers.LeftMouseButton);
        }
        Window.MouseUp(to, MouseButton.Left, RawInputModifiers.None);
        Layout();
    }

    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPress(key, modifiers, physicalKey, null);
        Window.KeyRelease(key, modifiers, physicalKey, null);
        Layout();
    }

    /// <summary>Clicks into <paramref name="box"/>, selects its text and types <paramref name="text"/> over it; nothing is committed.</summary>
    public void TypeInto(TextBox box, string text)
    {
        Click(box);
        box.Focus();
        box.SelectAll();
        Window.KeyTextInput(text);
        Layout();
    }

    /// <summary>Types <paramref name="text"/> over <paramref name="box"/>'s text and presses Enter.</summary>
    public void TypeAndEnter(TextBox box, string text)
    {
        TypeInto(box, text);
        Press(Key.Enter, PhysicalKey.Enter);
    }

    /// <summary>The rendered color at <paramref name="point"/> in the window.</summary>
    public Color PixelAt(Point point)
    {
        Layout();
        using WriteableBitmap frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless window rendered no frame.");
        return ReadPixel(frame, (int)point.X, (int)point.Y);
    }

    /// <summary>True when any pixel within <paramref name="radius"/> of <paramref name="center"/> satisfies <paramref name="matches"/>.</summary>
    public bool AnyPixelNear(Point center, int radius, Func<Color, bool> matches)
    {
        Layout();
        using WriteableBitmap frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless window rendered no frame.");
        for (int y = (int)center.Y - radius; y <= (int)center.Y + radius; y++)
        {
            for (int x = (int)center.X - radius; x <= (int)center.X + radius; x++)
            {
                if (x < 0 || y < 0 || x >= frame.PixelSize.Width || y >= frame.PixelSize.Height)
                {
                    continue;
                }
                if (matches(ReadPixel(frame, x, y)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static Color ReadPixel(WriteableBitmap frame, int x, int y)
    {
        using ILockedFramebuffer buffer = frame.Lock();
        int value = System.Runtime.InteropServices.Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
        byte b0 = (byte)value, b1 = (byte)(value >> 8), b2 = (byte)(value >> 16), b3 = (byte)(value >> 24);
        return buffer.Format == PixelFormat.Rgba8888
            ? Color.FromArgb(b3, b0, b1, b2)
            : Color.FromArgb(b3, b2, b1, b0);
    }

    /// <summary>Renders the window and saves it as a PNG for a person to look at; returns the path.</summary>
    public string SaveFrame(string name)
    {
        Layout();
        Directory.CreateDirectory(_framesFolder);
        string path = Path.Combine(_framesFolder, name + ".png");
        using WriteableBitmap frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The headless window rendered no frame.");
        frame.Save(path);
        return path;
    }

    /// <summary>
    /// Frees a view that outlives the test for another window. Each test runs in its own Avalonia
    /// session, so the next host re-applies every control's template, but the old template's
    /// presenters still hold the controls' content as their visual child and the new presenters
    /// then fail with "already has a visual parent". Clearing the old presenters (and whatever
    /// presenter hosts the view itself, such as a closed main window's tab) releases it.
    /// </summary>
    public static List<ContentPresenter> ReleaseTemplatedContent(Control view)
    {
        if (view.GetVisualParent() is ContentPresenter host)
        {
            // Nothing hosts this presenter again (the main window cannot be re-shown), so it is not handed back.
            host.Content = null;
        }
        List<ContentPresenter> released = new List<ContentPresenter>();
        foreach (ContentPresenter presenter in view.GetVisualDescendants().OfType<ContentPresenter>().ToList())
        {
            // A presenter released when an earlier window closed still counts: the next host may keep its template.
            if (presenter.TemplatedParent != null && (presenter.Child != null || ReleasedPresenters.TryGetValue(presenter, out _)))
            {
                presenter.Content = null;
                ReleasedPresenters.AddOrUpdate(presenter, new object());
                released.Add(presenter);
            }
        }
        return released;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ContentPresenter, object> ReleasedPresenters = new();

    /// <summary>Closes the window and detaches its content, so the view can be hosted again.</summary>
    public void Dispose()
    {
        Control? content = Window.Content as Control;
        Window.Content = null;
        Window.Close();
        if (_contentOutlivesTest && content != null)
        {
            // So a later session (the main window, another harness) can host it.
            ReleaseTemplatedContent(content);
        }
    }
}
