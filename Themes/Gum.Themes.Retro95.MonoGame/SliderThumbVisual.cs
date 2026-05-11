using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
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
    private StateSaveCategory _buttonCategory = null!;

    public SliderThumbVisual() : base(new InvisibleRenderable())
    {
        HasEvents = true;
        Width = ThumbWidth;
        Height = ThumbHeight;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

        Add(_buttonCategory, FrameworkElement.EnabledStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Colors.Surface));
        Add(_buttonCategory, FrameworkElement.HighlightedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Colors.SurfaceHover));
        Add(_buttonCategory, FrameworkElement.PushedStateName,
            () => Apply(mode: BevelMode.Sunken, fill: Retro95Colors.Surface));
        Add(_buttonCategory, FrameworkElement.FocusedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Colors.Surface));
        Add(_buttonCategory, FrameworkElement.HighlightedFocusedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Colors.SurfaceHover));
        // Win95 disabled-thumb look uses a single-tone bevel (no inner highlight
        // band) — StatusPanel mode collapses the inner ring to match the fill,
        // producing the flat "drained" appearance the OS used.
        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(mode: BevelMode.StatusPanel, fill: Retro95Colors.DisabledThumb));
        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(mode: BevelMode.StatusPanel, fill: Retro95Colors.DisabledThumb));
    }

    private static void Add(StateSaveCategory category, string name, System.Action apply)
    {
        StateSave state = new StateSave { Name = name };
        state.Apply = apply;
        category.States.Add(state);
    }

    private void Apply(BevelMode mode, Microsoft.Xna.Framework.Color fill)
    {
        _bevel.SetMode(mode);
        _bevel.SetFill(fill);
    }
}
