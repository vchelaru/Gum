using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if FRB
using FlatRedBall.Forms.Controls;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace MonoGameGum.Forms;

#endif


#if !FRB
using Gum.Forms.Controls;
namespace Gum.Forms;

#endif



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
            throw new ArgumentException("The GraphicalUiElement with the name " + name + " does not have a FormsControlAsObject. In other words, this is just a visual, not a Forms control.");
        }
#endif
        var frameworkElement = formsControlAsObject as FrameworkElementType;
        if (frameworkElement == null)
        {
#if DEBUG
            var message = "The GraphicalUiElement with the name " + name +
                " is expected to be of type " + typeof(FrameworkElementType) + " but is instead " + formsControlAsObject?.GetType();

            throw new ArgumentException(message);
#endif
        }
        return frameworkElement;
    }

    public static FrameworkElementType TryGetFrameworkElementByName<FrameworkElementType>(this GraphicalUiElement graphicalUiElement, string name) where FrameworkElementType : FrameworkElement
    {
        var frameworkVisual = graphicalUiElement.GetGraphicalUiElementByName(name);

        if (frameworkVisual == null)
        {
            return default(FrameworkElementType);
        }

        var frameworkVisualAsInteractiveGue = frameworkVisual as InteractiveGue;

        if (frameworkVisualAsInteractiveGue == null)
        {
            return default(FrameworkElementType);
        }

        var formsControlAsObject = frameworkVisualAsInteractiveGue?.FormsControlAsObject;

        if (formsControlAsObject == null)
        {
            return default(FrameworkElementType);
        }

        var frameworkElement = formsControlAsObject as FrameworkElementType;
        if (frameworkElement == null)
        {
            return default(FrameworkElementType);

        }
        return frameworkElement;
    }

}
