using System.Reflection;
using Gum.Forms;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Automation;

/// <summary>Thrown when a driver call or script line cannot do what it was asked.</summary>
public sealed class GumAutomationException : Exception
{
    /// <summary>Creates the exception.</summary>
    public GumAutomationException(string message) : base(message) { }
}

/// <summary>
/// Drives Gum Forms in-process: finds elements by name, moves and clicks a <see cref="ScriptedCursor"/>,
/// types through a <see cref="ScriptedKeyboard"/>, steps frames and reads properties. Nothing here
/// reads or moves the OS mouse or keyboard.
/// </summary>
/// <remarks>
/// Every input call runs the frames it needs through the host's frame callback, so a call returns
/// after Gum has processed it: <see cref="Click(string, MouseButton)"/> is a move frame, a push frame
/// and a release frame. Call <see cref="Install"/> once before driving.
/// </remarks>
public sealed class GumDriver
{
    private readonly Action _runOneFrame;
    private readonly Func<IEnumerable<GraphicalUiElement?>> _roots;

    /// <summary>Creates a driver over any host.</summary>
    /// <param name="runOneFrame">Runs one frame of the host, which must call Gum's Forms update.</param>
    /// <param name="roots">The roots searched by name, topmost first (modal, popup, main).</param>
    public GumDriver(Action runOneFrame, Func<IEnumerable<GraphicalUiElement?>> roots)
    {
        _runOneFrame = runOneFrame;
        _roots = roots;
    }

    /// <summary>
    /// A driver whose frames call <paramref name="service"/>'s Update with a fixed 1/60 s step, for
    /// headless tests. Game time is deterministic.
    /// </summary>
    public static GumDriver ForGumService(GumService service)
    {
        TimeSpan step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        TimeSpan total = TimeSpan.Zero;
        return new GumDriver(
            () =>
            {
                total += step;
                service.Update(new GameTime(total, step));
            },
            () => RootsOf(service));
    }

    /// <summary>The modal, popup and main roots of <paramref name="service"/>, topmost first.</summary>
    public static IEnumerable<GraphicalUiElement?> RootsOf(GumService service) =>
        new GraphicalUiElement?[] { service.ModalRoot, service.PopupRoot, service.Root };

    /// <summary>The cursor Forms reads once <see cref="Install"/> has run.</summary>
    public ScriptedCursor Cursor { get; } = new ScriptedCursor();

    /// <summary>The keyboard Forms reads once <see cref="Install"/> has run.</summary>
    public ScriptedKeyboard Keyboard { get; } = new ScriptedKeyboard();

    /// <summary>Frames run through this driver so far.</summary>
    public int FrameCount { get; private set; }

    /// <summary>
    /// Writes the current frame to a PNG path. Null until the host sets it; a headless host has none.
    /// </summary>
    public Action<string>? ScreenshotWriter { get; set; }

    /// <summary>Makes Forms read <see cref="Cursor"/> and <see cref="Keyboard"/>.</summary>
    public void Install()
    {
        FormsUtilities.SetCursor(Cursor);
        FormsUtilities.SetKeyboard(Keyboard);
    }

    /// <summary>Runs <paramref name="frames"/> frames.</summary>
    public void Step(int frames = 1)
    {
        for (int i = 0; i < frames; i++)
        {
            _runOneFrame();
            FrameCount++;
        }
    }

    /// <summary>The first element named <paramref name="name"/>, searching the roots topmost first.</summary>
    /// <exception cref="GumAutomationException">No element has that name.</exception>
    public GraphicalUiElement Find(string name)
    {
        foreach (GraphicalUiElement? root in _roots())
        {
            if (root == null)
            {
                continue;
            }
            if (root.Name == name)
            {
                return root;
            }
            GraphicalUiElement? found = root.FindByName(name);
            if (found != null)
            {
                return found;
            }
        }
        throw new GumAutomationException($"No element named '{name}' under any root.");
    }

    /// <summary>
    /// The screen pixel at the center of <paramref name="name"/>: its world center mapped through the
    /// default renderer's camera zoom and position and the viewport offset, the inverse of how the
    /// cursor maps screen to world.
    /// </summary>
    public (int X, int Y) CenterOf(string name)
    {
        GraphicalUiElement element = Find(name);
        float worldX = element.AbsoluteLeft + element.AbsoluteWidth / 2;
        float worldY = element.AbsoluteTop + element.AbsoluteHeight / 2;
        Renderer? renderer = SystemManagers.Default?.Renderer;
        if (renderer == null)
        {
            return ((int)worldX, (int)worldY);
        }
        Camera camera = renderer.Camera;
        Rectangle? viewport = renderer.GraphicsDevice?.Viewport.Bounds;
        return ((int)((worldX - camera.X) * camera.Zoom + (viewport?.Left ?? 0)),
            (int)((worldY - camera.Y) * camera.Zoom + (viewport?.Top ?? 0)));
    }

