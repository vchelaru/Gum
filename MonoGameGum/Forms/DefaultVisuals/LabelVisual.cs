#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Wireframe;
using RenderingLibrary.Graphics;


#if XNALIKE
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
#else
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals;

public class LabelVisual : TextRuntime
{
    public LabelVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation)
    {
        if (fullInstantiation)
        {
            Name = "TextInstance";
            Text = "Label";
            X = 0;
            Y = 0;
            Width = 0;
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            Height = 0;
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Color = Styling.ActiveStyle.Colors.White;

            this.ApplyState(Styling.ActiveStyle.Text.Normal);
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label(this);
        }
    }

    public Label FormsControl => FormsControlAsObject as Label;
}
