using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms;

public static class GraphicalUiElementFormsExtensions
{
    public static FrameworkElementType GetFrameworkElementByName<FrameworkElementType>(this GraphicalUiElement graphicalUiElement, string name) where FrameworkElementType : FrameworkElement
    {
        var frameworkVisual = graphicalUiElement.GetGraphicalUiElementByName(name);

#if DEBUG
        if(frameworkVisual == null)
        {
            throw new ArgumentException("Could not find a GraphicalUiElement with the name " + name);
        }
#endif

        var frameworkVisualAsInteractiveGue = frameworkVisual as InteractiveGue;

#if DEBUG

        if (frameworkVisualAsInteractiveGue == null)
        {
            throw new ArgumentException("The GraphicalUiElement with the name " + name + " is not an InteractiveGue");
        }

#endif
        var formsControlAsObject = frameworkVisualAsInteractiveGue?.FormsControlAsObject;

#if DEBUG

        if (formsControlAsObject == null)
        {
            throw new ArgumentException("The GraphicalUiElement with the name " + name + " does not have a FormsControlAsObject");
        }
#endif
        var frameworkElement = formsControlAsObject as FrameworkElementType;
        if (frameworkElement == null)
        {
#if DEBUG
            throw new ArgumentException("The GraphicalUiElement with the name " + name + " is not of type " + typeof(FrameworkElementType));
#endif
        }
        return frameworkElement;
    }
}
