#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using FlatRedBall.Glue.StateInterpolation;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.StateAnimation.Runtime;
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
/// width the ProgressBar sets as a percentage of the track. The Indeterminate state loops the
/// indicator across the track.
/// </summary>
public class ProgressBarVisual : InteractiveGue
{
    /// <summary>The width of the indicator while indeterminate, as a percentage of the track.</summary>
    public const float IndeterminateIndicatorWidth = 30;

    /// <summary>The seconds the indicator takes to cross the track while indeterminate.</summary>
    public const float IndeterminateSeconds = 1.5f;

    /// <summary>
    /// The area the indicator fills. It clips its children, since a NineSlice cannot, so the
    /// indeterminate indicator can slide in and out past its edges.
    /// </summary>
    public ContainerRuntime TrackInstance { get; private set; }

    /// <summary>The bar drawn behind the indicator, filling the track.</summary>
    public NineSliceRuntime TrackBackground { get; private set; }

    /// <summary>The bar showing progress, sized by the ProgressBar as a percentage of the track.</summary>
    public NineSliceRuntime IndicatorInstance { get; private set; }

    public class ProgressBarCategoryStates
    {
        public StateSave Determinate { get; set; } = new StateSave() { Name = ProgressBar.DeterminateStateName };
        public StateSave Indeterminate { get; set; } = new StateSave() { Name = ProgressBar.IndeterminateStateName };
    }

    /// <summary>This visual's category states, typed by name. Hides the untyped States dictionary on GraphicalUiElement.</summary>
    public new ProgressBarCategoryStates States;

    /// <summary>The state category the ProgressBar sets.</summary>
    public StateSaveCategory ProgressBarCategory { get; private set; }

    /// <summary>
    /// The category holding the keyframe states of <see cref="IndeterminateAnimation"/>. The ProgressBar
    /// never sets it.
    /// </summary>
    public StateSaveCategory IndeterminateAnimationCategory { get; private set; }

    /// <summary>The looping animation played while in the Indeterminate state.</summary>
    public AnimationRuntime IndeterminateAnimation { get; private set; }

    Color _trackColor;
    /// <summary>The color of the track. Setting this value immediately updates the visual.</summary>
    public Color TrackColor
    {
        get => _trackColor;
        set
        {
            if (!value.Equals(_trackColor))
            {
                _trackColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _indicatorColor;
    /// <summary>The color of the indicator. Setting this value immediately updates the visual.</summary>
    public Color IndicatorColor
    {
        get => _indicatorColor;
        set
        {
            if (!value.Equals(_indicatorColor))
            {
                _indicatorColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public ProgressBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 128;
        Height = 16;
        States = new ProgressBarCategoryStates();
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

        ProgressBarCategory = new StateSaveCategory();
        ProgressBarCategory.Name = ProgressBar.ProgressBarCategoryName;
        AddCategory(ProgressBarCategory);

        IndeterminateAnimationCategory = new StateSaveCategory();
        IndeterminateAnimationCategory.Name = "IndeterminateAnimationCategory";
        AddCategory(IndeterminateAnimationCategory);
        IndeterminateAnimation = CreateIndeterminateAnimation();

        TrackColor = Styling.ActiveStyle.Colors.InputBackground;
        IndicatorColor = Styling.ActiveStyle.Colors.Primary;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ProgressBar(this);
        }
    }

    // The indicator's X moves from just off the track's left edge to just past its right edge.
    private AnimationRuntime CreateIndeterminateAnimation()
    {
        StateSave start = new StateSave { Name = "Start" };
        start.Variables.Add(new VariableSave { Name = "IndicatorInstance.X", Type = "float", Value = -IndeterminateIndicatorWidth, SetsValue = true });
        StateSave end = new StateSave { Name = "End" };
        end.Variables.Add(new VariableSave { Name = "IndicatorInstance.X", Type = "float", Value = 100f, SetsValue = true });
        IndeterminateAnimationCategory.States.Add(start);
        IndeterminateAnimationCategory.States.Add(end);

        AnimationRuntime animation = new AnimationRuntime { Name = "IndeterminateAnimation", Loops = true };
        string category = IndeterminateAnimationCategory.Name;
        animation.Keyframes.Add(new KeyframeRuntime { Time = 0, StateName = category + "/Start", InterpolationType = InterpolationType.Linear });
        animation.Keyframes.Add(new KeyframeRuntime { Time = IndeterminateSeconds, StateName = category + "/End", InterpolationType = InterpolationType.Linear });
        animation.RefreshCumulativeStates(this);
        return animation;
    }

    private void DefineDynamicStyleChanges()
    {
        ProgressBarCategory.States.Add(States.Determinate);
        States.Determinate.Apply = () =>
        {
            SetColors();
            if (AnimationController.CurrentAnimation == IndeterminateAnimation)
            {
                AnimationController.Stop();
            }
            // The ProgressBar sets the width from Value after applying this state.
            IndicatorInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            IndicatorInstance.X = 0;
        };

        ProgressBarCategory.States.Add(States.Indeterminate);
        States.Indeterminate.Apply = () =>
        {
            SetColors();
            IndicatorInstance.XUnits = GeneralUnitType.Percentage;
            IndicatorInstance.Width = IndeterminateIndicatorWidth;
            IndicatorInstance.WidthUnits = DimensionUnitType.PercentageOfParent;
            // Re-applying the state (RefreshStyles, a color change) keeps the animation going.
            if (AnimationController.CurrentAnimation != IndeterminateAnimation || !AnimationController.IsPlaying)
            {
                IndicatorInstance.X = -IndeterminateIndicatorWidth;
                AnimationController.Play(IndeterminateAnimation);
            }
        };
    }

    private void SetColors()
    {
        TrackBackground.Color = _trackColor;
        IndicatorInstance.Color = _indicatorColor;
    }

    /// <summary>
    /// Returns the strongly-typed ProgressBar Forms control backing this visual.
    /// </summary>
    public ProgressBar FormsControl => (ProgressBar)FormsControlAsObject!;
}
