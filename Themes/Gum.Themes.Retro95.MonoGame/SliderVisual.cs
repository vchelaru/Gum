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
using BaseSliderVisual = Gum.Forms.DefaultVisuals.V3.SliderVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Slider visual. A thin 3 px white-with-2-toned-bevel track and a
/// raised-bevel rectangular thumb (12 × 22), matching <c>.rc-sldr</c>.
/// </summary>
public class SliderVisual : BaseSliderVisual
{
    private const float ThumbWidth = 12f;
    private const float TrackHeight = 3f;

    private readonly ContainerRuntime _trackContainer;
    private readonly Retro95Bevel _trackBevel;
    private readonly SliderThumbVisual _thumb;

    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        TrackInstance.Width = -ThumbWidth;
        FocusedIndicator.Parent = null;

        TrackBackground.Parent = null;

        _trackContainer = new ContainerRuntime();
        _trackContainer.Name = "Retro95SliderTrackContainer";
        // Don't intercept clicks — TrackInstance owns the drag interaction.
        _trackContainer.HasEvents = false;
        _trackContainer.X = 0;
        _trackContainer.Y = 0;
        _trackContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
        _trackContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        _trackContainer.XOrigin = HorizontalAlignment.Center;
        _trackContainer.YOrigin = VerticalAlignment.Center;
        _trackContainer.Width = 0f;
        _trackContainer.Height = TrackHeight;
        _trackContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        _trackContainer.HeightUnits = DimensionUnitType.Absolute;
        TrackInstance.AddChild(_trackContainer);

        _trackBevel = Retro95Bevel.AddTo(_trackContainer, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        ThumbInstance!.Parent = null;
        _thumb = new SliderThumbVisual();
        _thumb.Name = "ThumbInstance";
        _thumb.XUnits = GeneralUnitType.Percentage;
        _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
        _thumb.XOrigin = HorizontalAlignment.Center;
        _thumb.YOrigin = VerticalAlignment.Center;
        TrackInstance.AddChild(_thumb);

        WireStates();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.Slider(this);
        }
    }

    private void WireStates()
    {
        // The thumb's button category never sees the slider's keyboard focus
        // (focus lives on the Slider control, not the inner Button RangeBase
        // wraps around our ThumbInstance) — so we drive the dotted focus rect
        // here, from the slider's own state callbacks.
        States.Enabled.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.WhiteFill, focus: false);
        States.Highlighted.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.WhiteFill, focus: false);
        States.HighlightedFocused.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.WhiteFill, focus: true);
        States.Focused.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.WhiteFill, focus: true);
        States.Pushed.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.WhiteFill, focus: false);
        States.Disabled.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Surface, focus: false);
        States.DisabledFocused.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Surface, focus: true);
    }

    private void Apply(Color trackFill, bool focus)
    {
        _trackBevel.SetFill(trackFill);
        if (focus) _thumb.ShowFocusRect(); else _thumb.HideFocusRect();
    }
}
