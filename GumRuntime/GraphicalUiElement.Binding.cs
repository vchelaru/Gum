using Gum.Forms.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

// This file is NOT linked by FRB — FRB provides its own binding implementation via its own partial class files.

namespace Gum.Wireframe;

public class BindingContextChangedEventArgs : EventArgs
{
    public object? OldBindingContext { get; set; }
    public object? NewBindingContext { get; set; }
}

/// <summary>
/// BindingContext inheritance and SetBinding for visuals. Bindings run on the same engine as Forms controls
/// (<see cref="PropertyRegistry"/>), with this element as the target.
/// </summary>
public partial class GraphicalUiElement : IBindingTarget
{
    // Created by the first SetBinding, so an element that is never bound allocates nothing for binding.
    PropertyRegistry? _propertyRegistry;

    // The IBindingTarget form of BindingContextChanged, which the binding engine subscribes to.
    EventHandler<BindingContextChangedEventArgs>? _bindingTargetContextChanged;

    event EventHandler<BindingContextChangedEventArgs>? IBindingTarget.BindingContextChanged
    {
        add => _bindingTargetContextChanged += value;
        remove => _bindingTargetContextChanged -= value;
    }

    // A visual has no focus of its own, so a LostFocus trigger never fires on it.
    event EventHandler? IBindingTarget.LostFocus
    {
        add { }
        remove { }
    }

    // States, SetProperty and localization change a visual's properties, so a binding that does not set its mode
    // never writes them to the view model; only PushValueToViewModel does.
    BindingMode IBindingTarget.DefaultBindingMode => BindingMode.OneWay;

