using System.Linq.Expressions;
using Gum.Wireframe;
using System;





#if FRB
using FlatRedBall.Gui;
using FlatRedBall.Forms.Controls;
using FlatRedBall.Forms.Data;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace MonoGameGum.Forms.Controls;
#elif RAYLIB
using RaylibGum;
using Gum.Forms.Data;
namespace Gum.Forms.Controls;
#else
using MonoGameGum.Forms.Data;
namespace MonoGameGum.Forms.Controls;
#endif


public static class FrameworkElementExt
{
    public static void SetBinding(this FrameworkElement element, string uiProperty, LambdaExpression propertyExpression) =>
        element.SetBinding(uiProperty, BinderHelpers.ExtractPath(propertyExpression));
    
    public static void SetBinding<T>(this FrameworkElement element, string uiProperty, Expression<Func<T, object?>> propertyExpression) =>
        element.SetBinding(uiProperty, BinderHelpers.ExtractPath(propertyExpression));

    public static FrameworkElement? GetFrameworkElement(this FrameworkElement element, string name)
    {
        return element.Visual?.GetFrameworkElementByName<FrameworkElement>(name);
    }

    public static T? GetFrameworkElement<T>(this FrameworkElement element, string name) where T : FrameworkElement
    {
        return element.Visual?.GetFrameworkElementByName<T>(name);
    }

    public static IInputReceiver? GetParentInputReceiver(this FrameworkElement element)
    {
        var parentGue = element.Visual.Parent as GraphicalUiElement;

        while (parentGue != null)
        {
            if (parentGue is IInputReceiver receiver)
            {
                return receiver;
            }
            if (parentGue is InteractiveGue interactiveGue &&
                interactiveGue.FormsControlAsObject is IInputReceiver found)
            {
                return found;
            }
            parentGue = parentGue.Parent as GraphicalUiElement;
        }
        return null;

    }

    public static void AddToRoot(this FrameworkElement element)
    {
        GumService.Default.Root.Children.Add(element.Visual);
    }

    public static void RemoveFromRoot(this FrameworkElement element)
    {
        element.Visual.Parent = null;
        element.Visual.RemoveFromManagers();
    }
}