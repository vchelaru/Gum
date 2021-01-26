using Gum.DataTypes;
using Gum.Wireframe;
using SkiaSharp.Extended.Svg;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class SvgRuntime : BindableGraphicalUiElement
    {
        VectorSprite mContainedSprite;
        VectorSprite ContainedSprite
        {
            get
            {
                if (mContainedSprite == null)
                {
                    mContainedSprite = this.RenderableComponent as VectorSprite;
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
                SKSvg skiaSvg = contentLoader.LoadContent<SKSvg>(value);
                Texture = skiaSvg;
            }
        }

        public SKSvg Texture
        {
            get => ContainedSprite.Texture;
            set => ContainedSprite.Texture = value;
        }

        public SvgRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                //SetGraphicalUiElement
                SetContainedObject(new VectorSprite());

                // Give it some good defaults:
                WidthUnits = DimensionUnitType.Absolute;
                HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

                Width = 100;
                Height = 100;
            }
        }
    }
}
