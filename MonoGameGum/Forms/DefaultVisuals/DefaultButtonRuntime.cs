using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class DefaultButtonRuntime : InteractiveGue
    {
        public DefaultButtonRuntime() : base(new InvisibleRenderable())
        {
            this.Width = 128;
            this.Height = 32;

            var background = new ColoredRectangleRuntime();
            background.Width = 0;
            background.Height = 0;
            background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.Name = "ButtonBackground";
            this.Children.Add(background);

            var text = new TextRuntime();
            text.X = 0;
            text.Y = 0;
            text.Width = 0;
            text.Height = 0;
            text.Name = "TextInstance";
            text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            text.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            text.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            text.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            text.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            text.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            text.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            text.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Children.Add(text);



            var buttonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            buttonCategory.Name = "ButtonCategory";
            buttonCategory.States.Add(new ()
            {
                Name = "Enabled",
                Variables = new ()
                {
                    new ()
                    {
                        Name = "ButtonBackground.Color",
                        Value = new Color(0, 0, 128),
                    }
                }
            });

            buttonCategory.States.Add(new ()
            {
                Name = "Highlighted",
                Variables = new ()
                {
                    new ()
                    {
                        Name = "ButtonBackground.Color",
                        Value = new Color(0, 0, 160),
                    }
                }
            });

            buttonCategory.States.Add(new ()
            {
                Name = "Pushed",
                Variables = new ()
                {
                    new ()
                    {
                        Name = "ButtonBackground.Color",
                        Value = new Color(0, 0, 96),
                    }
                }
            });

            buttonCategory.States.Add(new ()
            {
                Name = "Disabled",
                Variables = new ()
                {
                    new ()
                    {
                        Name = "ButtonBackground.Color",
                        Value = new Color(48, 48, 64),
                    }
                }
            });

            this.AddCategory(buttonCategory);
        }
    }
}
