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
    public object OldBindingContext { get; set; }
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

    private void HandleParentChanged(object sender, EventArgs e)
    {
        var parent = this.EffectiveParentGue as BindableGue;

        var newInherited = parent?.BindingContext;

        if (mBindingContext == null && mInheritedBindingContext != newInherited)
        {
            InheritedBindingContext = newInherited;
        }
    }

    public override void RemoveFromManagers()
    {
        base.RemoveFromManagers();

        RemoveBindingContextRecursively();
    }

    private void RemoveBindingContextRecursively()
    {
        this.BindingContext = null;
        if (this.Children != null)
        {
            foreach (var child in this.Children)
            {
                if (child is BindableGue gue)
                {
                    gue.RemoveBindingContextRecursively();
                }
            }
        }
        else
        {
            foreach (var gue in this.WhatThisContains)
            {
                if(gue is BindableGue bindableGue)
                {
                    bindableGue.RemoveBindingContextRecursively();
                }
            }
        }
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

    object mInheritedBindingContext;
    internal object InheritedBindingContext
    {
        get => mInheritedBindingContext;
        set
        {
            if (value != mInheritedBindingContext)
            {
                var oldEffectiveBindingContext = EffectiveBindingContext;
                mInheritedBindingContext = value;
                HandleBindingContextChangedInternal(oldEffectiveBindingContext);

            }
        }
    }

    object mBindingContext;
    public object BindingContext
    {
        get => EffectiveBindingContext;
        set
        {
            if (value != EffectiveBindingContext)
            {
                var oldEffectiveBindingContext = EffectiveBindingContext;
                mBindingContext = value;
                HandleBindingContextChangedInternal(oldEffectiveBindingContext);
            }

        }
    }

    private void HandleBindingContextChangedInternal(object oldBindingContext)
    {
        // early out - this isn't technically necessary as 
        // the subscription code below can be called multiple
        // times, but it does make debugging easier.
        if (oldBindingContext == EffectiveBindingContext)
        {
            return;
        }

        if (oldBindingContext is INotifyPropertyChanged oldViewModel)
        {
            UnsubscribeEventsOnOldViewModel(oldViewModel);
        }
        if (EffectiveBindingContext is INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;

        }
        if (EffectiveBindingContext != null)
        {
            foreach (var vmProperty in vmPropsToUiProps.Keys)
            {
                UpdateToVmProperty(vmProperty);
            }


        }

        var args = new BindingContextChangedEventArgs();
        args.OldBindingContext = oldBindingContext;


        if (this.Children != null)
        {
            // do the default first...
            UpdateChildrenInheritedBindingContext(this.Children, EffectiveBindingContext);
            // ... then overwrite it
            foreach (var child in this.Children)
            {
                if (child is BindableGue gue)
                {
                    if (gue.BindingContextBinding != null)
                    {
                        gue.BindingContextBindingPropertyOwner = EffectiveBindingContext;

                        gue.UpdateToVmProperty(gue.BindingContextBinding);
                    }
                }
            }
        }
        else
        {
            // Do the default functionality first...
            UpdateChildrenInheritedBindingContext(this.ContainedElements, EffectiveBindingContext);
            // ... then overwrite it
            foreach (var gue in this.ContainedElements)
            {
                var bindableGue = gue as BindableGue;

                if (bindableGue?.BindingContextBinding != null)
                {
                    bindableGue.BindingContextBindingPropertyOwner = EffectiveBindingContext;

                    bindableGue.UpdateToVmProperty(bindableGue.BindingContextBinding);
                }
            }
        }
        BindingContextChanged?.Invoke(this, args);
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

    public object BindingContextBindingPropertyOwner { get; private set; }
    public string BindingContextBinding { get; private set; }

    public event Action<object, BindingContextChangedEventArgs> BindingContextChanged;

    object EffectiveBindingContext => mBindingContext ?? InheritedBindingContext;

    private static void UpdateChildrenInheritedBindingContext(IEnumerable<IRenderableIpso> children, object effectiveBindingContext)
    {
        foreach (var child in children)
        {
            if (child is BindableGue gue)
            {
                if (gue.InheritedBindingContext != effectiveBindingContext)
                {
                    var effectiveBeforeChange = gue.EffectiveBindingContext;
                    gue.InheritedBindingContext = effectiveBindingContext;
                    if (effectiveBindingContext != gue.EffectiveBindingContext)
                    {
                        // This saves us some processing. If the parent's effective didn't change, then no need
                        // to notify the children
                        UpdateChildrenInheritedBindingContext(child.Children, gue.EffectiveBindingContext);
                    }
                }
            }
        }
    }


    private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var vmPropertyName = e.PropertyName;
        var updated = UpdateToVmProperty(vmPropertyName);

        //if (updated)
        //{
        //    this.EffectiveManagers?.InvalidateSurface();
        //}
    }

    public void SetBinding(string uiProperty, string vmProperty, string toStringFormat = null)
    {
        if (uiProperty == nameof(BindingContext))
        {
            BindingContextBinding = vmProperty;
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

            if (EffectiveBindingContext != null)
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
                BindingContextBindingPropertyOwner : EffectiveBindingContext;

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
                    PropertyInfo uiProperty = this.GetType().GetProperty(binding.UiProperty);

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
        if (this.Children != null)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                IRenderableIpso child = Children[i];
                if (child is BindableGue gue)
                {
                    if (gue.BindingContextBinding == vmPropertyName && gue.BindingContextBindingPropertyOwner == EffectiveBindingContext)
                    {
                        gue.UpdateToVmProperty(vmPropertyName);
                    }
                    gue.TryPushBindingContextChangeToChildren(vmPropertyName);
                }
            }
        }
        else
        {
            for (int i = 0; i < WhatThisContains.Count; i++)
            {
                var gue = WhatThisContains[i] as BindableGue;
                if (gue != null )
                {
                    if( gue.BindingContextBinding == vmPropertyName && 
                        gue.BindingContextBindingPropertyOwner == EffectiveBindingContext)
                    {

                        gue.UpdateToVmProperty(vmPropertyName);
                    }
                    gue.TryPushBindingContextChangeToChildren(vmPropertyName);
                }
            }
        }
    }

    protected void PushValueToViewModel([CallerMemberName] string uiPropertyName = null)
    {

        var kvp = vmPropsToUiProps.FirstOrDefault(item => item.Value.UiProperty == uiPropertyName);

        if (kvp.Value.UiProperty == uiPropertyName)
        {
            var vmPropName = kvp.Key;

            var vmProperty = EffectiveBindingContext?.GetType().GetProperty(vmPropName);

            if (vmProperty != null)
            {
                var uiProperty = this.GetType().GetProperty(uiPropertyName);
                if (uiProperty != null)
                {
                    var uiValue = uiProperty.GetValue(this, null);

                    vmProperty.SetValue(EffectiveBindingContext, uiValue, null);
                }
            }
        }
    }

    #endregion

}