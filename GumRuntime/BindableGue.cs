using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using System.Collections;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gum.Wireframe;

public class BindingContextChangedEventArgs : EventArgs
{
    public object? OldBindingContext { get; set; }
    public object? NewBindingContext { get; set; }
}

/// <summary>
/// The base object for all Gum runtime objects. It contains functionality for
/// setting variables, states, and performing layout. The GraphicalUiElement can
/// wrap an underlying rendering object.
/// </summary>
public class BindableGue : GraphicalUiElement
{
    struct VmToUiProperty
    {
        public string VmProperty;
        public string UiProperty;

        public Delegate Delegate;

        public string ToStringFormat;

        public override string ToString()
        {
            return $"VM:{VmProperty} UI{UiProperty}";
        }

        public static VmToUiProperty Unassigned => new VmToUiProperty();
    }

    public BindableGue()
    {
        InitializeBindableGue();
    }

    public BindableGue(IRenderable renderable) : base(renderable)
    {
        InitializeBindableGue();
    }

    private void InitializeBindableGue()
    {
        this.ParentChanged += HandleParentChanged;
    }

    private void HandleParentChanged(object? sender, ParentChangedEventArgs args)
    {
        if (args.OldValue is BindableGue old)
        {
            old.BindingContextChanged -= ParentBindingContextChanged;
        }

        BindableGue? newParent = args.NewValue as BindableGue;

        if (newParent is not null)
        {
            newParent.BindingContextChanged += ParentBindingContextChanged;
        }

        InheritedBindingContext = newParent?.BindingContext;

    }
    void ParentBindingContextChanged(object? s, BindingContextChangedEventArgs e)
    {
        InheritedBindingContext = (EffectiveParentGue as BindableGue)?.BindingContext;
    }

    public event EventHandler<BindingContextChangedEventArgs>? InheritedBindingContextChanged;

    public override void RemoveFromManagers()
    {
        base.RemoveFromManagers();

        BindingContext = null;
    }

    #region Binding
    // Apr 19 2020:
    // Vic says I could
    // put this in GraphicalUiElement
    // class or in the .IWindow partial.
    // I don't know if I want this in all
    // Gum implementations yet, so I'm going
    // to put it here for now. I may eventually
    // migrate this to the common Gum code but we'll
    // see
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

    private void HandleBindingContextChangedInternal(object? oldContext, object? newContext)
    {
        if (oldContext == newContext)
        {

        }
        if (oldContext is INotifyPropertyChanged oldViewModel)
        {
            UnsubscribeEventsOnOldViewModel(oldViewModel);
        }
        if (newContext is INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        if (newContext != null)
        {
            foreach (var vmProperty in vmPropsToUiProps.Keys)
            {
                UpdateToVmProperty(vmProperty);
            }
        }

        foreach (BindableGue child in GetAllBindableChildren())
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

    private void UnsubscribeEventsOnOldViewModel(INotifyPropertyChanged oldViewModel)
    {
        oldViewModel.PropertyChanged -= HandleViewModelPropertyChanged;

        foreach (var eventItem in vmEventsToUiMethods.Values)
        {
            var delegateToRemove = eventItem.Delegate;

            var foundEvent = oldViewModel.GetType().GetEvent(eventItem.VmProperty);

            foundEvent?.RemoveEventHandler(oldViewModel, delegateToRemove);
        }
    }

    public object? BindingContextBindingPropertyOwner { get; private set; }
    public string? BindingContextBinding { get; private set; }

    public event Action<object, BindingContextChangedEventArgs> BindingContextChanged;

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            return;
        }
        UpdateToVmProperty(e.PropertyName);
    }

