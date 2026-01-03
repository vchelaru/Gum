using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace Gum.RenderingLibrary
{
    public static class IPositionedSizedObjectExtensionMethods
    {
        public static void GetParentWidthAndHeight(this IRenderableIpso ipso, float canvasWidth, float canvasHeight, out float parentWidth, out float parentHeight)
        {


            if (ipso.Parent == null)
            {
                parentWidth = canvasWidth;
                parentHeight = canvasHeight;
            }
            else
            {
                parentWidth = ipso.Parent.Width;
                parentHeight = ipso.Parent.Height;
            }
        }

        public static void GetFileWidthAndHeightOrDefault(this IRenderableIpso ipso, out float fileWidth, out float fileHeight)
        {
            // to prevent divide-by-zero issues
            fileWidth = 32;
            fileHeight = 32;

            var iTextureCoordinate = ipso as ITextureCoordinate;

            if(iTextureCoordinate == null && ipso is GraphicalUiElement graphicalUiElement)
            {
                iTextureCoordinate = graphicalUiElement.RenderableComponent as ITextureCoordinate;
            }

            if (iTextureCoordinate != null)
            {
                fileWidth = iTextureCoordinate.TextureWidth ?? 0;
                fileHeight = iTextureCoordinate.TextureHeight ?? 0;
            }
        }

#if MONOGAME
        public static string GetQualifiedPrefixWithDot(IRenderableIpso ipso, ElementSave elementSaveForIpso, ElementSave containerElement)
        {
            string qualifiedVariablePrefixWithDot = ipso.Name + ".";

            IRenderableIpso parent = ipso.Parent;

            while (parent != null)
            {
                qualifiedVariablePrefixWithDot = parent.Name + "." + qualifiedVariablePrefixWithDot;
                parent = parent.Parent;
            }

            if (
                // Not sure why we checked to make sure it doesn't contain a dot.  It always will because
                // of the first line:
                //!qualifiedVariablePrefixWithDot.Contains(".") && 
                elementSaveForIpso == containerElement)
            {
                qualifiedVariablePrefixWithDot = "";
            }
            if (containerElement is ComponentSave || containerElement is StandardElementSave)
            {
                // If the containerElement is a ComponentSave, then we assume (currently) that the ipso is attached to it.  Since we'll be using
                // the container element to get the variable, we don't want to include the name of the element, so let's get rid of the first part.
                // Update June 13, 2012
                // If we pass a Text object
                // that is the child of a Button
                // and the Button is the container
                // element, we want to return "TextInstance."
                // as the prefix.  The code below prevents that.
                // I now have unit tests for this so I'm going to
                // remove the code below and modify it later if it
                // turns out we really do need it.
                // Update June 13, 2012
                // Now we climb up the parent/child
                // relationship until we get to the root
                // If the containerElement is a ComponentSave
                // or StandardElementSave (although maybe this
                // case will never happen) we need to remove the
                // first name before the dot because it's the element
                // itself.  If it's a Screen there is no attachment so
                // the first name won't be the name of the Screen.
                int indexOfDot = qualifiedVariablePrefixWithDot.IndexOf('.');
                if (indexOfDot != -1)
                {
                    qualifiedVariablePrefixWithDot = qualifiedVariablePrefixWithDot.Substring(indexOfDot + 1, qualifiedVariablePrefixWithDot.Length - (indexOfDot + 1));
                }
            }


            return qualifiedVariablePrefixWithDot;
        }
                
#endif

        public static string GetAttachmentQualifiedName(this IRenderableIpso ipso, List<ElementWithState> elementStack)
        {
            IRenderableIpso parent = ipso.Parent;
            IRenderableIpso child = ipso;

            while (parent != null)
            {
                if (parent.Tag is ElementSave || parent.Tag == null)
                {
                    // we found it, so break!
                    break;
                }
                else
                {
                    InstanceSave thisInstance = child.Tag as InstanceSave;

                    if (thisInstance.IsParentASibling(elementStack))
                    {
                        child = parent;
                        parent = parent.Parent;
                    }
                    else
                    {
                        // we found it, so break;
                        break;
                    }
                }
            }


            if (parent == null)
            {
                return ipso.Name;
            }
            else
            {
                var parentName = parent.GetAttachmentQualifiedName(elementStack);
                if (!string.IsNullOrEmpty(parentName))
                {
                    return parentName + "." + ipso.Name;
                }
                else
                {
                    return ipso.Name;

                }
            }

        }


    }
}
