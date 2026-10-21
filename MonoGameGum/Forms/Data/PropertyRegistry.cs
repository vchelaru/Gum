using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;


#if FRB
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
using Gum.Forms.Data;
using Gum.Forms.Controls;
namespace Gum.Forms.Data;

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

    [RequiresUnreferencedCode("Calls " + nameof(NpcBindingExpression) + "." + nameof(NpcBindingExpression.Start) + ".")]
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

    /// <summary>
    /// Re-evaluates every binding's target from its source, for when the control's Visual is replaced and
    /// the new Visual's own values sit under the bound properties. Never writes to a source.
    /// </summary>
    public void RefreshTargets()
    {
        if (NpcBindingExpressions.Count == 0)
        {
            return;
        }

        // A target setter can run user code that adds or clears bindings.
        List<NpcBindingExpression> expressions = new (NpcBindingExpressions.Values);
        foreach (NpcBindingExpression expression in expressions)
        {
            expression.RefreshTarget();
        }
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