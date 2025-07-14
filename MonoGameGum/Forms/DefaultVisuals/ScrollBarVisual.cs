using Gum.Converters;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class ScrollBarVisual : InteractiveGue
    {
        public ButtonVisual UpButtonInstance { get; private set; }
        public SpriteRuntime UpButtonIcon { get; private set; }
        public ButtonVisual DownButtonInstance { get; private set; }
        public SpriteRuntime DownButtonIcon { get; private set; }
        public ContainerRuntime ThumbContainer {  get; private set; }
        public NineSliceRuntime TrackBackground { get; private set; }
        public ButtonVisual ThumbInstance { get; private set; }


        public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable()) 
        {
            if(fullInstantiation)
            {
                this.Width = 24;
                this.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

                this.Height = 128;
                this.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

                var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

                UpButtonIcon = new SpriteRuntime();
                UpButtonIcon.X = 0f;
                UpButtonIcon.XUnits = GeneralUnitType.PixelsFromMiddle;
                UpButtonIcon.Y = 0f;
                UpButtonIcon.YUnits = GeneralUnitType.PixelsFromMiddle;
                UpButtonIcon.XOrigin = HorizontalAlignment.Center;
                UpButtonIcon.YOrigin = VerticalAlignment.Center;
                UpButtonIcon.Width = 100;
                UpButtonIcon.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                UpButtonIcon.Height = 100;
                UpButtonIcon.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                UpButtonIcon.ApplyState(Styling.Icons.Arrow1);
                UpButtonIcon.Color = Styling.Colors.White;
                UpButtonIcon.Texture = uiSpriteSheetTexture;
                UpButtonIcon.Visible = true;
                UpButtonIcon.Rotation = 90;

                UpButtonInstance = new ButtonVisual();
                UpButtonInstance.Name = "UpButtonInstance";
                UpButtonInstance.TextInstance.Text = "";
                UpButtonInstance.Height = 24f;
                UpButtonInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                UpButtonInstance.Width = 0;
                UpButtonInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                UpButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                UpButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                UpButtonInstance.Children.Add(UpButtonIcon);
                this.Children.Add(UpButtonInstance);

                DownButtonIcon = new SpriteRuntime();
                DownButtonIcon.X = 0f;
                DownButtonIcon.XUnits = GeneralUnitType.PixelsFromMiddle;
                DownButtonIcon.Y = 0f;
                DownButtonIcon.YUnits = GeneralUnitType.PixelsFromMiddle;
                DownButtonIcon.XOrigin = HorizontalAlignment.Center;
                DownButtonIcon.YOrigin = VerticalAlignment.Center;
                DownButtonIcon.Width = 100;
                DownButtonIcon.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                DownButtonIcon.Height = 100;
                DownButtonIcon.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                DownButtonIcon.ApplyState(Styling.Icons.Arrow1);
                DownButtonIcon.Color = Styling.Colors.White;
                DownButtonIcon.Texture = uiSpriteSheetTexture;
                DownButtonIcon.Visible = true;
                DownButtonIcon.Rotation = -90;

                DownButtonInstance = new ButtonVisual();
                DownButtonInstance.Name = "DownButtonInstance";
                DownButtonInstance.TextInstance.Text = "";
                DownButtonInstance.Height = 24f;
                DownButtonInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                DownButtonInstance.Width = 0f;
                DownButtonInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                DownButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                DownButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;
                DownButtonInstance.Children.Add(DownButtonIcon);
                this.Children.Add(DownButtonInstance);


                ThumbContainer = new ContainerRuntime();
                ThumbContainer.Name = "ThumbContainer";
                ThumbContainer.Height = -48f;
                ThumbContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ThumbContainer.Width = 0f;
                ThumbContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ThumbContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                ThumbContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
                ThumbContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                ThumbContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
                this.Children.Add(ThumbContainer);


                TrackBackground = new NineSliceRuntime();
                TrackBackground.Name = "TrackInstance";
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
                TrackBackground.ApplyState(Styling.NineSlice.Solid);
                TrackBackground.Color = Styling.Colors.Gray;
                TrackBackground.Texture = uiSpriteSheetTexture;
                ThumbContainer.Children.Add(TrackBackground);

                ThumbInstance = new ButtonVisual();
                ThumbInstance.Name = "ThumbInstance";
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
