using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95 scroll-bar thumb. A raised-bevel gray rectangle (matches <c>.rc-sb-thm</c>),
/// fills its container. Inverts to sunken when pressed.
/// </summary>
public class ScrollBarThumbVisual : InteractiveGue
{
    private readonly Retro95Bevel _bevel;
    private StateSaveCategory _buttonCategory = null!;

    public ScrollBarThumbVisual() : base(new InvisibleRenderable())
    {
        HasEvents = true;
        Width = 0f;
        Height = 0f;
        WidthUnits = DimensionUnitType.RelativeToParent;
        HeightUnits = DimensionUnitType.RelativeToParent;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);

        WireStates();
    }

    private void WireStates()
    {
        _buttonCategory = new StateSaveCategory();
        _buttonCategory.Name = Button.ButtonCategoryName;
        AddCategory(_buttonCategory);

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
        Add(_buttonCategory, FrameworkElement.DisabledStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface));
        Add(_buttonCategory, FrameworkElement.DisabledFocusedStateName,
            () => Apply(mode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface));
    }

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
