using Gum.Mvvm;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// This file is NOT linked by FRB — FRB provides its own binding implementation via its own partial class files.

namespace Gum.Wireframe;

public class BindingContextChangedEventArgs : EventArgs
{
    public object? OldBindingContext { get; set; }
    public object? NewBindingContext { get; set; }
}

public partial class GraphicalUiElement
{
    class VmToUiProperty
    {
        public string VmProperty = string.Empty;
        public string UiProperty = string.Empty;

        public Delegate? Delegate;

        public string? ToStringFormat;

        /// <summary>
        /// For property bindings whose <see cref="VmProperty"/> path contains a '.',
        /// an observer that watches the dotted path on the active BindingContext and
        /// raises ValueChanged whenever the leaf value changes.
        /// </summary>
        public PropertyPathObserver? PathObserver;

        /// <summary>
        /// Named handler subscribed to <see cref="PathObserver"/>'s ValueChanged event.
        /// Stored so we can reliably unsubscribe when the binding is replaced or removed
        /// (anonymous lambdas captured at subscription time can't be unsubscribed).
        /// </summary>
        public Action? PathObserverValueChangedHandler;

        // Treat any path containing '.' (multi-segment) or '[' (indexed segment) as a
        // dotted path. Both require the PropertyPathObserver walk; only a bare property
        // name takes the flat-binding fast path.
        public bool IsDottedPath => VmProperty.Contains('.') || VmProperty.Contains('[');

        /// <summary>
        /// Detaches the dotted-path observer (if any) and unsubscribes the stored
        /// ValueChanged handler. Safe to call when no observer is attached.
        /// </summary>
        public void Detach()
        {
            if (PathObserver != null)
            {
                if (PathObserverValueChangedHandler != null)
                {
                    PathObserver.ValueChanged -= PathObserverValueChangedHandler;
                }
                PathObserver.Detach();
            }
            PathObserverValueChangedHandler = null;
        }

        public override string ToString()
        {
            return $"VM:{VmProperty} UI{UiProperty}";
        }
    }

    partial void OnConstructor()
    {
        this.ParentChanged += HandleBindingParentChanged;
    }

    partial void CustomRemoveFromManagers()
    {
        RemoveBindingContextRecursively();
    }

    private void HandleBindingParentChanged(object? sender, ParentChangedEventArgs args)
    {
        if (args.OldValue is GraphicalUiElement old)
        {
            old.BindingContextChanged -= ParentBindingContextChanged;
        }

        GraphicalUiElement? newParent = args.NewValue as GraphicalUiElement;

        if (newParent is not null)
        {
            newParent.BindingContextChanged += ParentBindingContextChanged;
        }

        InheritedBindingContext = newParent?.BindingContext;
    }

    void ParentBindingContextChanged(object? s, BindingContextChangedEventArgs e)
    {
        InheritedBindingContext = EffectiveParentGue?.BindingContext;
    }

    public event EventHandler<BindingContextChangedEventArgs>? InheritedBindingContextChanged;

    private void RemoveBindingContextRecursively()
    {
        this.BindingContext = null;
        if (this.Children != null)
        {
            foreach (var child in this.Children)
            {
                child.RemoveBindingContextRecursively();
            }
        }
        else
        {
            foreach (var gue in this.ContainedElements)
            {
                gue.RemoveBindingContextRecursively();
            }
        }
    }

    Dictionary<string, VmToUiProperty> vmPropsToUiProps = new Dictionary<string, VmToUiProperty>();
    Dictionary<string, VmToUiProperty> vmEventsToUiMethods = new Dictionary<string, VmToUiProperty>();

    object? mInheritedBindingContext;
    internal object? InheritedBindingContext
    {
        get => mInheritedBindingContext;
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
    public object? BindingContext
    {
        get => mBindingContext ?? mInheritedBindingContext;
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

    bool _isSubscribedToViewModelPropertyChanged = false;
    private void HandleBindingContextChangedInternal(object? oldContext, object? newContext)
    {
        if (oldContext is INotifyPropertyChanged oldViewModel)
        {
            UnsubscribeEventsOnOldViewModel(oldViewModel);
        }

        // Re-attach all dotted-path observers to the new context.
        DetachAllPathObservers();

        if (HasFlatPropertyBinding() && newContext is INotifyPropertyChanged viewModel)
        {
            TrySubscribeToViewModelChanges(viewModel);
        }

        if (newContext != null)
        {
            foreach (var vmProperty in vmPropsToUiProps.Keys)
            {
                UpdateToVmProperty(vmProperty);
            }
        }

        foreach (GraphicalUiElement child in GetAllBindableChildren())
        {
            child.InheritedBindingContext = newContext;

            if (child.BindingContextBinding != null)
            {
                child.BindingContextBindingPropertyOwner = newContext;

                child.UpdateToVmProperty(child.BindingContextBinding);
            }
        }

        BindingContextChanged?.Invoke(this, new()
        {
            OldBindingContext = oldContext,
            NewBindingContext = newContext
        });
    }

