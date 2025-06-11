﻿using System.Collections.Generic;
using System.Reflection;


#if FRB
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
using RaylibGum.Forms.Data;
using RaylibGum.Forms.Controls;

#else
using MonoGameGum.Forms.Controls;
namespace MonoGameGum.Forms.Data;
#endif

internal class PropertyRegistry
{
    private FrameworkElement Owner { get; }

    private Dictionary<string, NpcBindingExpression> NpcBindingExpressions { get; } = [];

    public PropertyRegistry(FrameworkElement frameworkElement)
    {
        Owner = frameworkElement;
    }

    public BindingExpressionBase? GetBindingExpression(string uiPropertyName)
    {
        NpcBindingExpressions.TryGetValue(uiPropertyName, out NpcBindingExpression? npcExpression);
        return npcExpression;
    }

    public void SetBinding(string uiPropertyName, Binding binding)
    {
        NpcBindingExpression npcExpression = new (Owner, uiPropertyName, binding);
        if (NpcBindingExpressions.TryGetValue(uiPropertyName, out NpcBindingExpression? existing))
        {
            existing.Dispose();
        }
        NpcBindingExpressions[uiPropertyName] = npcExpression;
        npcExpression.Start();
    }

    public void ClearBinding(string uiPropertyName)
    {
        if (NpcBindingExpressions.TryGetValue(uiPropertyName, out NpcBindingExpression? npcExpression))
        {
            PropertyInfo targetProperty = npcExpression.GetTargetProperty();
            npcExpression.Dispose();
            targetProperty.SetValue(Owner, null);
            NpcBindingExpressions.Remove(uiPropertyName);
        }
    }
    
}