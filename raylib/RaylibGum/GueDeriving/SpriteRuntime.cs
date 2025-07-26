using Gum.Renderables;
using Gum.Wireframe;
using Gum.Renderables;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.GueDeriving;
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

    public Color Color
    {
        get => ContainedSprite.Color;
        set
        {
            ContainedSprite.Color = value;
            NotifyPropertyChanged();
        }
    }

    //public Raylib_cs.Rectangle? SourceRectangle
    //{
    //    get
    //    {
    //        return ContainedSprite.SourceRectangle;
    //    }
    //    set
    //    {
    //        ContainedSprite.SourceRectangle = value;
    //        NotifyPropertyChanged();
    //    }
    //}

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
