using Gum.DataTypes;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using HarfBuzzSharp;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SkiaGum
{
    public class CustomSetPropertyOnRenderable
    {
        // todo - fill this out for the sake of performance...
        public static void SetPropertyOnRenderable(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
        {
            bool handled = false;
            if (mContainedObjectAsIpso is Text asText)
            {
                handled = TrySetPropertyOnText(asText, graphicalUiElement, propertyName, value);
            }

            if (!handled)
            {
                //SetPropertyOnRenderable(mContainedObjectAsIpso, propertyName, value);
            }
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



        private static bool TrySetPropertyOnText(Text text, GraphicalUiElement gue, string propertyName, object value)
        {
            bool handled = false;

            void ReactToFontValueChange()
            {
                gue.UpdateToFontValues();
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    gue.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    gue.UpdateLayout();
                }
                handled = true;
            }

            if (propertyName == "Text")
            {
                if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    gue.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    // make it have no line wrap width before assignign the text:
                    text.Width = 0;
                }

                text.RawText = value as string;
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    gue.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    gue.UpdateLayout();
                }
                handled = true;
            }
            else if (propertyName == "Font Scale")
            {
                text.FontScale = (float)value;
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    gue.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    gue.UpdateLayout();
                }
                handled = true;

            }
            else if (propertyName == "Font")
            {
                gue.Font = value as string;

                ReactToFontValueChange();
            }


            else if (propertyName == nameof(gue.FontSize))
            {
                gue.FontSize = (int)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(gue.OutlineThickness))
            {
                gue.OutlineThickness = (int)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(gue.IsItalic))
            {
                gue.IsItalic = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(gue.IsBold))
            {
                gue.IsBold = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(gue.UseFontSmoothing))
            {
                gue.UseFontSmoothing = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(Blend))
            {
#if MONOGAME || XNA4
                var valueAsGumBlend = (RenderingLibrary.Blend)value;

                var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                var text = mContainedObjectAsIpso as Text;
                text.BlendState = valueAsXnaBlend;
                handled = true;
#endif
            }
            else if (propertyName == "Alpha")
            {
#if MONOGAME || XNA4
                int valueAsInt = (int)value;
                ((Text)mContainedObjectAsIpso).Alpha = valueAsInt;
                handled = true;
#endif
            }
            else if (propertyName == "Red")
            {
                int valueAsInt = (int)value;
                text.Red = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Green")
            {
                int valueAsInt = (int)value;
                text.Green = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Blue")
            {
                int valueAsInt = (int)value;
                text.Blue = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Color")
            {
#if MONOGAME || XNA4
                var valueAsColor = (Color)value;
                ((Text)mContainedObjectAsIpso).Color = valueAsColor;
                handled = true;
#endif
            }

            else if (propertyName == "HorizontalAlignment")
            {
                text.HorizontalAlignment = (RenderingLibrary.Graphics.HorizontalAlignment)value;
                handled = true;
            }
            else if (propertyName == "VerticalAlignment")
            {
                text.VerticalAlignment = (VerticalAlignment)value;
                handled = true;
            }
            else if (propertyName == "MaxLettersToShow")
            {
#if MONOGAME || XNA4
                ((Text)mContainedObjectAsIpso).MaxLettersToShow = (int?)value;
                handled = true;
#endif
            }

            else if (propertyName == nameof(TextOverflowHorizontalMode))
            {
                var textOverflowMode = (TextOverflowHorizontalMode)value;

                if (textOverflowMode == TextOverflowHorizontalMode.EllipsisLetter)
                {
                    text.IsTruncatingWithEllipsisOnLastLine = true;
                }
                else
                {
                    text.IsTruncatingWithEllipsisOnLastLine = false;
                }
            }
            else if (propertyName == nameof(TextOverflowVerticalMode))
            {
                var textOverflowMode = (TextOverflowVerticalMode)value;
#if MONOGAME || XNA4

                ((Text)mContainedObjectAsIpso).TextOverflowVerticalMode = textOverflowMode;
#endif

            }

            return handled;
        }



    }
}
