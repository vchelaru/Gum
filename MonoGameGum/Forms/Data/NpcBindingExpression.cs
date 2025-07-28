using System;
using System.ComponentModel;
using System.Reflection;
using Gum.Wireframe;

#if FRB
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
using Gum.Forms.Controls;
namespace Gum.Forms.Data;
#endif
internal class NpcBindingExpression : UntypedBindingExpression
{
    private PropertyInfo TargetProperty { get; }
    internal PropertyInfo GetTargetProperty() => TargetProperty;
    private object? DefaultTargetValue { get; }
    private object? LastTargetValue { get; set; }
    
    public NpcBindingExpression(
        FrameworkElement target,
        string targetPropertyName,
        Binding binding) : base(target, binding)
    {
        TargetProperty = target.GetType()
            .GetProperty(targetPropertyName)
            ?? throw new InvalidOperationException(
                $"Property '{targetPropertyName}' not found on {target.GetType().Name}");

        DefaultTargetValue = TargetProperty.PropertyType.IsValueType
            ? Activator.CreateInstance(TargetProperty.PropertyType)
            : null;

        LastTargetValue = TargetProperty.GetValue(target);

        if (binding.Mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            HookUpdateSource(binding.UpdateSourceTrigger);
        }

        if (targetPropertyName == nameof(FrameworkElement.BindingContext))
        {
            target.InheritedBindingContextChanged += OnInheritedBindingContextChanged;
        }
    }
    
    private bool ShouldUpdateSource =>
        Binding.Mode is not BindingMode.OneWay &&
        TargetProperty.Name is not nameof(FrameworkElement.BindingContext);

    private void OnInheritedBindingContextChanged(object? sender, BindingContextChangedEventArgs e)
    {
        if (CurrentRoot != null)
        {
            AttachToSource(TargetElement.BindingContext);
        }
    }

    private void HookUpdateSource(UpdateSourceTrigger trigger)
    {
        switch (trigger)
        {
            case UpdateSourceTrigger.Default:
                // A more robust property system could have a default trigger defined on the property metadata
                // for now we just default to PropertyChanged
            case UpdateSourceTrigger.PropertyChanged:
                if (TargetElement is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += OnTargetPropertyChanged;
                }
                break;

            case UpdateSourceTrigger.LostFocus when ShouldUpdateSource:
                TargetElement.LostFocus += OnLostFocus;
                break;
        }
    }

    private void OnLostFocus(object? s, EventArgs e)
    {
        object? targetValue = TargetProperty.GetValue(TargetElement);
        if (targetValue != LastTargetValue)
        {
            SetValueToSource(targetValue);
        }
    }

    private void OnTargetPropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == TargetProperty.Name)
        {
            UpdateSource();
        }
    }

    public override void UpdateTarget()
    {
        if (Binding.Mode is BindingMode.OneWayToSource)
        {
            return;
        }

        object? value = TargetProperty.Name switch
        {
            nameof(FrameworkElement.BindingContext) => GetRootSourceValue(),
            _ => GetSourceValue()
        };

        if (value == GumProperty.UnsetValue)
        {
            // treat binding errors as null
            value = null;
        }

        if (value is null && Binding.FallbackValue is not null)
        {
            value = Binding.FallbackValue;
        }
        else if (Binding.Converter is not null && value != Binding.DoNothing) // we don't run fallbacks through the converter
        {
            value = Convert(Binding.Converter, value, TargetProperty.PropertyType);
        }

        if (value == Binding.DoNothing)
        {
            return;
        }

        if (value != GumProperty.UnsetValue)
        {
            if (TargetProperty.PropertyType == typeof(string) && 
                Binding.StringFormat is not null && 
                value is not null)
            {
                value = string.Format(Binding.StringFormat, value);
            }
            else if (!IsValidForType(value, TargetProperty.PropertyType))
            {
                value = TryConvert(value, TargetProperty.PropertyType);
            }
        }

        if (value == GumProperty.UnsetValue)
        {
            // this will result in default(T), which is the best we can get
            // without more robust property metadata that could define its own default
            value = DefaultTargetValue;
        }
        
        SuppressAttach = true;
        TargetProperty.SetValue(TargetElement, value);
        SuppressAttach = false;
        LastTargetValue = value;
    }

    public override void UpdateSource()
    {
        if (!ShouldUpdateSource)
        {
            return;
        }
        object? targetValue = TargetProperty.GetValue(TargetElement);
        SetValueToSource(targetValue);
    }

    private void SetValueToSource(object? value)
    {        
        if (LeafType is not { } sourceType)
        {
            return;
        }
        
        if (Binding.Converter is { } converter)
        {
            value = ConvertBack(converter, value, sourceType);
        }

        if (value == Binding.DoNothing) return;

        if (value == GumProperty.UnsetValue)
        {
            value = Binding.FallbackValue;
        }

        value ??= Binding.TargetNullValue;

        if (!IsValidForType(value, sourceType))
        {
            value = TryConvert(value, sourceType);
        }

        if (value == GumProperty.UnsetValue)
        {
            // conversion failed: don't update source if we don't know what to set
            return;
        }

        SetSourceValue(value);
    }

    public override void Dispose()
    {
        if (TargetElement is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnTargetPropertyChanged;
        }
        TargetElement.LostFocus -= OnLostFocus;
        TargetElement.InheritedBindingContextChanged -= OnInheritedBindingContextChanged;
        base.Dispose();
    }

    static bool IsValidForType(object? value, Type targetType)
    {
        if (value is null)
        {
            return !targetType.IsValueType ||
                   Nullable.GetUnderlyingType(targetType) is not null;
        }

        return value.GetType() == targetType ||
               targetType.IsInstanceOfType(value);
    }
    static object? TryConvert(object? source, Type targetType)
    {
        try
        {
            return System.Convert.ChangeType(source, targetType);
        }
        catch
        {
            return GumProperty.UnsetValue;
        }
    }
}