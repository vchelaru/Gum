using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade ScrollBar. Translucent leaf-small thumb on a track-only
/// bar (CSS <c>.fg-sb</c> ships no buttons; we follow Neon's convention and
/// drop the up/down buttons too). <see cref="ShowFrame"/> paints an optional
/// leaf-medium framed track for free-floating scroll bars.
/// </summary>
public class ScrollBarVisual : BaseScrollBarVisual
{
    private const float ThumbInset = 2f;

    private const float FrameBorderThickness = 1f;

    private readonly ScrollBarThumbVisual _thumb;
    private readonly RectangleRuntime _frameFill;
    private readonly RectangleRuntime _frameBorder;
    private bool _isHorizontal;

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        TrackInstance.Parent = null;

        if (UpButtonInstance != null) UpButtonInstance.Parent = null;
        if (DownButtonInstance != null) DownButtonInstance.Parent = null;

        ThumbInstance.Parent = null;

        _thumb = new ScrollBarThumbVisual();
        _thumb.Name = "ThumbInstance";
        ThumbContainer.AddChild(_thumb);

        // Optional frame chrome behind the thumb container.
        ThumbContainer.Parent = null;
        _frameFill = CreateFrameFill();
        _frameFill.Visible = false;
        AddChild(_frameFill);
        _frameBorder = CreateFrameBorder();
        _frameBorder.Visible = false;
        AddChild(_frameBorder);
        AddChild(ThumbContainer);

        States.OrientationStates.Vertical.Apply = () =>
        {
            _isHorizontal = false;
            Height = 128f;
            HeightUnits = DimensionUnitType.Absolute;
            Width = 16f;
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
            Height = 16f;
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
    /// When true, paints a deep-canopy fill + sun-pale border around the
    /// scroll bar. Use for free-floating scroll bars that aren't nested
    /// inside a Forest Glade container.
    /// </summary>
    public bool ShowFrame
    {
        get => _frameFill.Visible;
        set
        {
            if (_frameFill.Visible == value) return;
            _frameFill.Visible = value;
            _frameBorder.Visible = value;
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

    private static RectangleRuntime CreateFrameFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "ForestGladeScrollBarFrameFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyMedium(fill);
        fill.IsFilled = true;
        fill.FillColor = ForestGladeStyling.ActiveStyle.Colors.ScrollTrack;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateFrameBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "ForestGladeScrollBarFrameBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyMedium(border);
        border.IsFilled = false;
        border.StrokeWidth = FrameBorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.Border;
        return border;
    }
}
