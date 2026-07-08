using System.Linq.Expressions;
using Gum.Wireframe;
using RenderingLibrary;
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



    /// <summary>
    /// Adds this Forms control's underlying visual to the active runtime's root container,
    /// making it a top-level element. Resolves the root via <see cref="IGumService.Default"/>
    /// so this single implementation works on every runtime (MonoGame/KNI/FNA, Raylib,
    /// Skia, Sokol — all wire the default during Initialize).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no <see cref="IGumService"/> has been initialized.
    /// </exception>
    public static void AddToRoot(this FrameworkElement element)
    {
#if !FRB
        if (IGumService.Default?.IsInitialized != true)
        {
            throw new InvalidOperationException(
                "Cannot call AddToRoot because IGumService.Default is not initialized — " +
                "did you remember to initialize Gum first (GumService.Default.Initialize)?");
        }
        IGumService.Default.Root.Children.Add(element.Visual);
#endif
    }

    public static void RemoveFromRoot(this FrameworkElement element)
    {
        // suppress layouts when removing from root, this improves performance
        var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;
        if(!wasSuspended)
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
        }
        element.Visual.Parent = null;
        element.Visual.RemoveFromManagers();
        if (!wasSuspended)
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
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

    public static void AddChild(this GraphicalUiElement element, FrameworkElement child)
    {
        element.Children.Add(child.Visual);
    }

    public static void RemoveChild(this GraphicalUiElement element, FrameworkElement child)
    {
        element.Children.Remove(child.Visual);
    }

}