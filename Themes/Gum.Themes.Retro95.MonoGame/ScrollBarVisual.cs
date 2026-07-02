using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ScrollBar visual. 16 px wide gray track with up/down (or
/// left/right) arrow buttons at each end, beveled raised, and a raised-bevel
/// thumb in the middle. Matches <c>.rc-sb</c> from the CSS.
/// <para>
/// V3's <c>UpButtonInstance</c> and <c>DownButtonInstance</c> are kept (not
/// detached) so the Forms-layer click handlers continue to work. Their default
/// <c>UpButtonIcon</c> / <c>DownButtonIcon</c> sprites (which point at the V3
/// sprite-sheet arrow glyph) are hidden, and a <see cref="TextRuntime"/> glyph
/// in the bundled icon font replaces them.
/// </para>
/// </summary>
public class ScrollBarVisual : BaseScrollBarVisual
{
    private const float ButtonSize = 16f;
    private const float ThumbInset = 1f;
    private const int ArrowFontSize = 8;

    private readonly ScrollBarThumbVisual _thumb;
    private readonly TextRuntime _upGlyph;
    private readonly TextRuntime _downGlyph;
    private bool _isHorizontal;

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        // Replace V3's NineSlice track background with a flat gray fill — the
        // Win95 trough was actually a checker pattern (repeating-conic-gradient
        // in the CSS) but the runtime has no equivalent primitive, so a solid
        // fill reads close enough at typical scroll-bar widths.
        TrackInstance.Parent = null;

        // V3 added UpButtonInstance / DownButtonInstance / ThumbContainer as
        // children in the base ctor; AddChild appends to the end so anything
        // we add now would paint ON TOP of them. Detach the chrome children,
        // add the track fill first, then re-attach so the final paint order is
        // [trackFill (back), buttons, thumb (front)].
        GraphicalUiElement? upBtn = UpButtonInstance;
        GraphicalUiElement? downBtn = DownButtonInstance;
        ContainerRuntime existingThumbContainer = ThumbContainer;
        if (upBtn != null) upBtn.Parent = null;
        if (downBtn != null) downBtn.Parent = null;
        existingThumbContainer.Parent = null;

        RectangleRuntime trackFill = NewStretched("Retro95ScrollBarTrackFill", Retro95Styling.ActiveStyle.Colors.Surface);
        AddChild(trackFill);

        if (upBtn != null) AddChild(upBtn);
        if (downBtn != null) AddChild(downBtn);
        AddChild(existingThumbContainer);

        // V3 attaches the up/down arrow icons via SpriteRuntimes that reference
        // the V3 sprite-sheet texture — we don't ship that texture and we want
        // the arrows in our icon font anyway. Hide the V3 sprite icons and add
        // TextRuntime glyphs to each button.
        if (UpButtonIcon != null) UpButtonIcon.Visible = false;
        if (DownButtonIcon != null) DownButtonIcon.Visible = false;

        _upGlyph = NewArrowGlyph("Retro95ScrollUpGlyph", "▲");
        if (UpButtonInstance != null) UpButtonInstance.AddChild(_upGlyph);

        _downGlyph = NewArrowGlyph("Retro95ScrollDownGlyph", "▼");
        if (DownButtonInstance != null) DownButtonInstance.AddChild(_downGlyph);

        // Replace the V3 thumb (a Button) with our beveled thumb so press / hover
        // states render in raised / sunken bevel chrome.
        ThumbInstance.Parent = null;
        _thumb = new ScrollBarThumbVisual();
        _thumb.Name = "ThumbInstance";
        ThumbContainer.AddChild(_thumb);

        States.OrientationStates.Vertical.Apply = () => ApplyVertical();
        States.OrientationStates.Horizontal.Apply = () => ApplyHorizontal();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ScrollBar(this);
        }
    }

    private void ApplyVertical()
    {
        _isHorizontal = false;
        Height = 128f; HeightUnits = DimensionUnitType.Absolute;
        Width = ButtonSize; WidthUnits = DimensionUnitType.Absolute;

        if (UpButtonInstance != null)
        {
            UpButtonInstance.X = 0f;
            UpButtonInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            UpButtonInstance.YUnits = GeneralUnitType.PixelsFromSmall;
            UpButtonInstance.XOrigin = HorizontalAlignment.Center;
            UpButtonInstance.YOrigin = VerticalAlignment.Top;
            UpButtonInstance.Width = ButtonSize; UpButtonInstance.WidthUnits = DimensionUnitType.Absolute;
            UpButtonInstance.Height = ButtonSize; UpButtonInstance.HeightUnits = DimensionUnitType.Absolute;
        }
        if (DownButtonInstance != null)
        {
            DownButtonInstance.X = 0f;
            DownButtonInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;
            DownButtonInstance.XOrigin = HorizontalAlignment.Center;
            DownButtonInstance.YOrigin = VerticalAlignment.Bottom;
            DownButtonInstance.Width = ButtonSize; DownButtonInstance.WidthUnits = DimensionUnitType.Absolute;
            DownButtonInstance.Height = ButtonSize; DownButtonInstance.HeightUnits = DimensionUnitType.Absolute;
        }
        _upGlyph.Text = "▲";
        _downGlyph.Text = "▼";

        // Leave room for the two ButtonSize arrows at top and bottom.
        ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
        ThumbContainer.Height = -(ButtonSize * 2f);
        ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        ThumbContainer.Width = 0f;

        _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
        _thumb.Height = 0f; _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
        _thumb.X = 0f; _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
        _thumb.XOrigin = HorizontalAlignment.Center;
        _thumb.Y = 0f; _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
        _thumb.YOrigin = VerticalAlignment.Center;
        ApplyInsets();
    }

    private void ApplyHorizontal()
    {
        _isHorizontal = true;
        Height = ButtonSize; HeightUnits = DimensionUnitType.Absolute;
        Width = 128f; WidthUnits = DimensionUnitType.Absolute;

        if (UpButtonInstance != null)
        {
            // "Up" button becomes "left" in horizontal orientation.
            UpButtonInstance.Y = 0f;
            UpButtonInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            UpButtonInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            UpButtonInstance.XOrigin = HorizontalAlignment.Left;
            UpButtonInstance.YOrigin = VerticalAlignment.Center;
            UpButtonInstance.Width = ButtonSize; UpButtonInstance.WidthUnits = DimensionUnitType.Absolute;
            UpButtonInstance.Height = ButtonSize; UpButtonInstance.HeightUnits = DimensionUnitType.Absolute;
        }
        if (DownButtonInstance != null)
        {
            DownButtonInstance.Y = 0f;
            DownButtonInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            DownButtonInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            DownButtonInstance.XOrigin = HorizontalAlignment.Right;
            DownButtonInstance.YOrigin = VerticalAlignment.Center;
            DownButtonInstance.Width = ButtonSize; DownButtonInstance.WidthUnits = DimensionUnitType.Absolute;
            DownButtonInstance.Height = ButtonSize; DownButtonInstance.HeightUnits = DimensionUnitType.Absolute;
        }
        _upGlyph.Text = "◀";
        _downGlyph.Text = "▶";

        ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        ThumbContainer.Width = -(ButtonSize * 2f);
        ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
        ThumbContainer.Height = 0f;

        _thumb.HeightUnits = DimensionUnitType.RelativeToParent;
        _thumb.Width = 0f; _thumb.WidthUnits = DimensionUnitType.RelativeToParent;
        _thumb.X = 0f; _thumb.XUnits = GeneralUnitType.PixelsFromMiddle;
        _thumb.XOrigin = HorizontalAlignment.Center;
        _thumb.Y = 0f; _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
        _thumb.YOrigin = VerticalAlignment.Center;
        ApplyInsets();
    }

    private void ApplyInsets()
    {
        if (_isHorizontal)
        {
            _thumb.Height = -ThumbInset * 2f;
        }
        else
        {
            _thumb.Width = -ThumbInset * 2f;
        }
    }

    private static TextRuntime NewArrowGlyph(string name, string glyph)
    {
        TextRuntime t = new TextRuntime();
        t.Name = name;
        t.X = 0; t.Y = 0;
        t.XUnits = GeneralUnitType.PixelsFromMiddle;
        t.YUnits = GeneralUnitType.PixelsFromMiddle;
        t.XOrigin = HorizontalAlignment.Center;
        t.YOrigin = VerticalAlignment.Center;
        t.Width = ButtonSize; t.Height = ButtonSize;
        t.WidthUnits = DimensionUnitType.Absolute;
        t.HeightUnits = DimensionUnitType.Absolute;
        t.HorizontalAlignment = HorizontalAlignment.Center;
        t.VerticalAlignment = VerticalAlignment.Center;
        t.Font = Retro95Styling.ActiveStyle.Text.IconFontFamily;
        t.FontSize = ArrowFontSize;
        t.Text = glyph;
        t.Color = Retro95Styling.ActiveStyle.Colors.Text;
        return t;
    }

    private static RectangleRuntime NewStretched(string name, Color color)
    {
        RectangleRuntime r = new RectangleRuntime();
        r.Name = name;
        r.X = 0; r.Y = 0;
        r.XUnits = GeneralUnitType.PixelsFromMiddle;
        r.YUnits = GeneralUnitType.PixelsFromMiddle;
        r.XOrigin = HorizontalAlignment.Center;
        r.YOrigin = VerticalAlignment.Center;
        r.Width = 0; r.Height = 0;
        r.WidthUnits = DimensionUnitType.RelativeToParent;
        r.HeightUnits = DimensionUnitType.RelativeToParent;
        r.IsFilled = true;
        r.FillColor = color;
        r.StrokeWidth = 0;
        return r;
    }
}
