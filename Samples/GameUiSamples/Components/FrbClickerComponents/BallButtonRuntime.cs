using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Components.FrbClickerComponents;
internal class BallButtonRuntime : InteractiveGue
{
    public BallButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {

            this.Width = 350;
            this.Height = 350;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

            this.XOrigin = HorizontalAlignment.Center;
            this.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

            var sprite = new SpriteRuntime();
            sprite.Name = "SpriteInstance";
            sprite.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            sprite.XOrigin = HorizontalAlignment.Center;
            sprite.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            sprite.YOrigin = VerticalAlignment.Center;
            sprite.SourceFileName = "Components/FrbClickerComponents/FrbIcon.png";

            this.Children.Add(sprite);



            var buttonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            buttonCategory.Name = "ButtonCategory";
            buttonCategory.States.Add(new()
            {
                Name = "Enabled",
                Variables = new()
                    {
                        new ()
                        {
                            Name = "SpriteInstance.Width",
                            Value = 100f,
                        },
                        new ()
                        {
                            Name = "SpriteInstance.Height",
                            Value = 100f,
                        },
                    }
            });

            buttonCategory.States.Add(new()
            {
                Name = "Highlighted",
                Variables = new()
                    {
                        new ()
                        {
                            Name = "SpriteInstance.Width",
                            Value = 102f,
                        },
                        new ()
                        {
                            Name = "SpriteInstance.Height",
                            Value = 102f,
                        },
                    }
            });

            buttonCategory.States.Add(new()
            {
                Name = "Pushed",
                Variables = new()
                    {
                        new ()
                        {
                            Name = "SpriteInstance.Width",
                            Value = 98f,
                        },
                        new ()
                        {
                            Name = "SpriteInstance.Height",
                            Value = 98f,
                        },
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
