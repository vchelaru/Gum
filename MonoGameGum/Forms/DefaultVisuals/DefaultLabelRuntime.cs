using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultLabelRuntime : TextRuntime
{

    public DefaultLabelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation)
    {
        this.HasEvents = false;
        if (fullInstantiation)
        {
            this.Width = 0;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Height = 0;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            Name = "TextInstance";
            Text = "Label";
            Width = 0;
            Height = 0;
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label(this);
        }

    }

    public Label FormsControl => FormsControlAsObject as Label;
}
