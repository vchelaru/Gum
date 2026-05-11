using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Bubblegum shape stack:
/// translucent focus ring + Surface1 fill + 2 px pink border at 8 px corner
/// radius. Shared by <see cref="TextBoxVisual"/> and <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class BubblegumTextInputDecoration
{
    private const float CornerRadius = 8f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public BubblegumTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _focusRing = CreateFocusRing();
        host.AddChild(_focusRing);

        _fill = CreateFill();
        host.AddChild(_fill);

        // Re-attach ClipContainer between fill and border so text / placeholder /
        // caret / selection render above the fill, but the rounded border draws
        // ON TOP. Gum's clip container is rectangular — without rounded
        // clipping, content rendered to the edge would visibly poke past the
        // rounded outline at the corners. Painting the border last masks those
        // corner regions with the theme's pink stroke.
        host.AddChild(host.ClipContainer);

        _border = CreateBorder();
        host.AddChild(_border);

        WireStates(host);
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "BubblegumTextInputFill";
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
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.Color = BubblegumColors.Surface1;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "BubblegumTextInputBorder";
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
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = BubblegumColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "BubblegumTextInputFocusRing";
        ring.X = 0;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = CornerRadius + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = BubblegumPalette.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        host.States.Enabled.Apply = () => Apply(host,
            fill: BubblegumColors.Surface1, border: BubblegumColors.Border,
            text: BubblegumColors.Text, placeholder: BubblegumColors.Placeholder,
            caret: BubblegumColors.Accent, selection: BubblegumColors.AccentLight, ring: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, placeholder: BubblegumColors.Placeholder,
            caret: BubblegumColors.Accent, selection: BubblegumColors.AccentLight, ring: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, placeholder: BubblegumColors.Placeholder,
            caret: BubblegumColors.Accent, selection: BubblegumColors.AccentLight, ring: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, placeholder: BubblegumColors.Disabled,
            caret: BubblegumColors.Disabled, selection: BubblegumColors.AccentLight, ring: false);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text,
        Color placeholder, Color caret, Color selection, bool ring)
    {
        _fill.Color = fill;
        _border.Color = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = placeholder;
        host.CaretInstance.Color = caret;
        host.SelectionInstance.Color = selection;
        _focusRing.Visible = ring;
    }
}
