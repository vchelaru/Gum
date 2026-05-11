using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Button visual. Battleship-gray fill, 2 px raised bevel (white/light
/// outer, dark gray inner — flips to sunken when pressed). Focus indicator is the
/// canonical Win95 1 px dotted black rectangle, inset 4 px from the body edge.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    /// <summary>Inset from the body edge to the dotted focus rectangle. Matches the CSS
    /// <c>outline-offset:-5px</c> (negative = inward).</summary>
    private const float FocusIndicatorInset = 4f;
    private const float DotSize = 1f;
    /// <summary>Pixels between dot centers. 2 = 1 px dot + 1 px gap, the literal Win95 pattern.</summary>
    private const float DotPitch = 2f;

    private readonly Retro95Bevel _bevel;
    private readonly ContainerRuntime _focusDotsContainer;
    private readonly List<ColoredRectangleRuntime> _focusDots = new List<ColoredRectangleRuntime>();
    private bool _focusVisible;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 96;
        Height = 24;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);

        // Dedicated container for the dotted focus rectangle. ContainerRuntime
        // defaults HasEvents=true and would swallow clicks — disable so the
        // dotted overlay is purely visual.
        _focusDotsContainer = new ContainerRuntime();
        _focusDotsContainer.Name = "Retro95FocusDots";
        _focusDotsContainer.HasEvents = false;
        _focusDotsContainer.X = 0; _focusDotsContainer.Y = 0;
        _focusDotsContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
        _focusDotsContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        _focusDotsContainer.XOrigin = HorizontalAlignment.Center;
        _focusDotsContainer.YOrigin = VerticalAlignment.Center;
        _focusDotsContainer.Width = 0; _focusDotsContainer.Height = 0;
        _focusDotsContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        _focusDotsContainer.HeightUnits = DimensionUnitType.RelativeToParent;
        _focusDotsContainer.Visible = false;
        AddChild(_focusDotsContainer);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Retro95Colors.Text;

        WireStates();
    }

    /// <summary>
    /// Rebuild the dotted-focus rectangle for the current Width / Height. Called
    /// from any state callback that turns the focus indicator on, so the dots
    /// reflect the visual's current dimensions (which the consumer typically
    /// sets after construction).
    /// </summary>
    private void RegenerateFocusDots()
    {
        foreach (ColoredRectangleRuntime dot in _focusDots)
        {
            dot.Parent = null;
        }
        _focusDots.Clear();

        float innerWidth = Width - (FocusIndicatorInset * 2f);
        float innerHeight = Height - (FocusIndicatorInset * 2f);
        if (innerWidth <= 0 || innerHeight <= 0) return;

        // Horizontal edges (top + bottom).
        int horizontalDotCount = (int)(innerWidth / DotPitch);
        for (int i = 0; i < horizontalDotCount; i++)
        {
            float x = -innerWidth / 2f + i * DotPitch;
            AddDot(x, 0, top: true, vertical: false);
            AddDot(x, 0, top: false, vertical: false);
        }

        // Vertical edges (left + right). Start one DotPitch in to avoid stacking
        // corner dots with the horizontal edges.
        int verticalDotCount = (int)((innerHeight - DotPitch * 2f) / DotPitch);
        for (int i = 0; i < verticalDotCount; i++)
        {
            float y = -innerHeight / 2f + DotPitch + i * DotPitch;
            AddDot(0, y, top: true, vertical: true);
            AddDot(0, y, top: false, vertical: true);
        }
    }

    private void AddDot(float xOffset, float yOffset, bool top, bool vertical)
    {
        ColoredRectangleRuntime dot = new ColoredRectangleRuntime();
        dot.Name = "Retro95FocusDot";
        dot.Color = Retro95Colors.Text;
        dot.Width = DotSize;
        dot.Height = DotSize;
        dot.WidthUnits = DimensionUnitType.Absolute;
        dot.HeightUnits = DimensionUnitType.Absolute;

        if (!vertical)
        {
            // top / bottom edge dot — fixed Y, varying X
            dot.X = xOffset;
            dot.XUnits = GeneralUnitType.PixelsFromMiddle;
            dot.XOrigin = HorizontalAlignment.Left;
            dot.Y = top ? FocusIndicatorInset : -FocusIndicatorInset;
            dot.YUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            dot.YOrigin = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        }
        else
        {
            // left / right edge dot — fixed X, varying Y
            dot.X = top ? FocusIndicatorInset : -FocusIndicatorInset;
            dot.XUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            dot.XOrigin = top ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            dot.Y = yOffset;
            dot.YUnits = GeneralUnitType.PixelsFromMiddle;
            dot.YOrigin = VerticalAlignment.Top;
        }

        _focusDotsContainer.AddChild(dot);
        _focusDots.Add(dot);
    }

    private void SetFocusIndicatorVisible(bool visible)
    {
        if (visible && !_focusVisible)
        {
            RegenerateFocusDots();
        }
        _focusVisible = visible;
        _focusDotsContainer.Visible = visible;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.Text, focus: false);

        States.Highlighted.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.SurfaceHover,
            text: Retro95Colors.Text, focus: false);

        States.Focused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.Text, focus: true);

        States.HighlightedFocused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.SurfaceHover,
            text: Retro95Colors.Text, focus: true);

        States.Pushed.Apply = () => Apply(
            bevelMode: BevelMode.Sunken, fill: Retro95Colors.Surface,
            text: Retro95Colors.Text, focus: false);

        States.Disabled.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.DisabledText, focus: false);

        States.DisabledFocused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.DisabledText, focus: true);
    }

    private void Apply(BevelMode bevelMode, Color fill, Color text, bool focus)
    {
        _bevel.SetMode(bevelMode);
        _bevel.SetFill(fill);
        TextInstance.Color = text;
        // Always regenerate when toggling focus on so dots reflect current
        // Width / Height (consumer typically sets size after construction).
        if (focus) RegenerateFocusDots();
        SetFocusIndicatorVisible(focus);
    }
}
