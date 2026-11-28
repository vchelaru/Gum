using System;
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


/// <summary>
/// The bindings of one target (a Forms control, or a visual outside FRB), keyed by the target member name.
/// A target property gets a value binding; a target method gets a view model event binding.
/// </summary>
internal class PropertyRegistry
{
    private IBindingTarget Owner { get; }

    private Dictionary<string, BindingExpressionBase> BindingExpressions { get; } = [];

    private Dictionary<string, Binding> Bindings { get; } = [];

    // What each property held before it was first bound: the value a cleared binding or BindingContext shows.
    private Dictionary<string, object?> UnboundValues { get; } = [];

    public PropertyRegistry(IBindingTarget owner)
    {
        Owner = owner;
    }

    public BindingExpressionBase? GetBindingExpression(string uiPropertyName)
    {
        BindingExpressions.TryGetValue(uiPropertyName, out BindingExpressionBase? expression);
        return expression;
    }

    /// <summary>The bindings set on the owner, by target member name, for replaying them on a copy.</summary>
    public IEnumerable<KeyValuePair<string, Binding>> GetBindings() => Bindings;

    [RequiresUnreferencedCode("Calls " + nameof(NpcBindingExpression) + "." + nameof(NpcBindingExpression.Start) + ".")]
    public void SetBinding(string uiPropertyName, Binding binding)
    {
        Type ownerType = Owner.GetType();
        if (ownerType.GetProperty(uiPropertyName) != null)
        {
            NpcBindingExpression npcExpression = new(Owner, uiPropertyName, binding);
            // Refused before Replace, so the property's previous binding keeps working.
            if (npcExpression.GetStringFormatSourceTypeErrorMessage() is { } stringFormatMessage)
            {
                npcExpression.Dispose();
                throw new InvalidOperationException(stringFormatMessage);
            }
            // Captured once, the first time the property is bound, so a rebind does not take a view model's value
            // as the value to reset to.
            if (UnboundValues.TryGetValue(uiPropertyName, out object? unboundValue))
            {
                npcExpression.UnboundTargetValue = unboundValue;
            }
            else
            {
                UnboundValues[uiPropertyName] = npcExpression.UnboundTargetValue;
            }
            Replace(uiPropertyName, npcExpression, binding);
            npcExpression.Start();
        }
        else if (HasInstanceMethod(ownerType, uiPropertyName))
        {
            MethodBindingExpression methodExpression = new(Owner, uiPropertyName, binding.Path);
            Replace(uiPropertyName, methodExpression, binding);
            methodExpression.Start();
        }
        else
        {
            throw new InvalidOperationException(
                $"{ownerType.Name} has no public property and no method named '{uiPropertyName}', so it cannot be " +
                "the target of a binding. Bind a public property, or a method for a view model event.");
        }
    }

    private void Replace(string uiPropertyName, BindingExpressionBase expression, Binding binding)
    {
        if (BindingExpressions.TryGetValue(uiPropertyName, out BindingExpressionBase? existing))
        {
            existing.Dispose();
        }
        BindingExpressions[uiPropertyName] = expression;
        Bindings[uiPropertyName] = binding;
    }

    // Accepts any instance method in the hierarchy, of any visibility, so no method that
    // Delegate.CreateDelegate(type, target, name) can bind is refused.
    [RequiresUnreferencedCode("Looks up methods on the target's type by name.")]
    private static bool HasInstanceMethod(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;
        for (Type? current = type; current != null; current = current.BaseType)
        {
            foreach (MethodInfo method in current.GetMethods(flags))
            {
                if (method.Name == name)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Writes the owner's <paramref name="uiPropertyName"/> to its binding's source, if it is bound.</summary>
    public void PushTargetToSource(string uiPropertyName)
    {
        if (BindingExpressions.TryGetValue(uiPropertyName, out BindingExpressionBase? expression) &&
            expression is NpcBindingExpression npcExpression)
        {
            npcExpression.PushTargetToSource();
        }
    }

    /// <summary>
    /// Re-evaluates every binding's target from its source, for when the control's Visual is replaced and
    /// the new Visual's own values sit under the bound properties. Never writes to a source.
    /// </summary>
    public void RefreshTargets()
    {
        if (BindingExpressions.Count == 0)
        {
            return;
        }

        // A target setter can run user code that adds or clears bindings.
        List<BindingExpressionBase> expressions = new(BindingExpressions.Values);
        foreach (BindingExpressionBase expression in expressions)
        {
            if (expression is NpcBindingExpression npcExpression)
            {
                npcExpression.RefreshTarget();
            }
        }
    }

    /// <summary>
    /// Pauses or resumes every binding as the owner's subtree leaves or rejoins the root. A paused binding
    /// stops listening to its source and leaves the target as it is; resuming reattaches it to the owner's
    /// current context and refreshes the target.
    /// </summary>
    [RequiresUnreferencedCode("Calls " + nameof(BindingExpressionBase.Resume) + ".")]
    public void SetPaused(bool isPaused)
    {
        if (BindingExpressions.Count == 0)
        {
            return;
        }

        // A target setter can run user code that adds or clears bindings.
        List<BindingExpressionBase> expressions = new(BindingExpressions.Values);
        foreach (BindingExpressionBase expression in expressions)
        {
            if (isPaused)
            {
                expression.Pause();
            }
            else
            {
                expression.Resume();
            }
        }
    }

    public void ClearBinding(string uiPropertyName)
    {
        if (!BindingExpressions.TryGetValue(uiPropertyName, out BindingExpressionBase? expression))
        {
            return;
        }

        expression.Dispose();
        // Removed before the restore, so a setter that pushes or commits its binding finds none and leaves the
        // view model alone.
        BindingExpressions.Remove(uiPropertyName);
        Bindings.Remove(uiPropertyName);
        // Only a OneWayToSource binding can target a property with no setter, and it never wrote it.
        if (expression is NpcBindingExpression { HasTargetSetter: true } npcExpression)
        {
            npcExpression.GetTargetProperty().SetValue(Owner, npcExpression.UnboundTargetValue);
        }
    }
}
