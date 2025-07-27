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
using Gum.Forms.Controls;
using Gum.GueDeriving;

namespace Gum.Forms.DefaultVisuals;


#else
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
namespace MonoGameGum.Forms.DefaultVisuals;
#endif

public class SliderVisual : InteractiveGue
{
    public ContainerRuntime TrackInstance { get; private set; }
    public NineSliceRuntime NineSliceInstance { get; private set; }
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

    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {

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
        this.AddChild(TrackInstance);

        NineSliceInstance = new NineSliceRuntime();
        NineSliceInstance.Name = "NineSliceInstance";
        NineSliceInstance.Y = 0;
        NineSliceInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        NineSliceInstance.YOrigin = VerticalAlignment.Center;
        NineSliceInstance.Width = 0f;
        NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        NineSliceInstance.Height = 8f;
        NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        NineSliceInstance.Color = Styling.ActiveStyle.Colors.DarkGray;
        NineSliceInstance.Texture = uiSpriteSheetTexture;
        NineSliceInstance.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        TrackInstance.AddChild(NineSliceInstance);

        ThumbInstance = new ButtonVisual();
        ThumbInstance.Name = "ThumbInstance";
        ThumbInstance.TextInstance.Text = "";
        ThumbInstance.XUnits = GeneralUnitType.Percentage;
        ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        ThumbInstance.Width = sliderButtonWidth;
        ThumbInstance.Height = 24f;
        ThumbInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        TrackInstance.AddChild(ThumbInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
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

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        SliderCategory.States.Add(States.Disabled);
        AddVariable(States.Disabled, "FocusedIndicator.Visible", false);
        AddVariable(States.Disabled, "ThumbInstance.IsEnabled", false);

        SliderCategory.States.Add(States.DisabledFocused);
        AddVariable(States.DisabledFocused, "FocusedIndicator.Visible", true);
        AddVariable(States.DisabledFocused, "ThumbInstance.IsEnabled", false);

        SliderCategory.States.Add(States.Enabled);
        AddVariable(States.Enabled, "FocusedIndicator.Visible", false);
        AddVariable(States.Enabled, "ThumbInstance.IsEnabled", true);

        SliderCategory.States.Add(States.Focused);
        AddVariable(States.Focused, "FocusedIndicator.Visible", true);

        SliderCategory.States.Add(States.Highlighted);
        AddVariable(States.Highlighted, "FocusedIndicator.Visible", false);

        SliderCategory.States.Add(States.HighlightedFocused);
        AddVariable(States.HighlightedFocused, "FocusedIndicator.Visible", true);

        SliderCategory.States.Add(States.Pushed);
        AddVariable(States.Pushed, "FocusedIndicator.Visible", false);

        if(tryCreateFormsObject)
        {
            FormsControlAsObject = new Controls.Slider(this);
        }
    }

    public Controls.Slider FormsControl => FormsControlAsObject as Controls.Slider;
}
