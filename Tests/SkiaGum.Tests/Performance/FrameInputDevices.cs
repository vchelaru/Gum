using Gum.Forms;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using GumKeys = Gum.Forms.Input.Keys;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// A cursor whose state the test sets before each frame, for allocation tests on the render-only Skia
/// service, which has no cursor of its own. <c>Gum.Automation</c>'s <c>ScriptedCursor</c> derives from
/// the MonoGame cursor, so it cannot run here. <see cref="Activity"/> turns the held state into push and
/// click edges the way Gum's cursor does; it allocates nothing, so every byte measured is Forms' own.
/// </summary>
internal sealed class FrameCursor : ICursor
{
    private bool _primaryHeldLastFrame;
    private int _xLastFrame;
    private int _yLastFrame;
    private int _wheelLastFrame;

    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>Whether the primary button is held during the next <see cref="Activity"/>.</summary>
    public bool PrimaryHeld { get; set; }

    /// <summary>The running wheel total; one detent is 120 units, negative scrolls down.</summary>
    public int Wheel { get; set; }

    /// <summary>Places the cursor at the center of <paramref name="element"/>.</summary>
    public void MoveToCenterOf(GraphicalUiElement element)
    {
        X = (int)(element.GetAbsoluteX() + element.AbsoluteWidth / 2);
        Y = (int)(element.GetAbsoluteY() + element.AbsoluteHeight / 2);
    }

    public Cursors? CustomCursor { get; set; }
    public InputDevice LastInputDevice => InputDevice.Mouse;
    public float XRespectingGumZoomAndBounds() => X;
    public float YRespectingGumZoomAndBounds() => Y;
    public double LastPrimaryPushTime { get; private set; }
    public double LastPrimaryClickTime { get; private set; }
    public int XChange { get; private set; }
    public int YChange { get; private set; }
    public int ScrollWheelChange { get; private set; }
    public float ZVelocity => ScrollWheelChange / 120f;
    public bool PrimaryPush { get; private set; }
    public bool PrimaryDown { get; private set; }
    public bool PrimaryClick { get; private set; }
    public bool PrimaryClickNoSlide => PrimaryClick;
    public bool PrimaryDoubleClick => false;
    public bool PrimaryDoublePush => false;
    public bool SecondaryPush => false;
    public bool SecondaryDown => false;
    public bool SecondaryClick => false;
    public bool SecondaryDoubleClick => false;
    public bool MiddlePush => false;
    public bool MiddleDown => false;
    public bool MiddleClick => false;
    public bool MiddleDoubleClick => false;
    public InteractiveGue? WindowPushed { get; set; }
    public InteractiveGue? VisualRightPushed { get; set; }
#pragma warning disable CS0618 // The interface still requires the obsolete member.
    public InteractiveGue? WindowOver { get; set; }
#pragma warning restore CS0618
    public InteractiveGue? VisualOver { get; set; }

    public void Activity(double currentGameTimeTotalSeconds)
    {
        XChange = X - _xLastFrame;
        YChange = Y - _yLastFrame;
        ScrollWheelChange = (Wheel - _wheelLastFrame) / 120;
        PrimaryDown = PrimaryHeld;
        PrimaryPush = PrimaryHeld && !_primaryHeldLastFrame;
        PrimaryClick = !PrimaryHeld && _primaryHeldLastFrame;
        if (PrimaryPush)
        {
            LastPrimaryPushTime = currentGameTimeTotalSeconds;
        }
        if (PrimaryClick)
        {
            LastPrimaryClickTime = currentGameTimeTotalSeconds;
        }

        _primaryHeldLastFrame = PrimaryHeld;
        _xLastFrame = X;
        _yLastFrame = Y;
        _wheelLastFrame = Wheel;
    }
}

/// <summary>
/// A keyboard built on Gum's <see cref="PolledKeyboard"/>, the base the Skia hosts' keyboards use, whose
/// held keys and typed text the test sets before each frame.
/// </summary>
internal sealed class FrameKeyboard : PolledKeyboard
{
    private static readonly GumKeys[] Keys = { GumKeys.Back, GumKeys.Left, GumKeys.Right, GumKeys.LeftShift, GumKeys.RightShift };
    private readonly HashSet<GumKeys> _held = new HashSet<GumKeys>();

    protected override IEnumerable<GumKeys> SupportedKeys => Keys;

    protected override bool IsDeviceKeyDown(GumKeys key) => _held.Contains(key);

    public void SetKey(GumKeys key, bool held)
    {
        if (held)
        {
            _held.Add(key);
        }
        else
        {
            _held.Remove(key);
        }
    }

    public void Type(string text) => AppendTypedText(text);
}
