using System;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;



#if RAYLIB
using Raylib_cs;
using Gum.GueDeriving;

#else
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for <see cref="ToggleButton"/>. Structurally identical to
/// <see cref="ButtonVisual"/> but exposes On/Off state variants so the toggle
/// can visually distinguish its checked and unchecked states.
/// </summary>
public class ToggleButtonVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// The centered text label displayed on the button.
    /// </summary>
    public TextRuntime TextInstance { get; private set; }

    /// <summary>
    /// A thin bar displayed at the bottom of the control when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ToggleButtonCategoryStates
    {
        public StateSave EnabledOn { get; set; } = new StateSave { Name = nameof(EnabledOn) };
        public StateSave EnabledOff { get; set; } = new StateSave { Name = nameof(EnabledOff) };
        public StateSave HighlightedOn { get; set; } = new StateSave { Name = nameof(HighlightedOn) };
        public StateSave HighlightedOff { get; set; } = new StateSave { Name = nameof(HighlightedOff) };
        public StateSave PushedOn { get; set; } = new StateSave { Name = nameof(PushedOn) };
        public StateSave PushedOff { get; set; } = new StateSave { Name = nameof(PushedOff) };
        public StateSave DisabledOn { get; set; } = new StateSave { Name = nameof(DisabledOn) };
        public StateSave DisabledOff { get; set; } = new StateSave { Name = nameof(DisabledOff) };
        public StateSave FocusedOn { get; set; } = new StateSave { Name = nameof(FocusedOn) };
        public StateSave FocusedOff { get; set; } = new StateSave { Name = nameof(FocusedOff) };
        public StateSave HighlightedFocusedOn { get; set; } = new StateSave { Name = nameof(HighlightedFocusedOn) };
        public StateSave HighlightedFocusedOff { get; set; } = new StateSave { Name = nameof(HighlightedFocusedOff) };
        public StateSave DisabledFocusedOn { get; set; } = new StateSave { Name = nameof(DisabledFocusedOn) };
        public StateSave DisabledFocusedOff { get; set; } = new StateSave { Name = nameof(DisabledFocusedOff) };
    }

    public ToggleButtonCategoryStates States;

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory ToggleCategory { get; private set; }

    Color _backgroundColor;
    /// <summary>
    /// The base color applied to the background. Setting this value immediately updates the
    /// visual. States may tint this color (for example, disabled states convert to grayscale
    /// and darken, and 'On' states darken to indicate the toggle is active).
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!value.Equals(_backgroundColor))
            {
                _backgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }
    Color _foregroundColor;
    /// <summary>
    /// The base color applied to the text. Setting this value immediately updates the visual.
    /// States may tint this color (for example, disabled states convert to grayscale and darken).
    /// </summary>
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (!value.Equals(_foregroundColor))
            {
                _foregroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _focusedIndicatorColor;
    /// <summary>
    /// The color of the focus indicator bar shown when the control has focus. Setting this
    /// value immediately updates the visual.
    /// </summary>
    public Color FocusedIndicatorColor
    {
        get => _focusedIndicatorColor;
        set
        {
            if(!value.Equals(_focusedIndicatorColor))
            {
                _focusedIndicatorColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public ToggleButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Width = 128;
        Height = 5;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        States = new ToggleButtonCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.Height = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Name = "Background";
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

        TextInstance = new TextRuntime();
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.Width = 0;
        TextInstance.Height = 5;
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Toggle";
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.XOrigin = HorizontalAlignment.Center;
        TextInstance.YOrigin = VerticalAlignment.Center;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.HorizontalAlignment = HorizontalAlignment.Center;
        TextInstance.VerticalAlignment = VerticalAlignment.Center;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        this.AddChild(TextInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0;
        FocusedIndicator.Y = 2;
        FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = VerticalAlignment.Top;
        FocusedIndicator.Width = 0;
        FocusedIndicator.Height = 2;
        FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        this.AddChild(FocusedIndicator);

        ToggleCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        ToggleCategory.Name = "ToggleCategory";
        this.AddCategory(ToggleCategory);

        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;
        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ToggleButton(this);
        }
    }

    private void DefineDynamicStyleChanges()
    {
        // "On" states use the darkened (pushed) background to visually indicate the toggle is active.
        // "Off" states use the normal background color, matching standard ButtonVisual behavior.
        // Func<Color> parameters ensure lambdas re-read the current property values when applied.

        // Enabled
        AddOnOffStates(States.EnabledOn, States.EnabledOff,
            () => BackgroundColor, () => ForegroundColor, isFocused: false);

        // Highlighted
        AddOnOffStates(States.HighlightedOn, States.HighlightedOff,
            () => BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), () => ForegroundColor, isFocused: false);

        // Pushed
        AddOnOffStates(States.PushedOn, States.PushedOff,
            () => BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), () => ForegroundColor, isFocused: false);

        // Disabled
        AddOnOffStates(States.DisabledOn, States.DisabledOff,
            () => BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken),
            () => ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), isFocused: false);

        // Focused
        AddOnOffStates(States.FocusedOn, States.FocusedOff,
            () => BackgroundColor, () => ForegroundColor, isFocused: true);

        // Highlighted Focused
        AddOnOffStates(States.HighlightedFocusedOn, States.HighlightedFocusedOff,
            () => BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), () => ForegroundColor, isFocused: true);

        // Disabled Focused
        AddOnOffStates(States.DisabledFocusedOn, States.DisabledFocusedOff,
            () => BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken),
            () => ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), isFocused: true);
    }

    private void AddOnOffStates(StateSave onState, StateSave offState,
        Func<Color> getBaseBackgroundColor, Func<Color> getForegroundColor, bool isFocused)
    {
        // "On" uses a darkened background to show the toggle is pressed/active
        ToggleCategory.States.Add(onState);
        onState.Apply = () =>
        {
            SetValuesForState(
                getBaseBackgroundColor().Adjust(Styling.ActiveStyle.Colors.PercentDarken),
                getForegroundColor(),
                isFocused);
        };

        // "Off" uses the background as-is
        ToggleCategory.States.Add(offState);
        offState.Apply = () =>
        {
            SetValuesForState(
                getBaseBackgroundColor(),
                getForegroundColor(),
                isFocused);
        };
    }

    private void SetValuesForState(Color backgroundColor, Color foregroundColor, bool isFocused)
    {
        Background.Color = backgroundColor;
        TextInstance.Color = foregroundColor;
        FocusedIndicator.Visible = isFocused;
        FocusedIndicator.Color = FocusedIndicatorColor;
    }

    /// <summary>
    /// Returns the strongly-typed ToggleButton Forms control backing this visual.
    /// </summary>
    public ToggleButton FormsControl => (ToggleButton)FormsControlAsObject;
}
