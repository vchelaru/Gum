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
        public TextRuntime TextInstance { get; private set; }
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

            TextInstance = new TextRuntime();
            TextInstance.X = 0;
            TextInstance.Y = 0;
            TextInstance.Width = 0;
            TextInstance.Height = 0;
            TextInstance.Name = "TextInstance";
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            TextInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            TextInstance.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Children.Add(TextInstance);



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
