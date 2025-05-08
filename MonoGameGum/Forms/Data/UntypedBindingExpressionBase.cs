using System;
using Gum.Wireframe;


#if FRB
using FlatRedBall.Forms.Controls;
#else
using MonoGameGum.Forms.Controls;
#endif

#if FRB
namespace FlatRedBall.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

internal abstract class UntypedBindingExpression : BindingExpressionBase
{
    protected readonly Binding _binding;
    protected readonly FrameworkElement _targetElement;
    protected readonly Type _targetType;
    protected Type? LeafType => _pathObserver.LeafType;

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

    private void OnTargetBindingContextChanged(object? sender, BindingContextChangedEventArgs e) =>
        AttachToSource(e.NewBindingContext);

    private void AttachToSource(object? newSource)
    {
        _pathObserver.Attach(newSource);

        if (newSource?.GetType() is not { } sourceType)
        {
            return;
        }

        try
        {
            _sourceGetter = BinderHelpers.BuildGetter(sourceType, _binding.Path);
            _sourceSetter = BinderHelpers.BuildSetter(sourceType, _binding.Path);
        }
        catch (ArgumentException aex)
        {
            // binding error: broken path when trying to build the getter/setter
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

    protected void SetSourceValue(object? value)
    {
        if (_sourceSetter is null || !_pathObserver.HasResolution)
        {
            // binding error: broken path
            return;
        }
        _sourceSetter(_targetElement.BindingContext, value);
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
        _pathObserver.Dispose();
        base.Dispose();
    }
}