    private void TrySubscribeToViewModelChanges(INotifyPropertyChanged viewModel)
    {
        if (_isSubscribedToViewModelPropertyChanged == false)
        {
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            _isSubscribedToViewModelPropertyChanged = true;
        }
    }

    public static int PropertyUnsubscribeCallCount = 0;
    public static int GetTypeCallCount = 0;

    private void UnsubscribeEventsOnOldViewModel(INotifyPropertyChanged oldViewModel)
    {
        if (_isSubscribedToViewModelPropertyChanged)
        {
            oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            _isSubscribedToViewModelPropertyChanged = false;
        }

        foreach (var eventItem in vmEventsToUiMethods.Values)
        {
            var delegateToRemove = eventItem.Delegate;

            var foundEvent = oldViewModel.GetType().GetEvent(eventItem.VmProperty);
            GetTypeCallCount++;
            foundEvent?.RemoveEventHandler(oldViewModel, delegateToRemove);
        }
    }

    public object? BindingContextBindingPropertyOwner { get; private set; }
    public string? BindingContextBinding { get; private set; }

    public event Action<object, BindingContextChangedEventArgs> BindingContextChanged;

    object? EffectiveBindingContext => mBindingContext ?? InheritedBindingContext;

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            return;
        }
        // Dotted-path bindings receive notifications via their PropertyPathObserver,
        // not via this root-level handler.
        UpdateToVmProperty(e.PropertyName);
    }

