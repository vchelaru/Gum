using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled ScrollBar visual. 18 px wide pink thumb on a transparent
/// track, no up/down arrow buttons. <see cref="ShowFrame"/> paints an optional
/// 2 px pink-bordered frame for free-floating scrollbars.
/// </summary>
public class ScrollBarVisual : BaseScrollBarVisual
{
    private const float ThumbInset = 3f;

    private const float FrameCornerRadius = 1f;
    private const float FrameBorderThickness = 2f;

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

        // Optional frame chrome (ShowFrame). Inserted before ThumbContainer so
        // it renders behind the thumb.
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
            Width = 18f;
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
            Height = 18f;
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
    /// When true, paints a Surface1 fill + 2 px pink border around the scroll
    /// bar (CornerRadius=8). Use for free-floating scroll bars that aren't
    /// nested inside a Neon container. Mirrors Dark Pro's
    /// <c>ScrollBarVisual.ShowFrame</c> contract so consumer code is portable.
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
        fill.Name = "NeonScrollBarFrameFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = FrameCornerRadius;
        fill.IsFilled = true;
        fill.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateFrameBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "NeonScrollBarFrameBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.CornerRadius = FrameCornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = FrameBorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Border;
        return border;
    }
}
