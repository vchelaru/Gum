using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class SliderVisual : InteractiveGue
    {
        public ContainerRuntime TrackInstance { get; private set; }
        public NineSliceRuntime NineSliceInstance { get; private set; }
        public ButtonVisual ThumbInstance { get; private set; }
        public RectangleRuntime FocusedIndicator { get; private set; }
        public class SliderCategoryStates
        {
            public StateSave Enabled { get; set; }
            public StateSave Disabled { get; set; }
            public StateSave DisabledFocused { get; set; }
            public StateSave Focused { get; set; }
            public StateSave Highlighted { get; set; }
            public StateSave HighlightedFocused { get; set; }
            public StateSave Pushed { get; set; }
        }

        public SliderCategoryStates States;

        public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if(fullInstantiation)
            {
                Width = 128;
                Height = 24;
                States = new SliderCategoryStates();
                var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

                TrackInstance = new ContainerRuntime();
                TrackInstance.Name = "TrackInstance";
                TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TrackInstance.Width = -32f;
                TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TrackInstance.Height = 0f;
                TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                this.Children.Add(TrackInstance);

                NineSliceInstance = new NineSliceRuntime();
                NineSliceInstance.Name = "NineSliceInstance";
                NineSliceInstance.Y = 0;
                NineSliceInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                NineSliceInstance.YOrigin = VerticalAlignment.Center;
                NineSliceInstance.Width = 0f;
                NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                NineSliceInstance.Height = 8f;
                NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                NineSliceInstance.Color = Styling.Colors.DarkGray;
                NineSliceInstance.Texture = uiSpriteSheetTexture;
                NineSliceInstance.ApplyState(Styling.NineSlice.Bordered);
                TrackInstance.Children.Add(NineSliceInstance);

                ThumbInstance = new ButtonVisual();
                ThumbInstance.Name = "ThumbInstance";
                ThumbInstance.TextInstance.Text = "";
                ThumbInstance.XUnits = GeneralUnitType.Percentage;
                ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                ThumbInstance.Width = 32f;
                ThumbInstance.Height = 24f;
                ThumbInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                TrackInstance.Children.Add(ThumbInstance);

                FocusedIndicator = new RectangleRuntime();
                FocusedIndicator.Name = "FocusedIndicator";
                FocusedIndicator.X = 0;
                FocusedIndicator.Y = 0;
                FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.XOrigin = HorizontalAlignment.Center;
                FocusedIndicator.YOrigin = VerticalAlignment.Center;
                FocusedIndicator.Width = 0;
                FocusedIndicator.Height = 0;
                FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.Color = Color.White;
                FocusedIndicator.Visible = false;
                this.Children.Add(FocusedIndicator);

                var sliderCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                sliderCategory.Name = "SliderCategory";
                this.AddCategory(sliderCategory);

                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    sliderCategory.States.Add(state);
                    currentState = state;
                }

                void AddVariable(string name, object value)
                {
                    currentState.Variables.Add(new VariableSave
                    {
                        Name = name,
                        Value = value
                    });
                }

                AddState(FrameworkElement.DisabledStateName);
                AddVariable("FocusedIndicator.Visible", false);
                AddVariable("ThumbInstance.IsEnabled", false);
                this.States.Disabled = currentState;

                AddState(FrameworkElement.DisabledFocusedStateName);
                AddVariable("FocusedIndicator.Visible", true);
                AddVariable("ThumbInstance.IsEnabled", false);
                this.States.DisabledFocused = currentState;

                AddState(FrameworkElement.EnabledStateName);
                AddVariable("FocusedIndicator.Visible", false);
                AddVariable("ThumbInstance.IsEnabled", true);
                this.States.Enabled = currentState;

                AddState(FrameworkElement.FocusedStateName);
                AddVariable("FocusedIndicator.Visible", true);
                this.States.Focused = currentState;

                AddState(FrameworkElement.HighlightedStateName);
                AddVariable("FocusedIndicator.Visible", false);
                this.States.Highlighted = currentState;

                AddState(FrameworkElement.HighlightedFocusedStateName);
                AddVariable("FocusedIndicator.Visible", true);
                this.States.HighlightedFocused = currentState;

                AddState(FrameworkElement.PushedStateName);
                AddVariable("FocusedIndicator.Visible", false);
                this.States.Pushed = currentState;

                this.AddCategory(sliderCategory);
            }

            if(tryCreateFormsObject)
            {
                FormsControlAsObject = new Controls.Slider(this);
            }
        }

        public Controls.Slider FormsControl => FormsControlAsObject as Controls.Slider;
    }
}