    partial void OnConstructor()
    {
        this.ParentChanged += HandleBindingParentChanged;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Replays bindings the source already registered, so the trim risk was surfaced " +
            "at the source's SetBinding call.")]
    partial void ResetBindingStateForClone()
    {
        // MemberwiseClone shares the registry, whose expressions apply values to the source, so the clone gets
        // its own and the source's bindings are replayed on it. The clone has no parent to inherit a context
        // from and does not copy an explicit one; whoever places the clone supplies its context.
        PropertyRegistry? sourceRegistry = _propertyRegistry;

        _propertyRegistry = null;
        _bindingTargetContextChanged = null;
        mBindingContext = null;
        mInheritedBindingContext = null;
        BindingContextChanged = null;
        InheritedBindingContextChanged = null;

        // The clone has no BindingContext yet, so SetBinding only registers the binding and writes no value;
        // nothing here can reach a renderable that a runtime's Clone override has yet to reset.
        if (sourceRegistry != null)
        {
            foreach (KeyValuePair<string, Binding> binding in sourceRegistry.GetBindings())
            {
                SetBinding(binding.Key, binding.Value);
            }
        }
    }

    // Only the element RemoveFromManagers was called on loses its own context, matching leaving the
    // root; contained children keep theirs.
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic teardown clearing BindingContext when this element is removed from " +
            "managers. The trim risk this unwinds was already surfaced wherever BindingContext/SetBinding " +
            "was actually called by the app to set up the binding in the first place.")]
    partial void CustomRemoveFromManagers()
    {
        if (!IsRemovingFromManagersWithContainer)
        {
            BindingContext = null;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic parent-to-child BindingContext propagation, always wired regardless " +
            "of whether binding is used. The trim risk is already surfaced wherever an ancestor's " +
            "BindingContext/SetBinding was actually called by the app.")]
    private void HandleBindingParentChanged(object? sender, ParentChangedEventArgs args)
    {
        if (args.OldValue is GraphicalUiElement old)
        {
            old.BindingContextChanged -= ParentBindingContextChanged;

            if (old.ClearsBindingContextOfRemovedChildren)
            {
                BindingContext = null;
            }
        }

        GraphicalUiElement? newParent = args.NewValue as GraphicalUiElement;

        if (newParent is not null)
        {
            newParent.BindingContextChanged += ParentBindingContextChanged;
        }

        InheritedBindingContext = newParent?.BindingContext;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic parent-to-child BindingContext propagation, always wired regardless " +
            "of whether binding is used. The trim risk is already surfaced wherever an ancestor's " +
            "BindingContext/SetBinding was actually called by the app.")]
    void ParentBindingContextChanged(object? s, BindingContextChangedEventArgs e)
    {
        InheritedBindingContext = EffectiveParentGue?.BindingContext;
    }

    public event EventHandler<BindingContextChangedEventArgs>? InheritedBindingContextChanged;

    /// <summary>
    /// Whether a child that leaves this element, by any path, has its own explicitly set BindingContext
    /// set to null. Descendants that inherited it lose it with it; descendants given their own keep
    /// theirs. The GumService sets this on its Root; popup and modal roots leave it off.
    /// </summary>
    internal bool ClearsBindingContextOfRemovedChildren { get; set; }

    object? mInheritedBindingContext;
    internal object? InheritedBindingContext
    {
        get => mInheritedBindingContext;
        [RequiresUnreferencedCode(
            "Propagates a new BindingContext down from a parent, which re-resolves every existing " +
            "binding's VM member by name on the new context's type. Those members may be removed " +
            "under PublishTrimmed if nothing else in the app references them.")]
        set
        {
            var oldInherited = mInheritedBindingContext;

            if (oldInherited != value)
            {
                var oldContext = BindingContext;
                mInheritedBindingContext = value;

                InheritedBindingContextChanged?.Invoke(this, new BindingContextChangedEventArgs
                {
                    OldBindingContext = oldInherited,
                    NewBindingContext = mInheritedBindingContext
                });

                if (oldContext != BindingContext)
                {
                    HandleBindingContextChangedInternal(oldContext, BindingContext);
                }
            }
        }
    }

    object? mBindingContext;
    /// <summary>
    /// The object this element's bindings read from and write to. Children inherit it unless they
    /// are given their own. Leaving the root, by any path (<see cref="RemoveFromRoot"/>, removing
    /// from or clearing the root's <see cref="Children"/>, or assigning another
    /// <see cref="Parent"/>), sets this element's own BindingContext to null, so children that inherit
    /// it lose it too; a child given its own keeps it. Moving between other parents keeps it.
    /// </summary>
    public object? BindingContext
    {
        get => mBindingContext ?? mInheritedBindingContext;
        [RequiresUnreferencedCode(
            "Assigning a new BindingContext re-resolves every existing binding's VM member by name " +
            "on the new context's type. Those members may be removed under PublishTrimmed if nothing " +
            "else in the app references them.")]
        [UnconditionalSuppressMessage("Trimming", "IL2112",
            Justification = "The DynamicallyAccessedMembers annotation on GraphicalUiElement preserves " +
                "this setter without calling it. The trim risk is surfaced at the caller instead.")]
        set
        {
            var oldEffectiveBindingContext = BindingContext;
            mBindingContext = value;

            if (oldEffectiveBindingContext != BindingContext)
            {
                HandleBindingContextChangedInternal(oldEffectiveBindingContext, BindingContext);
            }
        }
    }

    [RequiresUnreferencedCode(
        "Each binding re-resolves its VM member by name on the new context's own type. Those members may " +
        "be removed under PublishTrimmed if nothing else in the app references them.")]
    private void HandleBindingContextChangedInternal(object? oldContext, object? newContext)
    {
        BindingContextChangedEventArgs args = new()
        {
            OldBindingContext = oldContext,
            NewBindingContext = newContext
        };

        // This element's own bindings follow the new context before its children inherit it.
        _bindingTargetContextChanged?.Invoke(this, args);

        foreach (GraphicalUiElement child in GetAllBindableChildren())
        {
            child.InheritedBindingContext = newContext;
        }

        BindingContextChanged?.Invoke(this, args);
    }

    /// <summary>
    /// The object a binding of <see cref="BindingContext"/> (see <see cref="BindingContextBinding"/>) reads its
    /// path from: the context this element inherits from its parent. Null when BindingContext is not bound.
    /// </summary>
    public object? BindingContextBindingPropertyOwner => BindingContextBinding == null ? null : mInheritedBindingContext;

    /// <summary>
    /// The path <see cref="BindingContext"/> is bound to, resolved against the parent's BindingContext, or null
    /// when BindingContext is not bound.
    /// </summary>
    public string? BindingContextBinding
    {
        get
        {
            if (_propertyRegistry != null)
            {
                foreach (KeyValuePair<string, Binding> binding in _propertyRegistry.GetBindings())
                {
                    if (binding.Key == nameof(BindingContext))
                    {
                        return binding.Value.Path;
                    }
                }
            }
            return null;
        }
    }

    public event Action<object, BindingContextChangedEventArgs>? BindingContextChanged;

    /// <summary>
    /// Returns whether <paramref name="uiProperty"/> on this element has a binding.
    /// </summary>
    internal bool IsDataBound(string uiProperty) =>
        _propertyRegistry?.GetBindingExpression(uiProperty) != null;

    /// <summary>
    /// Binds <paramref name="uiProperty"/> to the VM member named by <paramref name="vmProperty"/>,
    /// resolved by reflection at bind time. Under <c>PublishTrimmed</c>, that member survives only if
    /// something else in the app references it directly -- prefer the
    /// <see cref="SetBinding{TVm}(string, Expression{Func{TVm, object}}, string?)"/> overload, whose
    /// expression argument keeps the member from being trimmed.
    /// </summary>
    /// <param name="uiProperty">The property on this element to set, or a method to call when the view
    /// model event named by <paramref name="vmProperty"/> is raised.</param>
    /// <param name="vmProperty">The path of the view model member.</param>
    /// <param name="toStringFormat">A .NET format specifier such as "N0", applied when the target is a
    /// string. Equivalent to a <see cref="Binding.StringFormat"/> of "{0:N0}".</param>
    [RequiresUnreferencedCode(
        "vmProperty is resolved by name against the BindingContext's own type. That member may be " +
        "removed under PublishTrimmed if nothing else in the app references it. Prefer the " +
        "SetBinding<TVm> overload that takes an expression instead of a string.")]
    public void SetBinding(string uiProperty, string vmProperty, string? toStringFormat = null)
    {
        SetBinding(uiProperty, new Binding(vmProperty)
        {
            StringFormat = string.IsNullOrEmpty(toStringFormat) ? null : "{0:" + toStringFormat + "}"
        });
    }

    /// <summary>
    /// Binds <paramref name="uiProperty"/> using <paramref name="binding"/>, with every option a Forms control's
    /// SetBinding takes (mode, converter, StringFormat, UpdateSourceTrigger, FallbackValue, TargetNullValue).
    /// <paramref name="uiProperty"/> may also name a method, called when the view model event at
    /// <see cref="Binding.Path"/> is raised. Binding <see cref="BindingContext"/> resolves the path against the
    /// parent's BindingContext. The path is resolved by reflection at bind time; under <c>PublishTrimmed</c>, the
    /// bound VM member survives only if something else in the app references it directly.
    /// </summary>
    [RequiresUnreferencedCode(
        "binding.Path is resolved by name against the BindingContext's own type. That member may be " +
        "removed under PublishTrimmed if nothing else in the app references it. Prefer the " +
        "SetBinding<TVm> overload that takes an expression instead of a string.")]
    public void SetBinding(string uiProperty, Binding binding)
    {
        _propertyRegistry ??= new PropertyRegistry(this);
        _propertyRegistry.SetBinding(uiProperty, binding);
    }

    /// <summary>
    /// Binds <paramref name="uiProperty"/> to the VM member referenced by <paramref name="vmPropertyExpression"/>.
    /// Prefer this over the string-path overload: the expression is a compile-time reference to the VM
    /// member, which keeps that member from being removed under <c>PublishTrimmed</c>. See
    /// <see cref="SetBinding(string, string, string?)"/> for the trim-safety caveat this avoids.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "vmPropertyExpression is a compiler-built expression tree that references the " +
            "VM member directly, which keeps the trimmer from removing it. The string-path overload this " +
            "forwards to can therefore always resolve that member by name at runtime.")]
    public void SetBinding<TVm>(string uiProperty, Expression<Func<TVm, object?>> vmPropertyExpression, string? toStringFormat = null) =>
        SetBinding(uiProperty, BinderHelpers.ExtractPath(vmPropertyExpression), toStringFormat);

    /// <summary>
    /// Writes this element's <paramref name="uiPropertyName"/> to the view model member it is bound to. Call it
    /// from a property setter on a derived visual whose value the user changes. A binding whose mode is
    /// <see cref="BindingMode.OneWay"/> ignores it.
    /// </summary>
    protected void PushValueToViewModel([CallerMemberName] string? uiPropertyName = null)
    {
        if (uiPropertyName == null)
        {
            return;
        }
        _propertyRegistry?.PushTargetToSource(uiPropertyName);
    }

    private IEnumerable<GraphicalUiElement> GetAllBindableChildren()
    {
        if (Children != null) return Children;
        else return ContainedElements;
    }

    // Obsoleted 2026-11, keep until at least 2027-05.
    /// <summary>No longer updated: bindings on visuals run on the Forms engine. Always 0.</summary>
    [Obsolete("No longer updated since November 2026, when visual bindings moved to the Forms binding engine. Always 0.")]
    public static int PropertyUnsubscribeCallCount = 0;

    // Obsoleted 2026-11, keep until at least 2027-05.
    /// <summary>No longer updated: bindings on visuals run on the Forms engine. Always 0.</summary>
    [Obsolete("No longer updated since November 2026, when visual bindings moved to the Forms binding engine. Always 0.")]
    public static int GetTypeCallCount = 0;

    // Obsoleted 2026-11, keep until at least 2027-05.
    /// <summary>
    /// The conversion visual bindings used before November 2026. Bindings no longer call it; they convert the way
    /// Forms bindings do.
    /// </summary>
    [Obsolete("Bindings no longer use this since November 2026; they convert the way Forms bindings do. " +
        "Use a Binding Converter for custom conversion.")]
    public static object? ConvertValue(object? value, Type desiredType, string? format)
    {
        object? convertedValue = value;
        if (desiredType == typeof(string))
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (value is int asInt) convertedValue = asInt.ToString(format);
                else if (value is double asDouble) convertedValue = asDouble.ToString(format);
                else if (value is decimal asDecimal) convertedValue = asDecimal.ToString(format);
                else if (value is float asFloat) convertedValue = asFloat.ToString(format);
                else if (value is long asLong) convertedValue = asLong.ToString(format);
                else if (value is byte asByte) convertedValue = asByte.ToString(format);
            }
            else
            {
                convertedValue = value?.ToString();
            }
        }
        else if (desiredType == typeof(int))
        {
            if (value is decimal asDecimal)
            {
                convertedValue = (int)asDecimal;
            }
            else if (value is string asString)
            {
                if (int.TryParse(asString, out int asInt))
                {
                    convertedValue = asInt;
                }
            }
        }
        else if (desiredType == typeof(double))
        {
            if (value is int asInt)
            {
                convertedValue = (double)asInt;
            }
            else if (value is decimal asDecimal)
            {
                convertedValue = (double)asDecimal;
            }
            else if (value is float asFloat)
            {
                convertedValue = (double)asFloat;
            }
            else if (value is string asString && double.TryParse(asString, out double doubleResult))
            {
                convertedValue = doubleResult;
            }
        }
        else if (desiredType == typeof(decimal))
        {
            if (value is int asInt)
            {
                convertedValue = (decimal)asInt;
            }
            else if (value is double asDouble)
            {
                convertedValue = (decimal)asDouble;
            }
            else if (value is float asFloat)
            {
                convertedValue = (decimal)asFloat;
            }
            else if (value is string asString && decimal.TryParse(asString, out decimal asDecimal))
            {
                convertedValue = asDecimal;
            }
        }
        else if (desiredType == typeof(float))
        {
            if (value is int asInt)
            {
                convertedValue = (float)asInt;
            }
            else if (value is double asDouble)
            {
                convertedValue = (float)asDouble;
            }
            else if (value is decimal asDecimal)
            {
                convertedValue = (float)asDecimal;
            }
            else if (value is string asString)
            {
                convertedValue = float.TryParse(asString, out float result) ? result : 0;
            }
        }
        else if (desiredType == typeof(byte))
        {
            decimal numeric = 0;
            bool isNumeric = false;
            if (value is int asInt)
            {
                numeric = (decimal)asInt;
                isNumeric = true;
            }
            else if (value is double asDouble)
            {
                numeric = (decimal)asDouble;
                isNumeric = true;
            }
            else if (value is decimal asDecimal)
            {
                numeric = asDecimal;
                isNumeric = true;
            }
            else if (value is float asFloat)
            {
                numeric = (decimal)asFloat;
                isNumeric = true;
            }
            else if (value is string asString && byte.TryParse(asString, out byte asByte))
            {
                numeric = (decimal)asByte;
                isNumeric = true;
            }

            if (isNumeric)
            {
                var clamped = Math.Min(numeric, 255);
                clamped = Math.Max(clamped, 0);
                convertedValue = (byte)Math.Round(clamped);
            }
        }
        return convertedValue;
    }
}
