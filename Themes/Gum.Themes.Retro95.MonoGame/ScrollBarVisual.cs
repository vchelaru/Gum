using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ScrollBar visual. 16 px wide raised-bevel gray track with a
/// raised-bevel thumb. Matches <c>.rc-sb</c> from the CSS. Arrow buttons are
/// hidden — the V3 UpButton/DownButton instances aren't styled here, since the
/// Win95 chrome typically uses sprite arrows that we'd need to bake.
/// </summary>
public class ScrollBarVisual : BaseScrollBarVisual
{
    private const float ThumbInset = 1f;

    private readonly ScrollBarThumbVisual _thumb;
    private bool _isHorizontal;

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        TrackInstance.Parent = null;
        if (UpButtonInstance != null) UpButtonInstance.Parent = null;
        if (DownButtonInstance != null) DownButtonInstance.Parent = null;
        ThumbInstance.Parent = null;

        // Replace the bar background with a flat surface fill. No bevel —
        // Win95 scroll-bar trough is a checker pattern; we render a solid
        // gray since the runtime has no repeating-conic-gradient primitive.
        ColoredRectangleRuntime trackFill = new ColoredRectangleRuntime();
        trackFill.Name = "Retro95ScrollBarTrackFill";
        trackFill.X = 0; trackFill.Y = 0;
        trackFill.XUnits = GeneralUnitType.PixelsFromMiddle;
        trackFill.YUnits = GeneralUnitType.PixelsFromMiddle;
        trackFill.XOrigin = HorizontalAlignment.Center;
        trackFill.YOrigin = VerticalAlignment.Center;
        trackFill.Width = 0; trackFill.Height = 0;
        trackFill.WidthUnits = DimensionUnitType.RelativeToParent;
        trackFill.HeightUnits = DimensionUnitType.RelativeToParent;
        trackFill.Color = Retro95Colors.Surface;
        AddChild(trackFill);

        _thumb = new ScrollBarThumbVisual();
        _thumb.Name = "ThumbInstance";
        ThumbContainer.AddChild(_thumb);
        AddChild(ThumbContainer);

        States.OrientationStates.Vertical.Apply = () =>
        {
            _isHorizontal = false;
            Height = 128f; HeightUnits = DimensionUnitType.Absolute;
            Width = 16f; WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Width = 0f;
            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;

            _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
            _thumb.Height = 0f;
            _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
            _thumb.X = 0f; _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.XOrigin = HorizontalAlignment.Center;
            _thumb.Y = 0f; _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.YOrigin = VerticalAlignment.Center;

            ApplyInsets();
        };

        States.OrientationStates.Horizontal.Apply = () =>
        {
            _isHorizontal = true;
            Height = 16f; HeightUnits = DimensionUnitType.Absolute;
            Width = 128f; WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Height = 0f;
            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;

            _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
            _thumb.Width = 0f;
            _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
            _thumb.X = 0f; _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.XOrigin = HorizontalAlignment.Center;
            _thumb.Y = 0f; _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.YOrigin = VerticalAlignment.Center;

            ApplyInsets();
        };

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ScrollBar(this);
        }
    }

    private void ApplyInsets()
    {
        if (_isHorizontal)
        {
            ThumbContainer.Width = -ThumbInset * 2f;
            _thumb.Height = -ThumbInset * 2f;
        }
        else
        {
            ThumbContainer.Height = -ThumbInset * 2f;
            _thumb.Width = -ThumbInset * 2f;
        }
    }
}
