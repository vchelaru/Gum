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

    /// <summary>Shows <paramref name="content"/> in a new headless window of the given size.</summary>
    /// <param name="framesFolderName">Folder under the temp folder that <see cref="SaveFrame"/> writes to, unless GUM_HEADLESS_FRAMES names one.</param>
    public HeadlessWindowDriver(Control content, double width, double height, string framesFolderName)
    {
        _framesFolder = Environment.GetEnvironmentVariable("GUM_HEADLESS_FRAMES") is { Length: > 0 } folder
            ? folder
            : Path.Combine(Path.GetTempPath(), framesFolderName, "frames");
        DetachFromHost(content);
        Window = new Window { Content = content, Width = width, Height = height };
        Window.Show();
        Layout();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        if (Window.InputHitTest(new Point(2, 2)) == null)
        {
            // TEMP #5360 diagnostic: what state is the compositor in, and does more pumping recover it?
            string before = Diag5360();
            int recoveredAfter = -1;
            for (int i = 1; i <= 10; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                if (Window.InputHitTest(new Point(2, 2)) != null)
                {
                    recoveredAfter = i;
                    break;
                }
            }
            string after = Diag5360();
            // Every gesture would land on nothing; see Animations/README.md, "Gotchas".
            Dispose();
            throw new InvalidOperationException($"The window hit-tests nothing after a render tick. DIAG5360 before=[{before}] recoveredAfterRounds={recoveredAfter} after=[{after}]");
        }
        s_firstDispatcher ??= Dispatcher.UIThread;
    }

    private static Dispatcher? s_firstDispatcher;

    // TEMP #5360 diagnostic.
    private string Diag5360()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        void Add(string name, Func<object?> read)
        {
            try { sb.Append(name).Append('=').Append(read() ?? "null").Append("; "); }
            catch (Exception e) { sb.Append(name).Append("=!").Append(e.GetType().Name).Append("; "); }
        }
        const System.Reflection.BindingFlags Any = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        object? Field(object? target, string name) => target?.GetType().GetField(name, Any)?.GetValue(target);
        object? Prop(object? target, string name) => target?.GetType().GetProperty(name, Any)?.GetValue(target);
        Add("sameDispatcher", () => s_firstDispatcher == null ? "first" : ReferenceEquals(s_firstDispatcher, Dispatcher.UIThread));
        Add("checkAccess", () => Dispatcher.UIThread.CheckAccess());
        Add("thread", () => Environment.CurrentManagedThreadId);
        Add("visible", () => Window.IsVisible);
        Add("bounds", () => Window.Bounds);
        Add("clientSize", () => Window.ClientSize);
        Add("compVisual", () => Prop(Window, "CompositionVisual") != null);
        object? mediaContext = null;
        Add("mediaContext", () => (mediaContext = typeof(Window).Assembly.GetType("Avalonia.Media.MediaContext")?.GetProperty("Instance", Any)?.GetValue(null)) != null);
        Add("pendingBatches", () => Prop(Field(mediaContext, "_pendingCompositionBatches"), "Count"));
        Add("requestedCommits", () => Prop(Field(mediaContext, "_requestedCommits"), "Count"));
        Add("renderQueued", () => Field(mediaContext, "_nextRenderOp") != null);
        Add("mcDispatcherSame", () => ReferenceEquals(Field(mediaContext, "_dispatcher"), Dispatcher.UIThread));
        object? timer = null;
        Add("timerLookup", () =>
        {
            object? locator = typeof(AvaloniaObject).Assembly.GetType("Avalonia.AvaloniaLocator")!.GetProperty("Current", Any)!.GetValue(null);
            System.Reflection.MethodInfo getService = locator!.GetType().GetMethod("GetService", Any, new[] { typeof(Type) })!;
            timer = getService.Invoke(locator, new object[] { typeof(global::Avalonia.Rendering.IRenderTimer) });
            return timer != null;
        });
        Add("timerType", () => timer?.GetType().Name);
        Add("forceTickSet", () => Field(timer, "_forceTick") != null);
        object? renderer = Prop(Window, "Renderer");
        Add("rendererType", () => renderer?.GetType().Name);
        object? target = Prop(renderer, "CompositionTarget");
        Add("targetRoot", () => Prop(target, "Root") != null);
        Add("targetSize", () => Prop(target, "Size"));
        Add("targetScaling", () => Prop(target, "Scaling"));
        Add("rendererQueued", () => Field(renderer, "_queuedUpdate"));
        Add("rendererDirty", () => Prop(Field(renderer, "_dirty"), "Count"));
        Add("hitVisuals", () => { object? r = target?.GetType().GetMethod("TryHitTest")?.Invoke(target, new object?[] { new Point(2, 2), null, null }); return r == null ? "nullList" : Prop(r, "Count"); });
        return sb.ToString();
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
    /// Takes a view that outlives the test (a singleton tab's content) out of whatever still holds
    /// it, such as the tab of a main window an earlier test showed and closed, so a window can host it.
    /// </summary>
    public static void DetachFromHost(Control view)
    {
        if (view.GetVisualParent() is ContentPresenter host)
        {
            host.Content = null;
        }
    }

    /// <summary>Closes the window and detaches its content, so a later window can host the view.</summary>
    public void Dispose()
    {
        Window.Content = null;
        Window.Close();
    }
}
