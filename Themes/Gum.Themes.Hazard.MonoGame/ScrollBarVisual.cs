using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled ScrollBar visual. De-emphasized navigation chrome:
/// transparent track, no up/down arrow buttons, and a muted gray thumb that
/// brightens on hover. Mirrors the conventions of VS Code / JetBrains / Slack /
/// browser overlay scrollbars.
/// <para>
/// Insets are baked into the bar itself, not pushed onto consumers — the
/// ThumbContainer is shrunk on the long axis (so the thumb at scroll extremes
/// never touches a parent's top/bottom border) and the thumb is inset on the
/// short axis within the container. Result: consumers (ListBox, ScrollViewer,
/// standalone) can place the bar's bounding box flush against any edge and
/// the visible thumb still has consistent breathing room. The bar's track is
/// transparent, so the flush bounding box is invisible.
/// </para>
/// </summary>
public class ScrollBarVisual : BaseScrollBarVisual
{
    /// <summary>
    /// Gap (in pixels, per side) between the thumb and the bar's edge. The same
    /// value is used on both axes so the thumb reads as visually symmetric at
    /// scroll extremes. Short-axis lives on the thumb itself; long-axis lives on
    /// the ThumbContainer so RangeBase still sizes and positions the thumb freely
    /// within the shrunken container.
    /// </summary>
    private const float ThumbInset = 2f;

    /// <summary>Corner radius and stroke for the optional surrounding frame (see <see cref="ShowFrame"/>).</summary>
    private const float FrameCornerRadius = 0f;
    private const float FrameBorderThickness = 1f;

    private readonly ScrollBarThumbVisual _thumb;
    private readonly RectangleRuntime _frameFill;
    private readonly RectangleRuntime _frameBorder;
    private bool _isHorizontal;

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        // The base ctor creates the V3 ThumbInstance (a plain V3 ButtonVisual)
        // and assigns the Forms ScrollBar to FormsControlAsObject when
        // tryCreateFormsObject is true. We defer that, then replace the
        // thumb visual in-place and create the Forms object ourselves.
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        // Detach the V3 NineSlice track. The Hazard look has no visible
        // track — the parent container's surface reads through.
        TrackInstance.Parent = null;

        // Detach the V3 up/down arrow buttons. The de-emphasized look skips
        // them entirely; page-up/down via track click and mouse wheel cover
        // the use case.
        if (UpButtonInstance != null) UpButtonInstance.Parent = null;
        if (DownButtonInstance != null) DownButtonInstance.Parent = null;

        // Detach the V3 ThumbInstance (a V3.ButtonVisual the base ctor created
        // with no opportunity for our Button template to apply) and add our
        // own thumb visual named "ThumbInstance" so RangeBase.ReactToVisualChanged
        // wraps it in a Button.
        ThumbInstance.Parent = null;

        _thumb = new ScrollBarThumbVisual();
        _thumb.Name = "ThumbInstance";
        ThumbContainer.AddChild(_thumb);

        // Optional frame chrome — created up-front and hidden, toggled by
        // ShowFrame. Inserted before ThumbContainer so it renders behind the
        // thumb. Used for free-floating scrollbars (not inside a ListBox /
        // ScrollViewer shell) to give the bar a visible container.
        ThumbContainer.Parent = null;
        _frameFill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, FrameCornerRadius, "HazardScrollBarFrameFill");
        _frameFill.Visible = false;
        AddChild(_frameFill);
        _frameBorder = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, FrameCornerRadius, FrameBorderThickness, "HazardScrollBarFrameBorder");
        _frameBorder.Visible = false;
        AddChild(_frameBorder);
        AddChild(ThumbContainer);

        // Replace the V3 orientation callbacks. The base ones reserve 48px of
        // the bar for the now-detached arrow buttons and re-layout the V3
        // ThumbInstance we just replaced; both are stale. Our callbacks shrink
        // ThumbContainer on the long axis so the thumb at scroll extremes
        // never touches the bar's edge, and inset the thumb on the short axis
        // so it reads as slim navigation chrome.
        // When ShowFrame is on, the visible edge moves inward by the 1 px
        // border stroke, so we grow each inset by FrameBorderThickness to keep
        // the visible thumb-to-edge gap constant with or without the frame.
        States.OrientationStates.Vertical.Apply = () =>
        {
            _isHorizontal = false;
            Height = 128f;
            HeightUnits = DimensionUnitType.Absolute;
            Width = 14f;
            WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Width = 0f;
            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;

            _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
            _thumb.Height = 0f;
            _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
            _thumb.X = 0f;
            _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.XOrigin = HorizontalAlignment.Center;
            _thumb.Y = 0f;
            _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.YOrigin = VerticalAlignment.Center;

            ApplyInsets();
        };

        States.OrientationStates.Horizontal.Apply = () =>
        {
            _isHorizontal = true;
            Height = 14f;
            HeightUnits = DimensionUnitType.Absolute;
            Width = 128f;
            WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Height = 0f;
            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;

            _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
            _thumb.Width = 0f;
            _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
            _thumb.X = 0f;
            _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.XOrigin = HorizontalAlignment.Center;
            _thumb.Y = 0f;
            _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.YOrigin = VerticalAlignment.Center;

            ApplyInsets();
        };

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ScrollBar(this);
        }
    }

    /// <summary>
    /// When true, paints a Surface1 fill + 1 px Border stroke around the
    /// scroll bar (CornerRadius=2), matching the rest of the Hazard shell
    /// style. Use for free-floating scroll bars that aren't already nested
    /// inside a Hazard container (ListBox / ScrollViewer already provide
    /// the surrounding chrome, so leave this off in those cases). Defaults
    /// to false.
    /// </summary>
    public bool ShowFrame
    {
        get => _frameFill.Visible;
        set
        {
            if (_frameFill.Visible == value) return;
            _frameFill.Visible = value;
            _frameBorder.Visible = value;

            // Frame border eats 1 px per side, so the inset needs to grow.
            // Touch only the inset-dependent dimensions — do NOT re-run the
            // full orientation callback, which would clobber consumer-set
            // Width/Height (e.g. ScrollBar.Width = 16 from a showcase).
            ApplyInsets();
        }
    }

    private void ApplyInsets()
    {
        float inset = ThumbInset + (_frameFill.Visible ? FrameBorderThickness : 0f);

        if (_isHorizontal)
        {
            ThumbContainer.Width = -inset * 2f;
            _thumb.Height = -inset * 2f;
        }
        else
        {
            ThumbContainer.Height = -inset * 2f;
            _thumb.Width = -inset * 2f;
        }
    }
}
