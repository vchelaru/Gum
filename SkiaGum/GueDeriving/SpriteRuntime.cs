using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class SpriteRuntime : BindableGraphicalUiElement
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
                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;
                var image = contentLoader.LoadContent<SKBitmap>(value);
                Texture = image;
            }
        }

        public SKBitmap Texture
        {
            get => ContainedSprite.Texture;
            set => ContainedSprite.Texture = value;
        }

        public SpriteRuntime(bool fullInstantiaton = true)
        {
            if(fullInstantiaton)
            {
                SetContainedObject(new Sprite());

                WidthUnits = Gum.Wireframe.DimensionUnitType.PercentageOfSourceFile;
                HeightUnits = Gum.Wireframe.DimensionUnitType.PercentageOfSourceFile;

                Width = 100;
                Height = 100;
            }
        }
    }
}