    /// <summary>Moves the cursor and runs one frame.</summary>
    public void MoveTo(int x, int y)
    {
        Cursor.MoveTo(x, y);
        Step();
    }

    /// <summary>Moves the cursor to the center of <paramref name="name"/> and runs one frame.</summary>
    public void MoveTo(string name)
    {
        (int x, int y) = CenterOf(name);
        MoveTo(x, y);
    }

    /// <summary>Presses or releases <paramref name="button"/> where the cursor is, and runs one frame.</summary>
    public void SetButton(MouseButton button, bool held)
    {
        Cursor.SetButton(button, held);
        Step();
    }

    /// <summary>Moves to (<paramref name="x"/>, <paramref name="y"/>), pushes and releases: three frames.</summary>
    public void Click(int x, int y, MouseButton button = MouseButton.Primary)
    {
        MoveTo(x, y);
        SetButton(button, held: true);
        SetButton(button, held: false);
    }

    /// <summary>Clicks the center of <paramref name="name"/>.</summary>
    public void Click(string name, MouseButton button = MouseButton.Primary)
    {
        (int x, int y) = CenterOf(name);
        Click(x, y, button);
    }

    /// <summary>
    /// Pushes on the center of <paramref name="name"/>, moves to (<paramref name="toX"/>,
    /// <paramref name="toY"/>) in <paramref name="moveFrames"/> even steps, and releases there.
    /// </summary>
    public void Drag(string name, int toX, int toY, int moveFrames = 4)
    {
        (int fromX, int fromY) = CenterOf(name);
        MoveTo(fromX, fromY);
        SetButton(MouseButton.Primary, held: true);
        moveFrames = Math.Max(1, moveFrames);
        for (int i = 1; i <= moveFrames; i++)
        {
            MoveTo(fromX + (toX - fromX) * i / moveFrames, fromY + (toY - fromY) * i / moveFrames);
        }
        SetButton(MouseButton.Primary, held: false);
    }

    /// <summary>
    /// Moves over <paramref name="name"/> and turns the wheel by <paramref name="detents"/>
    /// (negative scrolls down): two frames.
    /// </summary>
    public void Scroll(string name, int detents)
    {
        MoveTo(name);
        Cursor.Scroll(detents);
        Step();
    }

    /// <summary>Delivers <paramref name="text"/> to the focused control on the next frame.</summary>
    public void Type(string text)
    {
        Keyboard.QueueText(text);
        Step();
    }

    /// <summary>Holds or releases <paramref name="key"/> and runs one frame.</summary>
    public void SetKey(GumKeys key, bool held)
    {
        Keyboard.SetKey(key, held);
        Step();
    }

    /// <summary>One keystroke: down for a frame, then up for a frame.</summary>
    public void PressKey(GumKeys key)
    {
        SetKey(key, held: true);
        SetKey(key, held: false);
    }

    /// <summary>
    /// Reads <paramref name="property"/> from the Forms control of <paramref name="name"/> when it has
    /// one with that property, otherwise from the visual.
    /// </summary>
    /// <exception cref="GumAutomationException">Neither has a readable public property of that name.</exception>
    public object? Read(string name, string property)
    {
        GraphicalUiElement element = Find(name);
        object? formsControl = (element as InteractiveGue)?.FormsControlAsObject;
        foreach (object? target in new[] { formsControl, element })
        {
            PropertyInfo? info = target?.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (info?.GetMethod != null && info.GetIndexParameters().Length == 0)
            {
                return info.GetValue(target);
            }
        }
        throw new GumAutomationException(
            $"'{name}' ({formsControl?.GetType().Name ?? element.GetType().Name}) has no readable property '{property}'.");
    }

    /// <summary>Writes the current frame to <paramref name="path"/> through <see cref="ScreenshotWriter"/>.</summary>
    /// <exception cref="GumAutomationException">The host set no screenshot writer.</exception>
    public void Screenshot(string path)
    {
        if (ScreenshotWriter == null)
        {
            throw new GumAutomationException("This host has no screenshot writer; set GumDriver.ScreenshotWriter.");
        }
        ScreenshotWriter(path);
    }
}
