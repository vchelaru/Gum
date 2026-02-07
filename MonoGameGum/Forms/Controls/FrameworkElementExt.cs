using System.Linq.Expressions;
using Gum.Wireframe;
using System;





#if FRB
using FlatRedBall.Gui;
using FlatRedBall.Forms.Controls;
using FlatRedBall.Forms.Data;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace MonoGameGum.Forms.Controls;
#endif

#if !FRB
using Gum.Forms.Data;
namespace Gum.Forms.Controls;
#endif

#if MONOGAME

using Microsoft.Xna.Framework;

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



    public static void RemoveFromRoot(this FrameworkElement element)
    {
        element.Visual.Parent = null;
        element.Visual.RemoveFromManagers();
    }

    // Vic says: I started adding this but I don't like the implementation because
    // if it's added to something like a StackPanel, it adjusts the size of the StackPanel
    // and it shifts the stacking of all other children. To solve this, the implementation below
    // adds to the popup root. This is problematic because the coloredRectangle only updates its position
    // when it is first added, and it doesn't continually update. This means users may get confused if they
    // call this before making some other UI changes.
    //public static void AddDebugOverlay(this FrameworkElement element, Color? color = null)
    //{
    //    color = color ?? Color.Pink;
        

    //    var coloredRectangle = new ColoredRectangleRuntime();
    //    coloredRectangle.Width = element.Visual.GetAbsoluteWidth();
    //    coloredRectangle.Height = element.Visual.GetAbsoluteHeight();
    //    coloredRectangle.X = element.Visual.AbsoluteLeft;
    //    coloredRectangle.Y = element.Visual.AbsoluteTop;
    //    coloredRectangle.Color = color.Value;
    //    coloredRectangle.Alpha = 128;
    //    FrameworkElement.PopupRoot.AddChild(coloredRectangle);
    //}

#if RAYLIB
    public static void AddChild(this GraphicalUiElement element, FrameworkElement child)
    {
        element.Children.Add(child.Visual);
    }

    public static void RemoveChild(this GraphicalUiElement element, FrameworkElement child)
    {
        element.Children.Remove(child.Visual);
    }
#endif

}