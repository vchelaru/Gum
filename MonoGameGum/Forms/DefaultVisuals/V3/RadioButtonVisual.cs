using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



#if RAYLIB
using Raylib_cs;
using Gum.GueDeriving;

#else
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
#endif

using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

public class RadioButtonVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public SpriteRuntime Radio { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class RadioButtonCategoryStates
    {
        public StateSave EnabledOn { get; set; } = new StateSave() { Name = nameof(EnabledOn) };
        public StateSave EnabledOff { get; set; } = new StateSave() { Name = nameof(EnabledOff) };
        public StateSave DisabledOn { get; set; } = new StateSave() { Name = nameof(DisabledOn) };
        public StateSave DisabledOff { get; set; } = new StateSave() { Name = nameof(DisabledOff) };
        public StateSave HighlightedOn { get; set; } = new StateSave() { Name = nameof(HighlightedOn) };
        public StateSave HighlightedOff { get; set; } = new StateSave() { Name = nameof(HighlightedOff) };
        public StateSave PushedOn { get; set; } = new StateSave() { Name = nameof(PushedOn) };
        public StateSave PushedOff { get; set; } = new StateSave() { Name = nameof(PushedOff) };
        public StateSave FocusedOn { get; set; } = new StateSave() { Name = nameof(FocusedOn) };
        public StateSave FocusedOff { get; set; } = new StateSave() { Name = nameof(FocusedOff) };
        public StateSave HighlightedFocusedOn { get; set; } = new StateSave() { Name = nameof(HighlightedFocusedOn) };
        public StateSave HighlightedFocusedOff { get; set; } = new StateSave() { Name = nameof(HighlightedFocusedOff) };
        public StateSave DisabledFocusedOn { get; set; } = new StateSave() { Name = nameof(DisabledFocusedOn) };
        public StateSave DisabledFocusedOff { get; set; } = new StateSave() { Name = nameof(DisabledFocusedOff) };

    }

    public RadioButtonCategoryStates States;

    public StateSaveCategory RadioButtonCategory { get; private set; }


    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (value != _backgroundColor)
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _backgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }
    Color _foregroundColor;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (value != _foregroundColor)
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _foregroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _radioColor;
    public Color RadioColor
    {
        get => _radioColor;
        set
        {
            if (value != _radioColor)
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _radioColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public RadioButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 128;
        Height = 24;

        States = new RadioButtonCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Left;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 24;
        Background.Height = 24;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.CircleBordered);
        this.AddChild(Background);

        // TOOL uses Elements/Icon, which contains an IconSprite that uses the same values (100 and PercentOfSourceFile)
        Radio = new SpriteRuntime();
        Radio.Name = "InnerCheck";
        Radio.Width = 100;
        Radio.Height = 100;
        Radio.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        Radio.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        Radio.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Radio.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Radio.XOrigin = HorizontalAlignment.Center;
        Radio.YOrigin = VerticalAlignment.Center;
        Radio.Texture = uiSpriteSheetTexture;
        Radio.ApplyState(Styling.ActiveStyle.Icons.Circle2);
        Background.Children.Add(Radio);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.X = 0;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        TextInstance.Y = 0;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Right;
        TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.Width = -28;
        TextInstance.Height = 0;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.Text = "Radio Label";
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
        FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
        this.AddChild(FocusedIndicator);

        RadioButtonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        RadioButtonCategory.Name = "RadioButtonCategory";
        this.AddCategory(RadioButtonCategory);

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.White;
        RadioColor = Styling.ActiveStyle.Colors.White;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new RadioButton(this);
        }
    }


    private void DefineDynamicStyleChanges()
    {
        // Enabled (On/Off)
        RadioButtonCategory.States.Add(States.EnabledOn);
        States.EnabledOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, RadioColor, false, true);
        };

        RadioButtonCategory.States.Add(States.EnabledOff);
        States.EnabledOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, RadioColor, false, false);
        };

        // Disabled (On/Off)
        RadioButtonCategory.States.Add(States.DisabledOn);
        States.DisabledOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), ForegroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), RadioColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), false, true);
        };

        RadioButtonCategory.States.Add(States.DisabledOff);
        States.DisabledOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), ForegroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), RadioColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), false, false);
        };

        // Disabled Focused (On/Off)
        RadioButtonCategory.States.Add(States.DisabledFocusedOn);
        States.DisabledFocusedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), ForegroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), RadioColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), true, true);
        };

        RadioButtonCategory.States.Add(States.DisabledFocusedOff);
        States.DisabledFocusedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), ForegroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), RadioColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleSuperDarken), true, false);
        };

        // Focused (On/Off)
        RadioButtonCategory.States.Add(States.FocusedOn);
        States.FocusedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, RadioColor, true, true);
        };

        RadioButtonCategory.States.Add(States.FocusedOff);
        States.FocusedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, RadioColor, true, false);
        };

        // Highlighted (On/Off)
        RadioButtonCategory.States.Add(States.HighlightedOn);
        States.HighlightedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, RadioColor, false, true);
        };

        RadioButtonCategory.States.Add(States.HighlightedOff);
        States.HighlightedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, RadioColor, false, false);
        };

        // Highlighted Focused (On/Off)
        RadioButtonCategory.States.Add(States.HighlightedFocusedOn);
        States.HighlightedFocusedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, RadioColor, true, true);
        };

        RadioButtonCategory.States.Add(States.HighlightedFocusedOff);
        States.HighlightedFocusedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor, RadioColor, true, false);
        };

        // Pushed (On/Off)
        RadioButtonCategory.States.Add(States.PushedOn);
        States.PushedOn.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), ForegroundColor, RadioColor, false, true);
        };

        RadioButtonCategory.States.Add(States.PushedOff);
        States.PushedOff.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), ForegroundColor, RadioColor, false, false);
        };
    }

    private void SetValuesForState(Color checkboxBackgroundColor, Color textColor, Color radioColor, bool isFocused, bool radioVisible)
    {
        Background.Color = checkboxBackgroundColor;
        TextInstance.Color = textColor;
        Radio.Color = radioColor;
        Radio.Visible = radioVisible;
        FocusedIndicator.Visible = isFocused;
    }

    public RadioButton FormsControl => FormsControlAsObject as RadioButton;
}
