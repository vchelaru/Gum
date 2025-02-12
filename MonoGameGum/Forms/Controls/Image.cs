using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class Image : FrameworkElement
{
    public string Source
    {
        set
        {
            Visual.SetProperty("SourceFile", value);
        }
    }

    public Image() :
        base(new Gum.Wireframe.InteractiveGue(new Sprite(texture:null)))
    {
        Visual.Width = 100;
        Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        Visual.Height = 100;
        Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

        IsVisible = true;
    }

}
