using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace GumFormsSample
{
    internal class FullyCustomizedButton : InteractiveGue
    {
        public TextRuntime TextInstance { get; private set; }
        public FullyCustomizedButton(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.Width = 128;
                this.Height = 32;

                var background = new NineSliceRuntime();
                background.Width = 0;
                background.Height = 0;
                background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                background.Name = "ButtonBackground";
                // This depends on the current RelativeDirectory. Typically the RelativeDirectory
                // is set to Content since that's where the Gum project lives. You may need to adjust
                // your SourceFileName to account for the relative directory.
                background.SourceFileName = "../button_square_gradient.png";
                this.Children.Add(background);

                // TextInstance is copied as-is from DefaultButtonRuntime.cs
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
                buttonCategory.States.Add(new()
                {
                    Name = "Enabled",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = new Color(255, 255, 255),
                        }
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = "Highlighted",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = new Color(230, 230, 230),
                        }
                    }
                });

                buttonCategory.States.Add(new()
                {
                    Name = "Pushed",
                    Variables = new()
                    {
                        new ()
                        {
                            Name = "ButtonBackground.Color",
                            Value = new Color(128, 128, 128),
                        }
                    }
                });

                this.AddCategory(buttonCategory);
            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new Button(this);
            }
        }
        public Button FormsControl => FormsControlAsObject as Button;

    }
}
