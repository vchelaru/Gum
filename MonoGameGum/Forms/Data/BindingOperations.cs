﻿
#if FRB

using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Data;

#else
using MonoGameGum.Forms.Controls;
namespace MonoGameGum.Forms.Data;

#endif


public static class BindingOperations
{
    public static BindingExpressionBase? GetBindingExpression(FrameworkElement element, string uiPropertyName)
        => element.PropertyRegistry.GetBindingExpression(uiPropertyName);

    public static void ClearBinding(FrameworkElement element, string uiPropertyName)
        => element.PropertyRegistry.ClearBinding(uiPropertyName);
}