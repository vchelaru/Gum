using Gum.Wireframe;
using RenderingLibrary.Graphics;


#if RAYLIB
using Gum.GueDeriving;
using Gum.Forms.Controls;
using Raylib_cs;
namespace Gum.Forms.DefaultVisuals;
#else
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
namespace MonoGameGum.Forms.DefaultVisuals;
#endif

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
        }
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Label(this);
        }
    }

    public Label FormsControl => FormsControlAsObject as Label;
}
