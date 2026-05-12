#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Wireframe;
using Gum.Forms.Controls;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
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
