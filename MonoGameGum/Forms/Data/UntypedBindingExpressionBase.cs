using System;
using System.Diagnostics;
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

internal abstract class UntypedBindingExpression : BindingExpressionBase
{
    protected readonly Binding _binding;
    protected readonly FrameworkElement _targetElement;
    protected readonly Type _targetType;
    protected Type? LeafType => _pathObserver.LeafType;
    protected object? CurrentRoot => _pathObserver.CurrentRoot;

    private Func<object, object?>? _sourceGetter;
    private Action<object, object?>? _sourceSetter;

    private readonly PropertyPathObserver _pathObserver;

    protected UntypedBindingExpression(FrameworkElement targetElement, Binding binding, Type targetType)
    {
        _targetElement = targetElement;
        _targetElement.BindingContextChanged += OnTargetBindingContextChanged;

        _binding = binding;

        _pathObserver = new PropertyPathObserver(binding.Path);
        _pathObserver.ValueChanged += UpdateTarget;
        _targetType = targetType;
    }

    public void Start() => AttachToSource(_targetElement.BindingContext);

    protected void OnTargetBindingContextChanged(object? sender, BindingContextChangedEventArgs e)
    {
        AttachToSource(e.NewBindingContext);
    }

    protected bool _suppressAttach;

    protected void AttachToSource(object? newSource)
    {
        if (_suppressAttach)
        {
            return;
        }

        _pathObserver.Detach();
        _sourceGetter = null;
        _sourceSetter = null;

        if (newSource is null)
        {
            return;
        }

        _pathObserver.Attach(newSource);

        try
        {
            // Since simple inpc properties can't define default binding modes,
            // we always default to two-way and just make an attempt to build a setter
            _sourceSetter = BinderHelpers.BuildSetter(newSource.GetType(), _binding.Path);
        }
        catch (Exception aex)
        {
            // The property might not have a setter
            // -or- we have other binding errors
        }

        try
        {
            _sourceGetter = BinderHelpers.BuildGetter(newSource.GetType(), _binding.Path);
        }
        catch (Exception aex)
        {
            // If we have binding errors in the source-getter we have nothing to update the target with.
            return;
        }

        UpdateTarget();
    }

    protected object? GetSourceValue()
    {
        if (_sourceGetter is null || !_pathObserver.HasResolution)
        {
            // binding error: broken path
            return GumProperty.UnsetValue;
        }
        return _sourceGetter(_targetElement.BindingContext);
    }

    protected object? GetRootSourceValue()
    {
        if (_sourceGetter is null || !_pathObserver.HasResolution || CurrentRoot is null)
        {
            // binding error: broken path
            return GumProperty.UnsetValue;
        }
        return _sourceGetter(CurrentRoot);
    }


    protected void SetSourceValue(object? value)
    {
        if (_sourceSetter is null || !_pathObserver.HasResolution || CurrentRoot is null)
        {
            // binding error: broken path
            return;
        }
        _sourceSetter(CurrentRoot, value);
    }

    protected object? Convert(IValueConverter converter, object? value, Type targetType)
    {
        try
        {
            return converter.Convert(value, targetType, _binding.ConverterParameter);
        }
        catch (Exception ex)
        {
            return GumProperty.UnsetValue;
        }
    }

    protected object? ConvertBack(IValueConverter converter, object? value, Type targetType)
    {
        try
        {
            return converter.ConvertBack(value, targetType, _binding.ConverterParameter);
        }
        catch (Exception ex)
        {
            return GumProperty.UnsetValue;
        }
    }

    public override void Dispose()
    {
        _targetElement.BindingContextChanged -= OnTargetBindingContextChanged;
        _pathObserver.ValueChanged -= UpdateTarget;
        _pathObserver.Dispose();
        base.Dispose();
    }
}