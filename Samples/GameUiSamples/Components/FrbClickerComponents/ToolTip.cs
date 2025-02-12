using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Components.FrbClickerComponents;
public class ToolTip : InteractiveGue
{
    TextRuntime TextInstance { get; set; }

    public string Text
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public ToolTip(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 8;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Height = 8;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.HasEvents = false;

            var background = new ColoredRectangleRuntime();
            background.Width = 0;
            background.Height = 0;
            background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.Name = "Background";
            background.Color = new Microsoft.Xna.Framework.Color(0, 0, 0);
            this.Children.Add(background);


            TextInstance = new TextRuntime();
            TextInstance.X = 0;
            TextInstance.Y = 0;
            TextInstance.Width = 0;
            TextInstance.Height = 0;
            TextInstance.Name = "TextInstance";
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            TextInstance.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Children.Add(TextInstance);
        }
    }
}
