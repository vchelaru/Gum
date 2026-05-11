using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseToggleButtonVisual = Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ToggleButton visual. Off variants render raised exactly like a
/// <see cref="ButtonVisual"/>; On variants render sunken (the toggle is "pressed in")
/// — same visual idiom the Win95 task-bar uses for active app buttons.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private readonly Retro95Bevel _bevel;

    public ToggleButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
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

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Retro95Colors.Text;

        WireStates();
    }

    private void WireStates()
    {
        // Off variants — raised bevel.
        States.EnabledOff.Apply = () => Apply(BevelMode.Raised, Retro95Colors.Surface, Retro95Colors.Text);
        States.HighlightedOff.Apply = () => Apply(BevelMode.Raised, Retro95Colors.SurfaceHover, Retro95Colors.Text);
        States.PushedOff.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.Surface, Retro95Colors.Text);
        States.FocusedOff.Apply = () => Apply(BevelMode.Raised, Retro95Colors.Surface, Retro95Colors.Text);
        States.HighlightedFocusedOff.Apply = () => Apply(BevelMode.Raised, Retro95Colors.SurfaceHover, Retro95Colors.Text);
        States.DisabledOff.Apply = () => Apply(BevelMode.Raised, Retro95Colors.Surface, Retro95Colors.DisabledText);
        States.DisabledFocusedOff.Apply = () => Apply(BevelMode.Raised, Retro95Colors.Surface, Retro95Colors.DisabledText);

        // On variants — sunken bevel (the "pressed-in" toggle convention).
        States.EnabledOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.Surface, Retro95Colors.Text);
        States.HighlightedOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.SurfaceHover, Retro95Colors.Text);
        States.PushedOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.Surface, Retro95Colors.Text);
        States.FocusedOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.Surface, Retro95Colors.Text);
        States.HighlightedFocusedOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.SurfaceHover, Retro95Colors.Text);
        States.DisabledOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.Surface, Retro95Colors.DisabledText);
        States.DisabledFocusedOn.Apply = () => Apply(BevelMode.Sunken, Retro95Colors.Surface, Retro95Colors.DisabledText);
    }

    private void Apply(BevelMode mode, Color fill, Color text)
    {
        _bevel.SetMode(mode);
        _bevel.SetFill(fill);
        TextInstance.Color = text;
    }
}
