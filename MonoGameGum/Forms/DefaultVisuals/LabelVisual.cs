using Gum.Wireframe;
using RenderingLibrary.Graphics;


#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;
#else
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
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
            this.Color = Color.White;

            this.ApplyState(Styling.ActiveStyle.Text.Normal);
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label(this);
        }
    }

    public Label FormsControl => FormsControlAsObject as Label;
}
