using Gum.DataTypes;
using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace SkiaGum.GueDeriving;

public class SpriteRuntime : BindableGue
{
    Sprite mContainedSprite;
    Sprite ContainedSprite
    {
        get
        {
            if(mContainedSprite == null)
            {
                mContainedSprite = this.RenderableComponent as Sprite;
            }
            return mContainedSprite;
        }
    }

    public string SourceFile
    {
        // eventually we may want to store this off somehow
        get => null;
        set
        {
            if(string.IsNullOrEmpty(value))
            {
                Texture = null;
            }
            else
            {
                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;
                var image = contentLoader.LoadContent<SKBitmap>(value);
                Texture = image;
            }
        }
    }

    public SKBitmap Texture
    {
        get => ContainedSprite.Texture;
        set => ContainedSprite.Texture = value;
    }

    public SKImage Image
    {
        get => ContainedSprite.Image;
        set => ContainedSprite.Image = value;
    }

    public SpriteRuntime(bool fullInstantiaton = true)
    {
        if(fullInstantiaton)
        {
            SetContainedObject(new Sprite());

            WidthUnits = DimensionUnitType.PercentageOfSourceFile;
            HeightUnits = DimensionUnitType.PercentageOfSourceFile;

            Width = 100;
            Height = 100;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (SpriteRuntime)base.Clone();

        toReturn.mContainedSprite = null;

        return toReturn;
    }
}
