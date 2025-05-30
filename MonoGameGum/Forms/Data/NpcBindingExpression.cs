using System;
using System.ComponentModel;
using System.Reflection;
using Gum.Wireframe;

#if FRB
using FlatRedBall.Forms.Controls;
#elif RAYLIB
using RaylibGum.Forms.Controls;
#else
using MonoGameGum.Forms.Controls;
#endif

#if FRB
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
namespace RaylibGum.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

internal class NpcBindingExpression : UntypedBindingExpression
{
    private readonly PropertyInfo _targetProperty;

    public NpcBindingExpression(
        FrameworkElement target,
        string targetPropertyName,
        Binding binding) : base(target, binding, GetPropertyType(target, targetPropertyName))
    {
        _targetProperty = target.GetType()
            .GetProperty(targetPropertyName)
            ?? throw new InvalidOperationException(
                $"Property '{targetPropertyName}' not found on {target.GetType().Name}");

        if (binding.Mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            HookUpdateSource(binding.UpdateSourceTrigger);
        }

        if (targetPropertyName == nameof(FrameworkElement.BindingContext))
        {
            _targetElement.InheritedBindingContextChanged += OnInheritedBindingContextChanged;
        }
    }

    private void OnInheritedBindingContextChanged(object? sender, BindingContextChangedEventArgs e)
    {
        if (CurrentRoot != null)
        {
            AttachToSource(_targetElement.BindingContext);
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
                if (_targetElement is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += OnTargetPropertyChanged;
                }
                break;

            case UpdateSourceTrigger.LostFocus:
                _targetElement.LostFocus += OnLostFocus;
                break;
        }
    }

    private void OnLostFocus(object? s, EventArgs e) => UpdateSource();

    private void OnTargetPropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _targetProperty.Name)
        {
            UpdateSource();
        }
    }

    public override void UpdateTarget()
    {
        if (_binding.Mode is BindingMode.OneWayToSource)
        {
            return;
        }

        object? value = _targetProperty.Name switch
        {
            nameof(FrameworkElement.BindingContext) => GetRootSourceValue(),
            _ => GetSourceValue()
        };

        if (value == GumProperty.UnsetValue)
        {
            // treat binding errors as null
            value = null;
        }

        if (value is null && _binding.FallbackValue is not null)
        {
            value = _binding.FallbackValue;
        }
        else if (_binding.Converter is not null && value != Binding.DoNothing) // we don't run fallbacks through the converter
        {
            value = Convert(_binding.Converter, value, _targetType);
        }

        if (value == Binding.DoNothing)
        {
            return;
        }

        if (value != GumProperty.UnsetValue)
        {
            if (_targetProperty.PropertyType == typeof(string) && 
                _binding.StringFormat is not null && 
                value is not null)
            {
                value = string.Format(_binding.StringFormat, value);
            }
            else if (!IsValidForType(value, _targetType))
            {
                value = TryConvert(value, _targetType);
            }
        }

        if (value == GumProperty.UnsetValue)
        {
            // this will result in default(T), which is the best we can get
            // without more robust property metadata that would define a default
            value = null;
        }

        _suppressAttach = true;
        _targetProperty.SetValue(_targetElement, value);
        _suppressAttach = false;
    }

    public override void UpdateSource()
    {
        if (_binding.Mode is BindingMode.OneWay || LeafType is not {} sourceType || _targetProperty.Name == nameof(FrameworkElement.BindingContext))
        {
            return;
        }

        object? value = _targetProperty.GetValue(_targetElement);

        if (_binding.Converter is { } converter)
        {
            value = ConvertBack(converter, value, sourceType);
        }

        if (value == Binding.DoNothing) return;

        if (value == GumProperty.UnsetValue)
        {
            value = _binding.FallbackValue;
        }

        value ??= _binding.TargetNullValue;

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
        if (_targetElement is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnTargetPropertyChanged;
        }
        _targetElement.LostFocus -= OnLostFocus;
        _targetElement.InheritedBindingContextChanged -= OnInheritedBindingContextChanged;
        base.Dispose();
    }

    static Type GetPropertyType(object target, string propertyName)
    {
        if (target.GetType().GetProperty(propertyName) is not { } pi)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on {target.GetType().Name}");
        }
        return pi.PropertyType;
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