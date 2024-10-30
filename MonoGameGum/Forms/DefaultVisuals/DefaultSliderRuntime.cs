using Gum.Converters;
using Gum.Wireframe;
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
                var FocusedIndicator = new ColoredRectangleRuntime();
                FocusedIndicator.Name = "FocusedIndicator";

                this.Children.Add(TrackInstance);
                TrackInstance.Children.Add(NineSliceInstance);
                TrackInstance.Children.Add(ThumbInstance);
                this.Children.Add(FocusedIndicator);



                TrackInstance.Height = 0f;
                TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                TrackInstance.Width = -32f;
                TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

                NineSliceInstance.Color = new Microsoft.Xna.Framework.Color(70, 70, 70);
                NineSliceInstance.Height = 8f;
                NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                NineSliceInstance.Width = 0f;
                NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
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

                //FocusedIndicator.ColorCategoryState = NineSliceRuntime.ColorCategory.Warning;
                //FocusedIndicator.StyleCategoryState = NineSliceRuntime.StyleCategory.Solid;
                FocusedIndicator.Height = 2f;
                FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                FocusedIndicator.Visible = false;
                FocusedIndicator.Y = 2f;
                FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

                //var background = new ColoredRectangleRuntime();
                //background.Width = 0;
                //background.Height = 0;
                //background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                //background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                //background.Name = "SliderBackground";
                //this.Children.Add(background);

                //var thumb = new ColoredRectangleRuntime();
                //thumb.Width = 0;
                //thumb.Height = 0;
                //thumb.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                //thumb.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                //thumb.Name = "SliderThumb";
                //this.Children.Add(thumb);

                var sliderCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                sliderCategory.Name = "SliderCategory";
                sliderCategory.States.Add(new ()
                {
                    //Name = "Enabled",
                    //Variables = new ()
                    //{
                    //    new ()
                    //    {
                    //        Name = "SliderBackground.Color",
                    //        Value = new Microsoft.Xna.Framework.Color(0, 0, 128),
                    //    },
                    //    new ()
                    //    {
                    //        Name = "SliderThumb.Color",
                    //        Value = new Microsoft.Xna.Framework.Color(128, 0, 0),
                    //    }
                    //}
                });

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
