using System;
using System.Diagnostics;
using Gum.Wireframe;


#if FRB
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
using Gum.Forms.Controls;
namespace Gum.Forms.Data;
#endif

internal abstract class UntypedBindingExpression : BindingExpressionBase
{
    protected Binding Binding { get; }
    protected FrameworkElement TargetElement { get; }
    protected Type? LeafType => PathObserver.LeafType;
    protected object? CurrentRoot => PathObserver.CurrentRoot;
    private Func<object, object?>? SourceGetter { get; set; }
    private Action<object, object?>? SourceSetter { get; set; }
    private PropertyPathObserver PathObserver { get; }

    protected UntypedBindingExpression(FrameworkElement targetElement, Binding binding)
    {
        TargetElement = targetElement;
        TargetElement.BindingContextChanged += OnTargetBindingContextChanged;

        Binding = binding;

        PathObserver = new PropertyPathObserver(binding.Path);
        PathObserver.ValueChanged += UpdateTarget;
    }

    public void Start() => AttachToSource(TargetElement.BindingContext);

    private void OnTargetBindingContextChanged(object? sender, BindingContextChangedEventArgs e)
    {
        AttachToSource(e.NewBindingContext);
    }

    protected bool SuppressAttach { get; set; }

    protected void AttachToSource(object? newSource)
    {
        if (SuppressAttach)
        {
            return;
        }

        PathObserver.Detach();
        SourceGetter = null;
        SourceSetter = null;

        if (newSource is null)
        {
            return;
        }

        PathObserver.Attach(newSource);

        try
        {
            // Since simple inpc properties can't define default binding modes,
            // we always default to two-way and just make an attempt to build a setter
            SourceSetter = BinderHelpers.BuildSetter(newSource.GetType(), Binding.Path);
        }
        catch (Exception aex)
        {
            // The property might not have a setter
            // -or- we have other binding errors
        }

        try
        {
            SourceGetter = BinderHelpers.BuildGetter(newSource.GetType(), Binding.Path);
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
        if (SourceGetter is null || !PathObserver.HasResolution)
        {
            // binding error: broken path
            return GumProperty.UnsetValue;
        }
        return SourceGetter(TargetElement.BindingContext);
    }

    protected object? GetRootSourceValue()
    {
        if (SourceGetter is null || !PathObserver.HasResolution || CurrentRoot is null)
        {
            // binding error: broken path
            return GumProperty.UnsetValue;
        }
        return SourceGetter(CurrentRoot);
    }


    protected void SetSourceValue(object? value)
    {
        if (SourceSetter is null || !PathObserver.HasResolution || CurrentRoot is null)
        {
            // binding error: broken path
            return;
        }
        SourceSetter(CurrentRoot, value);
    }

    protected object? Convert(IValueConverter converter, object? value, Type targetType)
    {
        try
        {
            return converter.Convert(value, targetType, Binding.ConverterParameter);
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
            return converter.ConvertBack(value, targetType, Binding.ConverterParameter);
        }
        catch (Exception ex)
        {
            return GumProperty.UnsetValue;
        }
    }

    public override void Dispose()
    {
        TargetElement.BindingContextChanged -= OnTargetBindingContextChanged;
        PathObserver.ValueChanged -= UpdateTarget;
        PathObserver.Dispose();
        base.Dispose();
    }
}