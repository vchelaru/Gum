using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95 slider thumb. A 12 × 22 raised-bevel gray rectangle (matches <c>.rc-sldr-thumb</c>).
/// Inverts to a sunken bevel when pressed.
/// Wrapped by RangeBase in a <see cref="Button"/>; the Retro95 Button template never gets to
/// apply because V3 ScrollBar / Slider instantiate the thumb directly — this visual stands in.
/// </summary>
public class SliderThumbVisual : InteractiveGue
{
    private const float ThumbWidth = 12f;
    private const float ThumbHeight = 22f;

    private readonly Retro95Bevel _bevel;
    private readonly Retro95DottedFocusRect _focusRect;
    private StateSaveCategory _buttonCategory = null!;

    public SliderThumbVisual() : base(new InvisibleRenderable())
    {
        HasEvents = true;
        Width = ThumbWidth;
        Height = ThumbHeight;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);
        // Win95 shows a dotted focus rect inside the slider thumb body.
        _focusRect = new Retro95DottedFocusRect(this, inset: 2f);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        // The thumb's button category is driven by the inner Button wrapper
        // RangeBase creates around our ThumbInstance — its Focused state fires
        // only when the BUTTON has keyboard focus. For a Slider, focus lives
        // on the Slider control itself, so the thumb button never sees the
        // Focused state. SliderVisual drives the focus rect explicitly via
        // ShowFocusRect / HideFocusRect; the button category here only handles
        // hover / press / disabled chrome.
        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface));
        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.SurfaceHover));
        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(mode: BevelMode.Sunken, fill: Retro95Styling.ActiveStyle.Colors.Surface));
        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface));
        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.SurfaceHover));
        // Win95 disabled-thumb look uses a single-tone bevel (no inner highlight
        // band) — StatusPanel mode collapses the inner ring to match the fill,
        // producing the flat "drained" appearance the OS used.
        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(mode: BevelMode.StatusPanel, fill: Retro95Styling.ActiveStyle.Colors.DisabledThumb));
        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(mode: BevelMode.StatusPanel, fill: Retro95Styling.ActiveStyle.Colors.DisabledThumb));
    }

    public void ShowFocusRect() => _focusRect.Show();
    public void HideFocusRect() => _focusRect.Hide();

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(BevelMode mode, Color fill)
    {
        _bevel.SetMode(mode);
        _bevel.SetFill(fill);
    }
}
