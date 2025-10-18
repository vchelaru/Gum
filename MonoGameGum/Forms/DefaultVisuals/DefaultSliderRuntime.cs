using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class DefaultSliderRuntime : InteractiveGue
    {
        public RectangleRuntime FocusedIndicator { get; private set; }

        public DefaultSliderRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if(fullInstantiation)
            {
                this.Width = 128;
                this.Height = 24;

                var TrackInstance = new InteractiveGue(new InvisibleRenderable());
                TrackInstance.Name = "TrackInstance";
                var NineSliceInstance = new ColoredRectangleRuntime();
                NineSliceInstance.Name = "NineSliceInstance";
                var ThumbInstance = new DefaultButtonRuntime();
                ThumbInstance.Name = "ThumbInstance";
                this.Children.Add(TrackInstance);
                TrackInstance.Children.Add(NineSliceInstance);
                TrackInstance.Children.Add(ThumbInstance);



                TrackInstance.Height = 0f;
                TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TrackInstance.Width = -32f;
                TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                NineSliceInstance.Color = new Microsoft.Xna.Framework.Color(70, 70, 70);
                NineSliceInstance.Height = 8f;
                NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                NineSliceInstance.Width = 0f;
                NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                NineSliceInstance.Y = 0;
                NineSliceInstance.YOrigin = VerticalAlignment.Center;
                NineSliceInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                ThumbInstance.TextInstance.Text = "";
                ThumbInstance.Height = 24f;
                ThumbInstance.Width = 32f;
                ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                ThumbInstance.XUnits = GeneralUnitType.Percentage;
                ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                FocusedIndicator = new RectangleRuntime();
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
                FocusedIndicator.Name = "FocusedIndicator";
                this.Children.Add(FocusedIndicator);

                //var background = new ColoredRectangleRuntime();
                //background.Width = 0;
                //background.Height = 0;
                //background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                //background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                //background.Name = "SliderBackground";
                //this.Children.Add(background);

                //var thumb = new ColoredRectangleRuntime();
                //thumb.Width = 0;
                //thumb.Height = 0;
                //thumb.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                //thumb.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                //thumb.Name = "SliderThumb";
                //this.Children.Add(thumb);


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


                AddState(FrameworkElement.DisabledFocusedStateName);
                AddVariable("FocusedIndicator.Visible", true);
                AddVariable("ThumbInstance.IsEnabled", false);

                AddState(FrameworkElement.EnabledStateName);
                AddVariable("FocusedIndicator.Visible", false);
                AddVariable("ThumbInstance.IsEnabled", true);

                AddState(FrameworkElement.FocusedStateName);
                AddVariable("FocusedIndicator.Visible", true);

                AddState(FrameworkElement.HighlightedStateName);
                AddVariable("FocusedIndicator.Visible", false);

                AddState(FrameworkElement.HighlightedFocusedStateName);
                AddVariable("FocusedIndicator.Visible", true);

                AddState(FrameworkElement.PushedStateName);
                AddVariable("FocusedIndicator.Visible", false);


                this.AddCategory(sliderCategory);
            }

            if(tryCreateFormsObject)
            {
                FormsControlAsObject = new Gum.Forms.Controls.Slider(this);
            }
        }

        public Gum.Forms.Controls.Slider FormsControl => FormsControlAsObject as Gum.Forms.Controls.Slider;
    }
}
