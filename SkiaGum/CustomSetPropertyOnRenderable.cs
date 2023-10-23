using ExCSS;
using Gum.Wireframe;
using HarfBuzzSharp;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaGum
{
    public class CustomSetPropertyOnRenderable
    {
        // todo - fill this out for the sake of performance...
        public static void SetPropertyOnRenderable(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
        {

        }

        public static void UpdateToFontValues(IText itext, GraphicalUiElement graphicalUiElement)
        {
            // BitmapFont font = null;

            var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
            var contentLoader = loaderManager.ContentLoader;

            //if(UseCustomFont)
            //{

            //}
            //else
            {
                if (/*FontSize > 0 &&*/ !string.IsNullOrEmpty(graphicalUiElement.Font))
                {
                    //SKTypeface font = contentLoader.LoadContent<SKTypeface>(Font);
                    if (graphicalUiElement.Font != null && itext is Text text)
                    {
                        text.FontName = graphicalUiElement.Font;
                        text.FontSize = graphicalUiElement.FontSize;
                    }
                }
            }
        }
    }
}