    private bool HasFlatPropertyBinding()
    {
        // Only flat property bindings need a root-level INPC subscription.
        // Dotted-path bindings manage their own subscriptions via PropertyPathObserver,
        // and event bindings use a separate AddEventHandler / RemoveEventHandler path
        // (see vmEventsToUiMethods).
        foreach (var binding in vmPropsToUiProps.Values)
        {
            if (!binding.IsDottedPath)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Detects whether a dotted VM path resolves to an event on the leaf's parent
    /// type. Detecting "is this an event" requires walking the path on a real
    /// BindingContext (we have to know the type of the second-to-last segment
    /// resolves to). When BindingContext is null we can't yet decide, so we defer
    /// the check until <see cref="UpdateToVmProperty"/> runs after a context is
    /// assigned. Returns true if the path was confirmed to target an event and
    /// should throw.
    /// </summary>
    private bool IsDottedPathTargetingEvent(string vmProperty)
    {
        var context = BindingContext;
        if (context == null)
        {
            return false;
        }

        PathSegment[] segments = PathSegmentParser.ParseSegments(vmProperty);
        object? parent = PropertyPathObserver.WalkSegments(context, segments, segments.Length - 1);
        if (parent == null)
        {
            return false;
        }

        var leafName = segments[segments.Length - 1].Name;
        return parent.GetType().GetEvent(leafName) != null;
    }

    private void ThrowIfDottedPathTargetsEvent(string vmProperty)
    {
        if (IsDottedPathTargetingEvent(vmProperty))
        {
            throw new NotSupportedException(
                $"Dotted binding paths are not supported for event bindings. Got '{vmProperty}'.");
        }
    }

    private void DetachAllPathObservers()
    {
        // Detach observers from their current roots without unsubscribing the
        // ValueChanged handler — observers will be re-attached to the new
        // BindingContext shortly. Full handler unsubscription happens only when
        // a binding is overwritten or cleared (see VmToUiProperty.Detach).
        foreach (var binding in vmPropsToUiProps.Values)
        {
            binding.PathObserver?.Detach();
        }
    }

    public void SetBinding(string uiProperty, string vmProperty, string? toStringFormat = null)
    {
        if (uiProperty == nameof(BindingContext))
        {
            BindingContextBinding = vmProperty;

            BindingContextBindingPropertyOwner = (this.Parent as GraphicalUiElement)?.BindingContext;

            UpdateToVmProperty(vmProperty);
        }
        else
        {
            if (vmPropsToUiProps.TryGetValue(vmProperty, out var existingByVm))
            {
                existingByVm.Detach();
                vmPropsToUiProps.Remove(vmProperty);
            }
            // This prevents single UI properties from being bound to multiple VM properties
            if (vmPropsToUiProps.Any(item => item.Value.UiProperty == uiProperty))
            {
                var toRemove = vmPropsToUiProps.Where(item => item.Value.UiProperty == uiProperty).ToArray();

                foreach (var kvp in toRemove)
                {
                    kvp.Value.Detach();
                    vmPropsToUiProps.Remove(kvp.Key);
                }
            }

            var newBinding = new VmToUiProperty();
            newBinding.UiProperty = uiProperty;
            newBinding.VmProperty = vmProperty;
            newBinding.ToStringFormat = toStringFormat;

            if (newBinding.IsDottedPath)
            {
                // Eagerly detect dotted-event-target if BindingContext is already set.
                // If BindingContext is null, the check is deferred to UpdateToVmProperty
                // (which runs once a context becomes available). Either way, dotted-path
                // event bindings are never permitted.
                ThrowIfDottedPathTargetsEvent(vmProperty);
                PropertyPathObserver observer = new(vmProperty);
                newBinding.PathObserver = observer;
                Action handler = () => ApplyVmValueToUi(newBinding, observer.GetCurrentValue());
                newBinding.PathObserverValueChangedHandler = handler;
                observer.ValueChanged += handler;
            }

            vmPropsToUiProps.Add(vmProperty, newBinding);

            if (BindingContext != null)
            {
                UpdateToVmProperty(vmProperty);

                // Flat-path bindings need a root-level INPC subscription. Dotted-path
                // bindings manage their own subscriptions via PropertyPathObserver.
                if (!newBinding.IsDottedPath
                    && _isSubscribedToViewModelPropertyChanged == false
                    && BindingContext is INotifyPropertyChanged notifyPropertyChanged)
                {
                    TrySubscribeToViewModelChanges(notifyPropertyChanged);
                }
            }
        }
    }

    private bool UpdateToVmProperty(string vmPropertyName)
    {
        var updated = false;

        var isBoundToVmProperty = vmPropsToUiProps.ContainsKey(vmPropertyName) ||
            BindingContextBinding == vmPropertyName;

        if (isBoundToVmProperty)
        {
            var bindingContextObjectToUse = BindingContextBinding == vmPropertyName ?
                BindingContextBindingPropertyOwner : BindingContext;

            // Dotted-path property binding: walk the path through PropertyPathObserver.
            if (BindingContextBinding != vmPropertyName
                && vmPropsToUiProps.TryGetValue(vmPropertyName, out var maybeDotted)
                && maybeDotted.IsDottedPath)
            {
                if (bindingContextObjectToUse != null && maybeDotted.PathObserver is { } observer)
                {
                    // Deferred event-target check: when SetBinding ran without a
                    // BindingContext, we couldn't walk the path to confirm the leaf
                    // was an event. Now that we have a context, validate before
                    // attaching the observer.
                    PathSegment[] segs = observer.Segments;
                    object? leafParent = PropertyPathObserver.WalkSegments(
                        bindingContextObjectToUse, segs, segs.Length - 1);
                    if (leafParent != null
                        && leafParent.GetType().GetEvent(segs[segs.Length - 1].Name) != null)
                    {
                        throw new NotSupportedException(
                            $"Dotted binding paths are not supported for event bindings. Got '{vmPropertyName}'.");
                    }

                    if (!ReferenceEquals(observer.CurrentRoot, bindingContextObjectToUse))
                    {
                        observer.Detach();
                        observer.Attach(bindingContextObjectToUse);
                    }
                    ApplyVmValueToUi(maybeDotted, observer.GetCurrentValue());
                    updated = true;
                }
                TryPushBindingContextChangeToChildren(vmPropertyName);
                return updated;
            }

            var bindingContextObjectType = bindingContextObjectToUse?.GetType();

            var vmProperty = bindingContextObjectType?.GetProperty(vmPropertyName);

            FieldInfo? vmField = null;

            if (vmProperty == null)
            {
                vmField = bindingContextObjectType?.GetField(vmPropertyName);
            }

            var foundEvent = bindingContextObjectType?.GetEvent(vmPropertyName);

            if (vmProperty == null && vmField == null && foundEvent == null)
            {
                System.Diagnostics.Debug.WriteLine($"Could not find field, property, or event {vmPropertyName} in {bindingContextObjectToUse?.GetType()}");
            }
            else if (foundEvent != null)
            {
                BindEvent(vmPropertyName, bindingContextObjectToUse, foundEvent);
            }
            else
            {
                var vmValue = vmField != null ? vmField?.GetValue(bindingContextObjectToUse) :
                    vmProperty?.GetValue(bindingContextObjectToUse, null);


                if (vmPropertyName == BindingContextBinding)
                {
                    BindingContext = vmValue;
                }
                else
                {
                    var binding = vmPropsToUiProps[vmPropertyName];
                    ApplyVmValueToUi(binding, vmValue);
                }
                updated = true;
            }
        }

        TryPushBindingContextChangeToChildren(vmPropertyName);

        return updated;
    }

    private void ApplyVmValueToUi(VmToUiProperty binding, object? vmValue)
    {
        var thisType = this.GetType();

        PropertyInfo? uiProperty = thisType.GetProperty(binding.UiProperty);

        if (uiProperty == null)
        {
            var message = $"The type {thisType} does not have a property {binding.UiProperty}. If this property exists, make sure that it's not private.";
            throw new Exception(message);
        }

        var convertedValue = ConvertValue(vmValue, uiProperty.PropertyType, binding.ToStringFormat);

        try
        {
            uiProperty.SetValue(this, convertedValue, null);
        }
        catch
        {
#if FULL_DIAGNOSTICS
            if (convertedValue != null && uiProperty.PropertyType != convertedValue.GetType())
            {
                throw new InvalidCastException(
                    $"Error applying binding: The bound property {convertedValue.GetType()} {binding.VmProperty} with value {convertedValue} " +
                    $"could not be converted to {uiProperty.PropertyType} which is the type of {binding.UiProperty}");
            }
#endif
            throw;
        }
    }

    private void BindEvent(string vmPropertyName, object? bindingContextObjectToUse, EventInfo foundEvent)
    {
        var binding = vmPropsToUiProps[vmPropertyName];

        if (binding.IsDottedPath)
        {
            throw new NotSupportedException(
                $"Dotted binding paths are not supported for event bindings. Got '{vmPropertyName}' for UI member '{binding.UiProperty}'.");
        }

        var isAlreadyBound = vmEventsToUiMethods.ContainsKey(vmPropertyName);

        if (!isAlreadyBound)
        {
            var delegateInstance = Delegate.CreateDelegate(foundEvent.EventHandlerType!, this, binding.UiProperty);

            vmEventsToUiMethods.Add(vmPropertyName, new VmToUiProperty { UiProperty = binding.UiProperty, VmProperty = vmPropertyName, Delegate = delegateInstance });

            foundEvent.AddEventHandler(bindingContextObjectToUse, delegateInstance);
        }

    }

    private void TryPushBindingContextChangeToChildren(string vmPropertyName)
    {
        foreach (GraphicalUiElement descendent in GetAllBindableDescendents())
        {
            if (descendent.BindingContextBinding == vmPropertyName && descendent.BindingContextBindingPropertyOwner == BindingContext)
            {
                descendent.UpdateToVmProperty(vmPropertyName);
            }
        }
    }

    protected void PushValueToViewModel([CallerMemberName] string? uiPropertyName = null)
    {
        if (uiPropertyName == null)
        {
            return;
        }

        VmToUiProperty? matched = null;
        foreach (var binding in vmPropsToUiProps.Values)
        {
            if (binding.UiProperty == uiPropertyName)
            {
                matched = binding;
                break;
            }
        }

        if (matched == null || BindingContext == null)
        {
            return;
        }

        PropertyInfo? uip = GetType().GetProperty(matched.UiProperty);
        if (uip == null)
        {
            return;
        }

        if (matched.IsDottedPath)
        {
            PathSegment[] segments = matched.PathObserver?.Segments
                ?? PathSegmentParser.ParseSegments(matched.VmProperty);

            // TODO: indexed leaf write-back (e.g. "Items[0].Name" or "Items[0]") is a
            // documented non-goal — short-circuit silently rather than attempt a
            // PropertyInfo.SetValue against an indexed leaf.
            if (segments[segments.Length - 1].Index.HasValue)
            {
                return;
            }

            object? parent = PropertyPathObserver.WalkSegments(BindingContext, segments, segments.Length - 1);
            if (parent == null)
            {
                // Intermediate is null — silently no-op.
                return;
            }

            // NOTE: If any intermediate along the path is a value type (struct), the
            // walk above produces a boxed copy. Writing through `leafProperty.SetValue`
            // mutates that copy rather than the original storage location. This is the
            // same limitation as standard reflection-based property setters; correcting
            // it would require generating per-path setter delegates that re-assign each
            // boxed segment back into its parent. For now, two-way binding through
            // struct intermediates is unsupported.
            string leafName = segments[segments.Length - 1].Name;
            PropertyInfo? leafProperty = parent.GetType().GetProperty(leafName);
            if (leafProperty == null)
            {
                return;
            }

            object? uiValue = uip.GetValue(this, null);
            leafProperty.SetValue(parent, uiValue, null);
        }
        else
        {
            if (BindingContext.GetType().GetProperty(matched.VmProperty) is { } vmp)
            {
                object? uiValue = uip.GetValue(this, null);
                vmp.SetValue(BindingContext, uiValue, null);
            }
        }
    }

    private IEnumerable<GraphicalUiElement> GetAllBindableChildren()
    {
        if (Children != null) return Children;
        else return ContainedElements;
    }

    private IEnumerable<GraphicalUiElement> GetAllBindableDescendents()
    {
        foreach (GraphicalUiElement child in GetAllBindableChildren())
        {
            yield return child;

            foreach (GraphicalUiElement subChild in child.GetAllBindableDescendents())
            {
                yield return subChild;
            }
        }
    }

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
