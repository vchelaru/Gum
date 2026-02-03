using Gum.Converters;
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
    public class DefaultScrollBarRuntime : InteractiveGue
    {
        public DefaultScrollBarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable()) 
        {
            this.HasEvents = true;

            if (fullInstantiation)
            {
                this.Width = 24;
                this.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

                this.Height = 128;
                this.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

                var UpButtonInstance = new DefaultButtonRuntime();
                UpButtonInstance.Name = "UpButtonInstance";
                var DownButtonInstance = new DefaultButtonRuntime();
                DownButtonInstance.Name = "DownButtonInstance";
                var ThumbContainer = new ContainerRuntime();
                ThumbContainer.Name = "ThumbContainer";
                var trackSolidRectangle = new SolidRectangle();
                var TrackBackground = new InteractiveGue(trackSolidRectangle);
                TrackBackground.Name = "TrackInstance";
                var ThumbInstance = new DefaultButtonRuntime();
                ThumbInstance.Name = "ThumbInstance";



                UpButtonInstance.TextInstance.Text = "^";
                UpButtonInstance.Height = 24f;
                UpButtonInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                UpButtonInstance.Width = 0;
                UpButtonInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                UpButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                UpButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                this.Children.Add(UpButtonInstance);

                DownButtonInstance.TextInstance.Text = "v";
                DownButtonInstance.Height = 24f;
                DownButtonInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                DownButtonInstance.Width = 0f;
                DownButtonInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                DownButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                DownButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;
                this.Children.Add(DownButtonInstance);

                ThumbContainer.Height = -48f;
                ThumbContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ThumbContainer.Width = 0f;
                ThumbContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ThumbContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                ThumbContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
                ThumbContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                ThumbContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
                this.Children.Add(ThumbContainer);

                trackSolidRectangle.Color = System.Drawing.Color.FromArgb(255, 130, 130, 130);
                TrackBackground.Height = 0f;
                TrackBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TrackBackground.Width = 0f;
                TrackBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TrackBackground.X = 0f;
                TrackBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TrackBackground.XUnits = GeneralUnitType.PixelsFromMiddle;
                TrackBackground.Y = 0f;
                TrackBackground.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TrackBackground.YUnits = GeneralUnitType.PixelsFromMiddle;
                ThumbContainer.Children.Add(TrackBackground);

                ThumbInstance.TextInstance.Text = String.Empty;
                ThumbInstance.Width = 0f;
                ThumbInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                ThumbInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                ThumbContainer.Children.Add(ThumbInstance);
            }

            if(tryCreateFormsObject)
            {
                this.FormsControlAsObject = new ScrollBar(this);
            }
        }

        public ScrollBar FormsControl => this.FormsControlAsObject as ScrollBar;
    }
}
