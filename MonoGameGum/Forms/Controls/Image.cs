using MonoGameGum.GueDeriving;
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
#endif

#if !FRB

namespace Gum.Forms.Controls;
#endif
public class Image : MonoGameGum.Forms.Controls.FrameworkElement
{
    global::RenderingLibrary.Graphics.Sprite mContainedSprite;
    
    public string Source
    {
        set
        {
            Visual.SetProperty("SourceFile", value);
        }
    }

    public Microsoft.Xna.Framework.Graphics.Texture2D Texture
    {
        get
        {
            return mContainedSprite.Texture;
        }
        set
        {
            var shouldUpdateLayout = false;
            int widthBefore = -1;
            int heightBefore = -1;
            var isUsingPercentageWidthOrHeight = 
                Visual?.WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile ||
                Visual?.HeightUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            if (isUsingPercentageWidthOrHeight)
            {
                if (mContainedSprite.Texture != null)
                {
                    widthBefore = mContainedSprite.Texture.Width;
                    heightBefore = mContainedSprite.Texture.Height;
                }
            }
            mContainedSprite.Texture = value;
            if (isUsingPercentageWidthOrHeight)
            {
                int widthAfter = -1;
                int heightAfter = -1;
                if (mContainedSprite.Texture != null)
                {
                    widthAfter = mContainedSprite.Texture.Width;
                    heightAfter = mContainedSprite.Texture.Height;
                }
                shouldUpdateLayout = widthBefore != widthAfter || heightBefore != heightAfter;
            }
            if (shouldUpdateLayout)
            {
                Visual?.UpdateLayout();
            }
        }
    }

    public Image() :
        // SpriteRuntime is not an InteractiveGue, so don't use that:
        //base(new SpriteRuntime(fullInstantiation:true, tryCreateFormsObject:false))
        base(new Gum.Wireframe.InteractiveGue(new Sprite(texture:null)))
    {
        Visual.Width = 100;
        Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        Visual.Height = 100;
        Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

        IsVisible = true;
    }

    protected override void ReactToVisualChanged()
    {
        RefreshInternalVisualReferences();
        base.ReactToVisualChanged();
    }

    protected override void RefreshInternalVisualReferences()
    {
        mContainedSprite = Visual?.RenderableComponent as Sprite;
    }
}
