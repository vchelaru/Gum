using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultLabelRuntime : InteractiveGue
{
    public DefaultLabelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 0;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Height = 0;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            var TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            TextInstance.Text = "Label";
            TextInstance.Width = 0;
            TextInstance.Height = 0;
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            this.Children.Add(TextInstance);
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label();
        }

    }

    public Label FormsControl => FormsControlAsObject as Label;
}
