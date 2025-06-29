﻿using System.Linq.Expressions;
using MonoGameGum.Forms.Data;

namespace MonoGameGum.Forms.Controls;

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
}