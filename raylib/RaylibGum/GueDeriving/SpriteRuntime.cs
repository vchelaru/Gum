using Gum.Wireframe;
using GumTest.Renderables;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.GueDeriving;
public class SpriteRuntime : BindableGue
{
    Sprite mContainedSprite;
    Sprite ContainedSprite
    {
        get
        {
            if (mContainedSprite == null)
            {
                mContainedSprite = this.RenderableComponent as Sprite;
            }
            return mContainedSprite;
        }
    }

    public Texture2D Texture
    {
        get => ContainedSprite.Texture;
        set => ContainedSprite.Texture = value;
    }


    public SpriteRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if (fullInstantiation)
        {
            mContainedSprite = new Sprite();
            SetContainedObject(mContainedSprite);
            Width = 100;
            Height = 100;
            WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        }
    }

}