    public void SetBinding(string uiProperty, string vmProperty, string toStringFormat = null)
    {
        if (uiProperty == nameof(BindingContext))
        {
            BindingContextBinding = vmProperty;

            BindingContextBindingPropertyOwner = (this.Parent as BindableGue)?.BindingContext;

            UpdateToVmProperty(vmProperty);
        }
        else
        {
            if (vmPropsToUiProps.ContainsKey(vmProperty))
            {
                vmPropsToUiProps.Remove(vmProperty);
            }
            // This prevents single UI properties from being bound to multiple VM properties
            if (vmPropsToUiProps.Any(item => item.Value.UiProperty == uiProperty))
            {
                var toRemove = vmPropsToUiProps.Where(item => item.Value.UiProperty == uiProperty).ToArray();

                foreach (var kvp in toRemove)
                {
                    vmPropsToUiProps.Remove(kvp.Key);
                }
            }

            var newBinding = new VmToUiProperty();
            newBinding.UiProperty = uiProperty;
            newBinding.VmProperty = vmProperty;
            newBinding.ToStringFormat = toStringFormat;

            vmPropsToUiProps.Add(vmProperty, newBinding);

            if (BindingContext != null)
            {
                UpdateToVmProperty(vmProperty);
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

            var bindingContextObjectType = bindingContextObjectToUse?.GetType();

            var vmProperty = bindingContextObjectType?.GetProperty(vmPropertyName);

            FieldInfo vmField = null;

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
                var vmValue = vmField != null ? vmField.GetValue(bindingContextObjectToUse) :
                    vmProperty.GetValue(bindingContextObjectToUse, null);


                if (vmPropertyName == BindingContextBinding)
                {
                    BindingContext = vmValue;
                }
                else
                {
                    var binding = vmPropsToUiProps[vmPropertyName];
                    PropertyInfo? uiProperty = this.GetType().GetProperty(binding.UiProperty);

                    if (uiProperty == null)
                    {
                        throw new Exception($"The type {this.GetType()} does not have a property {vmPropsToUiProps[vmPropertyName]}");
                    }

                    var convertedValue = ConvertValue(vmValue, uiProperty.PropertyType, binding.ToStringFormat);

                    try
                    {
                        uiProperty.SetValue(this, convertedValue, null);
                    }
                    catch
                    {
#if DEBUG
                        if (convertedValue != null && uiProperty.PropertyType != convertedValue.GetType())
                        {
                            throw new InvalidCastException(
                                $"Error applying binding: The bound property {convertedValue.GetType()} {vmPropertyName} with value {convertedValue} " +
                                $"could not be converted to {uiProperty.PropertyType} which is the type of {binding.UiProperty}");
                        }
#endif
                        throw;
                    }
                }
                updated = true;
            }
        }

        TryPushBindingContextChangeToChildren(vmPropertyName);

        return updated;
    }

    private void BindEvent(string vmPropertyName, object bindingContextObjectToUse, EventInfo foundEvent)
    {
        var binding = vmPropsToUiProps[vmPropertyName];

        var isAlreadyBound = vmEventsToUiMethods.ContainsKey(vmPropertyName);

        if (!isAlreadyBound)
        {
            var delegateInstance = Delegate.CreateDelegate(foundEvent.EventHandlerType, this, binding.UiProperty);

            vmEventsToUiMethods.Add(vmPropertyName, new VmToUiProperty { UiProperty = binding.UiProperty, VmProperty = vmPropertyName, Delegate = delegateInstance });

            foundEvent.AddEventHandler(bindingContextObjectToUse, delegateInstance);
        }

    }

    public static object ConvertValue(object value, Type desiredType, string format)
    {
        object convertedValue = value;
        if (desiredType == typeof(string))
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (value is int asInt) convertedValue = asInt.ToString(format);
                else if (value is double asDouble) convertedValue = asDouble.ToString(format);
                else if (value is decimal asDecimal) convertedValue = asDecimal.ToString(format);
                else if (value is float asFloat) convertedValue = asFloat.ToString(format);
                else if (value is long asLong) convertedValue = asLong.ToString(format);
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
                // do we round? 
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
        return convertedValue;
    }


    private void TryPushBindingContextChangeToChildren(string vmPropertyName)
    {
        foreach (BindableGue descendant in GetAllBindableDescendents())
        {
            if (descendant.BindingContextBinding == vmPropertyName && descendant.BindingContextBindingPropertyOwner == BindingContext)
            {
                descendant.UpdateToVmProperty(vmPropertyName);
            }
        }
    }

    protected void PushValueToViewModel([CallerMemberName] string uiPropertyName = null)
    {
        if (uiPropertyName != null && 
            vmPropsToUiProps.TryGetValue(uiPropertyName, out VmToUiProperty kvp) && 
            kvp.UiProperty == uiPropertyName &&
            BindingContext?.GetType().GetProperty(kvp.VmProperty) is {} vmp &&
            GetType().GetProperty(kvp.UiProperty) is {} uip)
        {
            object? uiValue = uip.GetValue(this, null);
            vmp.SetValue(BindingContext, uiValue, null);
        }
    }

    #endregion

    public IEnumerable<BindableGue> GetAllBindableChildren() =>
    [
        ..Children?.OfType<BindableGue>() ?? [], ..ContainedElements.OfType<BindableGue>()
    ];

    public IEnumerable<BindableGue> GetAllBindableDescendents()
    {
        foreach (BindableGue child in GetAllBindableChildren())
        {
            yield return child;

            foreach (BindableGue subChild in child.GetAllBindableDescendents())
            {
                yield return subChild;
            }
        }
    }
}