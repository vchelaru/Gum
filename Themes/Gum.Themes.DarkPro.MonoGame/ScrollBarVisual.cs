using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled ScrollBar visual. De-emphasized navigation chrome:
/// transparent track, no up/down arrow buttons, and a muted gray thumb that
/// brightens on hover. Mirrors the conventions of VS Code / JetBrains / Slack /
/// browser overlay scrollbars.
/// </summary>
public class ScrollBarVisual : BaseScrollBarVisual
{
    private const float ThumbInset = 4f;

    private readonly ScrollBarThumbVisual _thumb;

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        // The base ctor creates the V3 ThumbInstance (a plain V3 ButtonVisual)
        // and assigns the Forms ScrollBar to FormsControlAsObject when
        // tryCreateFormsObject is true. We accept that, then replace the
        // thumb visual in-place and re-run RangeBase's "find ThumbInstance by
        // name and wrap it in a Button" pass.
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        // Detach the V3 NineSlice track. The Dark Pro look has no visible
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

        // Replace the V3 orientation callbacks. The base ones reserve 48px of
        // the bar for the now-detached arrow buttons and re-layout the V3
        // ThumbInstance we just replaced; both are stale. Our callbacks let
        // ThumbContainer fill the full bar and inset the thumb on the cross
        // axis so it reads as slim navigation chrome.
        States.OrientationStates.Vertical.Apply = () =>
        {
            Height = 128f;
            HeightUnits = DimensionUnitType.Absolute;
            Width = 14f;
            WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.Height = 0f;
            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Width = 0f;
            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;

            _thumb.Width = -ThumbInset * 2f;
            _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
            _thumb.Height = 0f;
            _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
            _thumb.X = 0f;
            _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.XOrigin = HorizontalAlignment.Center;
            _thumb.Y = 0f;
            _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.YOrigin = VerticalAlignment.Center;
        };

        States.OrientationStates.Horizontal.Apply = () =>
        {
            Height = 14f;
            HeightUnits = DimensionUnitType.Absolute;
            Width = 128f;
            WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.Height = 0f;
            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Width = 0f;
            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;

            _thumb.Height = -ThumbInset * 2f;
            _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
            _thumb.Width = 0f;
            _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
            _thumb.X = 0f;
            _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.XOrigin = HorizontalAlignment.Center;
            _thumb.Y = 0f;
            _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
            _thumb.YOrigin = VerticalAlignment.Center;
        };

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ScrollBar(this);
        }
    }
}
