using Gum.Wireframe;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace GameUiSamples.Components.FrbClickerComponents;
internal class BuildingButtonRuntime : InteractiveGue
{
    public string BuildingName
    {
        get => NameTextRuntime.Text;
        set => NameTextRuntime.Text = value;
    }

    public string Cost
    {
        get => CostTextRuntime.Text;
        set => CostTextRuntime.Text = value;
    }

    public string Amount
    {
        get => AmountTextRuntime.Text;
        set => AmountTextRuntime.Text = value;
    }

    TextRuntime NameTextRuntime;
    TextRuntime CostTextRuntime;
    TextRuntime AmountTextRuntime;

    public BuildingButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base(new InvisibleRenderable())
    {
        if(fullInstantiation)
        {
            this.Width = 0;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Height = 64;

            var background = new ColoredRectangleRuntime();
            background.Width = 0;
            background.Height = 0;
            background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.Name = "ButtonBackground";
            this.Children.Add(background);

            NameTextRuntime = new TextRuntime();
            NameTextRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Children.Add(NameTextRuntime);

            CostTextRuntime = new TextRuntime();
            this.Children.Add(CostTextRuntime);
            CostTextRuntime.YOrigin = VerticalAlignment.Bottom;
            CostTextRuntime.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            CostTextRuntime.Height = 0;
            CostTextRuntime.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            AmountTextRuntime = new TextRuntime();
            this.Children.Add(AmountTextRuntime);
            AmountTextRuntime.XOrigin = HorizontalAlignment.Right;
            AmountTextRuntime.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            AmountTextRuntime.HorizontalAlignment = HorizontalAlignment.Right;

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
                            Value = new Color(0, 0, 128),
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
                            Value = new Color(0, 0, 160),
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
                            Value = new Color(0, 0, 96),
                        }
                    }
            });

            buttonCategory.States.Add(new()
            {
                Name = "Disabled",
                Variables = new()
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

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }
    }
    public Button FormsControl => FormsControlAsObject as Button;

}
