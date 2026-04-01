using Gum.Converters;
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
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

public class SliderVisual : InteractiveGue
{
    public ContainerRuntime TrackInstance { get; private set; }
    public NineSliceRuntime TrackBackground { get; private set; }
    public ButtonVisual ThumbInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }
    public class SliderCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave DisabledFocused { get; set; } = new StateSave() { Name = FrameworkElement.DisabledFocusedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave HighlightedFocused { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedFocusedStateName };
        public StateSave Pushed { get; set; } = new StateSave() { Name = FrameworkElement.PushedStateName };
    }

    public SliderCategoryStates States;

    public StateSaveCategory SliderCategory { get; private set; }

    Color _trackBackgroundColor;
    public Color TrackBackgroundColor
    {
        get => _trackBackgroundColor;
        set
        {
            if (!value.Equals(_trackBackgroundColor))
            {
                _trackBackgroundColor = value;
                FormsControl?.UpdateState();

            }
        }
    }

    Color _focusedIndicatorColor;
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

    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {

        this.HasEvents = true;
        Width = 128;
        Height = 24;
        float sliderButtonWidth = 32f;
        States = new SliderCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        TrackInstance = new ContainerRuntime();
        TrackInstance.Name = "TrackInstance";
        TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TrackInstance.Width = -sliderButtonWidth;
        TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TrackInstance.Height = 0f;
        TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TrackInstance.HasEvents = true;
        this.AddChild(TrackInstance);

        TrackBackground = new NineSliceRuntime();
        TrackBackground.Name = "TrackBackground";
        TrackBackground.X = 0;
        TrackBackground.XUnits = GeneralUnitType.PixelsFromMiddle;
        TrackBackground.Y = 0;
        TrackBackground.YUnits = GeneralUnitType.PixelsFromMiddle;
        TrackBackground.XOrigin = HorizontalAlignment.Center;
        TrackBackground.YOrigin = VerticalAlignment.Center;
        TrackBackground.Width = 0f;
        TrackBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TrackBackground.Height = 8f;
        TrackBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        TrackBackground.Texture = uiSpriteSheetTexture;
        TrackBackground.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        TrackInstance.AddChild(TrackBackground);

        ThumbInstance = new ButtonVisual();
        ThumbInstance.Name = "ThumbInstance";
        ThumbInstance.TextInstance.Text = "";
        ThumbInstance.XUnits = GeneralUnitType.Percentage;
        ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        ThumbInstance.Width = sliderButtonWidth;
        ThumbInstance.Height = 0f;
        ThumbInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TrackInstance.AddChild(ThumbInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0;
        FocusedIndicator.Y = 2;
        FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = VerticalAlignment.Top;
        FocusedIndicator.Width = -sliderButtonWidth;
        FocusedIndicator.Height = 2;
        FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        this.AddChild(FocusedIndicator);

        SliderCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        SliderCategory.Name = "SliderCategory";
        this.AddCategory(SliderCategory);

        TrackBackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Controls.Slider(this);
        }
    }


    private void DefineDynamicStyleChanges()
    {
        // Enabled (On/Off)
        SliderCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(isFocused: false, isEnabled: true);
        };

        SliderCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(isFocused: false, isEnabled: false);
        };

        SliderCategory.States.Add(States.DisabledFocused);
        States.DisabledFocused.Apply = () =>
        {
            SetValuesForState(isFocused: true, isEnabled: false);
        };

        SliderCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(isFocused: true, isEnabled: true);
        };

        SliderCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(isFocused: false, isEnabled: true);
        };

        SliderCategory.States.Add(States.HighlightedFocused);
        States.HighlightedFocused.Apply = () =>
        {
            SetValuesForState(isFocused: true, isEnabled: true);
        };

        SliderCategory.States.Add(States.Pushed);
        States.Pushed.Apply = () =>
        {
            SetValuesForState(isFocused: false, isEnabled: true);
        };
    }

    private void SetValuesForState(bool isFocused, bool isEnabled)
    {
        TrackBackground.Color = _trackBackgroundColor;
        FocusedIndicator.Visible = isFocused;
        ThumbInstance.IsEnabled = isEnabled;
        FocusedIndicator.Color = _focusedIndicatorColor;
    }

    public Controls.Slider FormsControl => (Slider)FormsControlAsObject;
}
