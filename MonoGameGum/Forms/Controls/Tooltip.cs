using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// A popup control that displays a short informational message for a host <see cref="FrameworkElement"/>.
/// Mirrors WPF's ToolTip. In v1 <see cref="Content"/> must be a <see cref="string"/>;
/// non-string content is reserved for a future release and currently throws <see cref="NotSupportedException"/>.
/// </summary>
/// <remarks>
/// Visibility is driven by <see cref="ToolTipService"/>, which observes cursor hover on the host element.
/// While shown, the tooltip's <see cref="FrameworkElement.Visual"/> is parented to
/// <see cref="FrameworkElement.PopupRoot"/>, matching the <see cref="ListBox"/> popup pattern.
/// </remarks>
public class Tooltip : FrameworkElement
{
    private GraphicalUiElement? _textInstance;
    private object? _content;

    /// <summary>
    /// The content displayed in the tooltip. In v1 only <see cref="string"/> values are supported.
    /// WPF accepts arbitrary object content; that is reserved for a future release.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when a non-null, non-string value is assigned.</exception>
    public object? Content
    {
        get => _content;
        set
        {
            if (value != null && value is not string)
            {
                throw new NotSupportedException(
                    $"Tooltip.Content currently supports only string values. " +
                    $"Got {value.GetType().FullName}. Non-string content is reserved for a future release.");
            }
            _content = value;
            ApplyContentToText();
        }
    }

    /// <summary>
    /// Whether the tooltip is currently displayed (its <see cref="FrameworkElement.Visual"/> is in <see cref="FrameworkElement.PopupRoot"/>).
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// The <see cref="FrameworkElement"/> the tooltip is associated with. Used for hit-testing and positioning.
    /// </summary>
    public FrameworkElement? PlacementTarget { get; set; }

    /// <summary>
    /// Raised when the tooltip becomes visible. Mirrors WPF's <c>ToolTip.Opened</c>.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Raised when the tooltip is hidden. Mirrors WPF's <c>ToolTip.Closed</c>.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Creates a new <see cref="Tooltip"/> using the default registered visual template.
    /// </summary>
    public Tooltip() : base() { }

    /// <summary>
    /// Creates a new <see cref="Tooltip"/> wrapping the provided visual.
    /// </summary>
    public Tooltip(InteractiveGue visual) : base(visual) { }

    /// <inheritdoc/>
    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        ApplyContentToText();
        base.ReactToVisualChanged();
    }

    /// <inheritdoc/>
    protected override void RefreshInternalVisualReferences()
    {
        _textInstance = Visual?.GetGraphicalUiElementByName("TextInstance");
    }

    private void ApplyContentToText()
    {
        if (_textInstance == null)
        {
            return;
        }
        _textInstance.SetProperty("Text", _content as string ?? string.Empty);
    }

    /// <summary>
    /// Shows the tooltip by adding its <see cref="FrameworkElement.Visual"/> to <see cref="FrameworkElement.PopupRoot"/>
    /// and positioning it near the cursor, clamped to the screen.
    /// </summary>
    /// <param name="cursorX">Cursor X in gum units.</param>
    /// <param name="cursorY">Cursor Y in gum units.</param>
    public void Show(float cursorX, float cursorY)
    {
        if (Visual == null || IsOpen)
        {
            return;
        }

#if !FRB
        Visual.Z = float.PositiveInfinity;
        Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        Visual.XOrigin = HorizontalAlignment.Left;
        Visual.YOrigin = VerticalAlignment.Top;

        Visual.Parent = null;

        // Place below-right of cursor, matching WPF default Bottom placement.
        Visual.X = cursorX + 12;
        Visual.Y = cursorY + 20;

        if (FrameworkElement.PopupRoot != null)
        {
            FrameworkElement.PopupRoot.Children.Add(Visual);
        }

        ClampToScreen();
#endif

        IsOpen = true;
        Opened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows the tooltip at a default screen-center-ish position. Primarily for programmatic use;
    /// normal hover-driven display uses <see cref="Show(float, float)"/>.
    /// </summary>
    public void Show()
    {
        Show(cursorX: 0, cursorY: 0);
    }

    /// <summary>
    /// Hides the tooltip and removes its <see cref="FrameworkElement.Visual"/> from <see cref="FrameworkElement.PopupRoot"/>.
    /// </summary>
    public void Hide()
    {
        if (!IsOpen)
        {
            return;
        }

#if !FRB
        if (Visual != null && FrameworkElement.PopupRoot != null &&
            FrameworkElement.PopupRoot.Children.Contains(Visual))
        {
            FrameworkElement.PopupRoot.Children.Remove(Visual);
        }
#endif

        IsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void ClampToScreen()
    {
        if (Visual == null)
        {
            return;
        }

        var camera = Renderer.Self.Camera;
        var cameraRight = camera.ClientWidth / camera.Zoom;
        var cameraBottom = camera.ClientHeight / camera.Zoom;

        var width = Visual.GetAbsoluteWidth();
        var height = Visual.GetAbsoluteHeight();

        var right = Visual.X + width;
        if (right > cameraRight)
        {
            Visual.X -= (right - cameraRight);
        }
        if (Visual.X < 0)
        {
            Visual.X = 0;
        }

        var bottom = Visual.Y + height;
        if (bottom > cameraBottom)
        {
            Visual.Y -= (bottom - cameraBottom);
        }
        if (Visual.Y < 0)
        {
            Visual.Y = 0;
        }
    }
}
