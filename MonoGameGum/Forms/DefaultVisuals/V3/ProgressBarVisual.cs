#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
using RenderingLibrary.Graphics;
#if SKIA
using Color = SkiaSharp.SKColor;
#elif XNALIKE
using Color = Microsoft.Xna.Framework.Color;
#endif

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a ProgressBar control. Contains a track with a background bar and an indicator whose
/// width the ProgressBar sets as a percentage of the track.
/// </summary>
public class ProgressBarVisual : InteractiveGue
{
    /// <summary>
    /// The area the indicator fills. It clips its children, since a NineSlice cannot.
    /// </summary>
    public ContainerRuntime TrackInstance { get; private set; }

    /// <summary>The bar drawn behind the indicator, filling the track.</summary>
    public NineSliceRuntime TrackBackground { get; private set; }

    /// <summary>
    /// The bar showing progress, sized by the ProgressBar as a percentage of the track. A theme may replace the
    /// child named "IndicatorInstance" with another shape, in which case this NineSlice is not drawn.
    /// </summary>
    public NineSliceRuntime IndicatorInstance { get; private set; }

    Color _trackColor;
    /// <summary>The color of the track. Setting this value immediately updates the visual.</summary>
    public virtual Color TrackColor
    {
        get => _trackColor;
        set
        {
            _trackColor = value;
            TrackBackground.Color = value;
        }
    }

    Color _indicatorColor;
    /// <summary>The color of the indicator. Setting this value immediately updates the visual.</summary>
    public virtual Color IndicatorColor
    {
        get => _indicatorColor;
        set
        {
            _indicatorColor = value;
            IndicatorInstance.Color = value;
        }
    }

    public ProgressBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 128;
        Height = 16;
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        TrackInstance = new ContainerRuntime();
        TrackInstance.Name = "TrackInstance";
        TrackInstance.Width = 0;
        TrackInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        TrackInstance.Height = 0;
        TrackInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        TrackInstance.ClipsChildren = true;
        AddChild(TrackInstance);

        TrackBackground = new NineSliceRuntime();
        TrackBackground.Name = "TrackBackground";
        TrackBackground.Width = 0;
        TrackBackground.WidthUnits = DimensionUnitType.RelativeToParent;
        TrackBackground.Height = 0;
        TrackBackground.HeightUnits = DimensionUnitType.RelativeToParent;
        TrackBackground.Texture = uiSpriteSheetTexture;
        TrackBackground.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        TrackInstance.AddChild(TrackBackground);

        IndicatorInstance = new NineSliceRuntime();
        IndicatorInstance.Name = "IndicatorInstance";
        IndicatorInstance.Dock(Gum.Wireframe.Dock.Left);
        IndicatorInstance.Width = 0;
        IndicatorInstance.WidthUnits = DimensionUnitType.PercentageOfParent;
        IndicatorInstance.Texture = uiSpriteSheetTexture;
        IndicatorInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        TrackInstance.AddChild(IndicatorInstance);

        // Assigned without the virtual setters, which a subclass overrides before its own shapes exist.
        _trackColor = Styling.ActiveStyle.Colors.InputBackground;
        TrackBackground.Color = _trackColor;
        _indicatorColor = Styling.ActiveStyle.Colors.Primary;
        IndicatorInstance.Color = _indicatorColor;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ProgressBar(this);
        }
    }

    /// <summary>
    /// Returns the strongly-typed ProgressBar Forms control backing this visual.
    /// </summary>
    public ProgressBar FormsControl => (ProgressBar)FormsControlAsObject!;
}
