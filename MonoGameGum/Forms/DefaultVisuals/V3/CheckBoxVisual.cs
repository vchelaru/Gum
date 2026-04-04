using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Math.Geometry;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a CheckBox control. Contains a bordered checkbox background, an inner check/dash icon, a text label, and a focus indicator bar.
/// </summary>
public class CheckBoxVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice for the checkbox indicator area.
    /// </summary>
    public NineSliceRuntime CheckBoxBackground { get; private set; }
    /// <summary>
    /// The sprite displayed inside the checkbox background showing the check or dash icon.
    /// </summary>
    public SpriteRuntime InnerCheck { get; private set; }
    /// <summary>
    /// The text label displayed next to the checkbox.
    /// </summary>
    public TextRuntime TextInstance { get; private set; }
    /// <summary>
    /// A thin bar displayed at the bottom of the control when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class CheckBoxCategoryStates
    {
        public StateSave EnabledOn { get; set; } = new StateSave { Name = nameof(EnabledOn) };
        public StateSave EnabledOff { get; set; } = new StateSave { Name = nameof(EnabledOff) };
        public StateSave EnabledIndeterminate { get; set; } = new StateSave { Name = nameof(EnabledIndeterminate) };
        public StateSave DisabledOn { get; set; } = new StateSave { Name = nameof(DisabledOn) };
        public StateSave DisabledOff { get; set; } = new StateSave { Name = nameof(DisabledOff) };
        public StateSave DisabledIndeterminate { get; set; } = new StateSave { Name = nameof(DisabledIndeterminate) };
        public StateSave HighlightedOn { get; set; } = new StateSave { Name = nameof(HighlightedOn) };
        public StateSave HighlightedOff { get; set; } = new StateSave { Name = nameof(HighlightedOff) };
        public StateSave HighlightedIndeterminate { get; set; } = new StateSave { Name = nameof(HighlightedIndeterminate) };
        public StateSave PushedOn { get; set; } = new StateSave { Name = nameof(PushedOn) };
        public StateSave PushedOff { get; set; } = new StateSave { Name = nameof(PushedOff) };
        public StateSave PushedIndeterminate { get; set; } = new StateSave { Name = nameof(PushedIndeterminate) };
        public StateSave FocusedOn { get; set; } = new StateSave { Name = nameof(FocusedOn) };
        public StateSave FocusedOff { get; set; } = new StateSave { Name = nameof(FocusedOff) };
        public StateSave FocusedIndeterminate { get; set; } = new StateSave { Name = nameof(FocusedIndeterminate) };
        public StateSave HighlightedFocusedOn { get; set; } = new StateSave { Name = nameof(HighlightedFocusedOn) };
        public StateSave HighlightedFocusedOff { get; set; } = new StateSave { Name = nameof(HighlightedFocusedOff) };
        public StateSave HighlightedFocusedIndeterminate { get; set; } = new StateSave { Name = nameof(HighlightedFocusedIndeterminate) };
        public StateSave DisabledFocusedOn { get; set; } = new StateSave { Name = nameof(DisabledFocusedOn) };
        public StateSave DisabledFocusedOff { get; set; } = new StateSave { Name = nameof(DisabledFocusedOff) };
        public StateSave DisabledFocusedIndeterminate { get; set; } = new StateSave { Name = nameof(DisabledFocusedIndeterminate) };
    }


    public CheckBoxCategoryStates States;

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory CheckboxCategory { get; private set; }

    Color _backgroundColor;
    /// <summary>
    /// The base color applied to the checkbox background. Setting this value immediately updates the visual.
    /// States may tint this color (for example, disabled states convert to grayscale and darken).
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
    /// The base color applied to the text label. Setting this value immediately updates the visual.
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

    Color _checkColor;
    /// <summary>
    /// The base color applied to the inner check/dash icon. Setting this value immediately updates the visual.
    /// States may tint this color (for example, disabled states convert to grayscale and darken).
    /// </summary>
    public Color CheckColor
    {
        get => _checkColor;
        set
        {
            if(!value.Equals(_checkColor))
            {
                _checkColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _focusedIndicatorColor;
    /// <summary>
    /// The color of the focus indicator bar shown when the control has focus.
    /// Setting this value immediately updates the visual.
    /// </summary>
    public Color FocusedIndicatorColor
    {
        get => _focusedIndicatorColor;
        set
        {
            if (!value.Equals(_focusedIndicatorColor))
            {
                _focusedIndicatorColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Width = 128;
        Height = 24;

        States = new CheckBoxCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        CheckBoxBackground = new NineSliceRuntime();
        CheckBoxBackground.Width = 24;
        CheckBoxBackground.Height = 24;
        CheckBoxBackground.Color = BackgroundColor;
        CheckBoxBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        CheckBoxBackground.YOrigin = VerticalAlignment.Center;
        CheckBoxBackground.Name = "CheckBoxBackground";
        CheckBoxBackground.Texture = uiSpriteSheetTexture;
        CheckBoxBackground.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(CheckBoxBackground);

        InnerCheck = new SpriteRuntime();
        InnerCheck.Width = 100f;
        InnerCheck.Height = 100f;
        InnerCheck.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        InnerCheck.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        InnerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        InnerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        InnerCheck.XOrigin = HorizontalAlignment.Center;
        InnerCheck.YOrigin = VerticalAlignment.Center;
        InnerCheck.Name = "InnerCheck";
        InnerCheck.Color = ForegroundColor;
        InnerCheck.Texture = uiSpriteSheetTexture;
        InnerCheck.ApplyState(Styling.ActiveStyle.Icons.Check);
        CheckBoxBackground.Children?.Add(InnerCheck);

        TextInstance = new TextRuntime();
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Right;
        TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.Width = -28;
        TextInstance.Height = 0;
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Label";
        TextInstance.Color = ForegroundColor;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
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

        CheckboxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        CheckboxCategory.Name = "CheckBoxCategory";
        this.AddCategory(CheckboxCategory);

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        CheckColor = Styling.ActiveStyle.Colors.IconDefault;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new CheckBox(this);
        }
    }

    private void DefineDynamicStyleChanges()
    {
        // Enabled (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.EnabledOn);
        States.EnabledOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, CheckColor, false, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.EnabledOff);
        States.EnabledOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, CheckColor, false, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.EnabledIndeterminate);
        States.EnabledIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, CheckColor, false, true, Styling.ActiveStyle.Icons.Dash);
        };

        // Disabled (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.DisabledOn);
        States.DisabledOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), CheckColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), false, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.DisabledOff);
        States.DisabledOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), CheckColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), false, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.DisabledIndeterminate);
        States.DisabledIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), CheckColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), false, true, Styling.ActiveStyle.Icons.Dash);
        };

        // Disabled Focused (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.DisabledFocusedOn);
        States.DisabledFocusedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), CheckColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), true, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.DisabledFocusedOff);
        States.DisabledFocusedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), CheckColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), true, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.DisabledFocusedIndeterminate);
        States.DisabledFocusedIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), CheckColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), true, true, Styling.ActiveStyle.Icons.Dash);
        };

        // Focused (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.FocusedOn);
        States.FocusedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, CheckColor, true, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.FocusedOff);
        States.FocusedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, CheckColor, true, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.FocusedIndeterminate);
        States.FocusedIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, CheckColor, true, true, Styling.ActiveStyle.Icons.Dash);
        };

        // Highlighted (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.HighlightedOn);
        States.HighlightedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, CheckColor, false, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.HighlightedOff);
        States.HighlightedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, CheckColor, false, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.HighlightedIndeterminate);
        States.HighlightedIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, CheckColor, false, true, Styling.ActiveStyle.Icons.Dash);
        };

        // Highlighted Focused (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.HighlightedFocusedOn);
        States.HighlightedFocusedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, CheckColor, true, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.HighlightedFocusedOff);
        States.HighlightedFocusedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, CheckColor, true, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.HighlightedFocusedIndeterminate);
        States.HighlightedFocusedIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, CheckColor, true, true, Styling.ActiveStyle.Icons.Dash);
        };

        // Pushed (On/Off/Indeterminate)
        CheckboxCategory.States.Add(States.PushedOn);
        States.PushedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), ForegroundColor, CheckColor, false, true, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.PushedOff);
        States.PushedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), ForegroundColor, CheckColor, false, false, Styling.ActiveStyle.Icons.Check);
        };

        CheckboxCategory.States.Add(States.PushedIndeterminate);
        States.PushedIndeterminate.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), ForegroundColor, CheckColor, false, true, Styling.ActiveStyle.Icons.Dash);
        };

    }

    private void SetValuesForState(Color checkboxBackgroundColor, Color textColor, Color checkColor, bool isFocused, bool checkVisible, StateSave iconSaveState)
    {
        CheckBoxBackground.Color = checkboxBackgroundColor;
        TextInstance.Color = textColor;
        InnerCheck.Color = checkColor;
        InnerCheck.Visible = checkVisible;
        FocusedIndicator.Visible = isFocused;
        InnerCheck.ApplyState(iconSaveState);
        FocusedIndicator.Color = _focusedIndicatorColor;
    }

    /// <summary>
    /// Returns the strongly-typed CheckBox Forms control backing this visual.
    /// </summary>
    public CheckBox FormsControl => (CheckBox)FormsControlAsObject;
}
