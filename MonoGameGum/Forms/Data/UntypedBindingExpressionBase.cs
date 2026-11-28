using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gum.Wireframe;

#if !FRB
using Gum.Mvvm;
#endif


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

    /// <summary>The binding's mode, or the target's default mode when the binding does not set one.</summary>
    protected BindingMode Mode { get; }

    /// <summary>
    /// Whether the source may be written: by the mode, or by an explicit push from a target whose binding left the
    /// mode unset (a visual's PushValueToViewModel).
    /// </summary>
    protected bool CanWriteSource => Mode is BindingMode.TwoWay or BindingMode.OneWayToSource || !Binding.IsModeSet;
    protected IBindingTarget TargetElement { get; }
    protected Type? LeafType => PathObserver.LeafType;
    protected object? CurrentRoot => PathObserver.CurrentRoot;

    /// <summary>
    /// Whether the path resolved to a source property that has no setter, so a write to the source does nothing.
    /// A field is excluded: it is read, never written, by design.
    /// </summary>
    protected bool IsSourceResolvedWithoutSetter =>
        PathObserver.HasResolution && !IsLeafViewModelField && SourceSetter is null &&
        _unassignableStructMessage is null;

    /// <summary>Whether the path resolved to a source field, which a binding reads but never writes.</summary>
    protected bool IsSourceField => PathObserver.HasResolution && IsLeafViewModelField;

    // A field of a struct (Vector2.X) is written on a copy that goes back through the holder's property setter, so
    // only a field of a view model object falls under "read, never written".
    private bool IsLeafViewModelField => PathObserver.IsLeafField && !_isLeafInStruct;

    private Func<object, object?>? SourceGetter { get; set; }
    private Action<object, object?>? SourceSetter { get; set; }
    private PropertyPathObserver PathObserver { get; }
    private readonly string _targetPropertyName;
    // Set when a write must assign a struct back to a member that cannot take it; reported on the first dropped write,
    // since a binding that only displays the value never writes.
    private string? _unassignableStructMessage;
    private bool _isLeafInStruct;

    protected UntypedBindingExpression(IBindingTarget targetElement, string targetPropertyName, Binding binding)
    {
        // Checked before anything subscribes, so a refused binding leaves nothing attached to the target.
        if (BinderHelpers.GetStringFormatErrorMessage(binding.StringFormat) is { } stringFormatErrorMessage)
        {
            throw new InvalidOperationException(stringFormatErrorMessage);
        }

        _targetPropertyName = targetPropertyName;
        TargetElement = targetElement;
        TargetElement.BindingContextChanged += OnTargetBindingContextChanged;

        Binding = binding;
        Mode = binding.IsModeSet ? binding.Mode : targetElement.DefaultBindingMode;

        PathObserver = new PropertyPathObserver(binding.Path);
        PathObserver.ValueChanged += UpdateTarget;
    }

    [RequiresUnreferencedCode("Calls " + nameof(AttachToSource) + ".")]
    public void Start()
    {
        if (IsBindingContextTarget)
        {
            // Resolved first, so the target takes over its context only once the path has given it a value.
            AttachToSource(TargetElement.InheritedBindingContext);
            TargetElement.SetIsBindingContextBound(true);
        }
        else
        {
            AttachToSource(TargetElement.BindingContext);
        }
    }

    /// <summary>
    /// Whether this binding sets the target's BindingContext. Its path is read from the context the target
    /// inherits, so it follows the parent's context rather than the context it sets.
    /// </summary>
    protected bool IsBindingContextTarget => _targetPropertyName == nameof(IBindingTarget.BindingContext);

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic relay when an already-bound target's BindingContext changes. The " +
            "trim risk is already surfaced wherever SetBinding was actually called to create this binding.")]
    private void OnTargetBindingContextChanged(object? sender, BindingContextChangedEventArgs e)
    {
        if (IsBindingContextTarget)
        {
            return;
        }
        AttachToSource(e.NewBindingContext);
    }

    protected bool SuppressAttach { get; set; }

    /// <summary>
    /// Whether the source this binding is attached to is the target's current BindingContext. False
    /// while a BindingContext change is being relayed and this binding has not re-attached yet.
    /// </summary>
    protected bool IsAttachedToCurrentBindingContext =>
        ReferenceEquals(CurrentRoot, TargetElement.BindingContext);

    /// <summary>
    /// Commits any pending edit to the outgoing source before <see cref="AttachToSource"/> repoints
    /// this binding at a new one. A source swap (e.g. a master-detail panel's BindingContext changing)
    /// is an implicit end-of-edit, since the control is about to display a different object entirely -
    /// so a deferred-commit trigger (like <see cref="UpdateSourceTrigger.LostFocus"/>) must flush here
    /// even when the target never raised its real focus-lost event.
    /// </summary>
    protected virtual void FlushPendingTargetValue() { }

    /// <summary>
    /// Shows the value for "no source" after the BindingContext became null, as WPF does when its
    /// DataContext is cleared.
    /// </summary>
    protected virtual void ResetTarget() { }

    [RequiresUnreferencedCode(
        "Resolves the binding path by name against newSource's own type (PropertyPathObserver.Attach, " +
        "BinderHelpers.BuildGetter/BuildSetter). Those members may be removed under PublishTrimmed if " +
        "nothing else in the app references them.")]
    protected void AttachToSource(object? newSource)
    {
        if (SuppressAttach)
        {
            return;
        }

        FlushPendingTargetValue();

        bool wasAttached = CurrentRoot is not null;
        PathObserver.Detach();
        SourceGetter = null;
        SourceSetter = null;
        _unassignableStructMessage = null;
        _isLeafInStruct = false;

        if (newSource is null)
        {
            // Only a context that was attached and went away resets the target, so binding before any
            // context exists keeps the value the control already shows.
            if (wasAttached)
            {
                ResetTarget();
            }
            return;
        }

        ReportMissingPathOnce(newSource);

        // OneWayToSource only writes, so a source member with no setter would leave it doing nothing. TwoWay (the
        // default mode) still shows the source value, so a get-only source keeps working in that direction.
        if (Mode is BindingMode.OneWayToSource &&
            BinderHelpers.GetReadOnlySourceMessage(newSource.GetType(), Binding.Path, Mode.ToString(),
                TargetElement.GetType().Name, _targetPropertyName) is { } readOnlySourceMessage)
        {
            throw new InvalidOperationException(readOnlySourceMessage);
        }

        PathObserver.Attach(newSource);
        _isLeafInStruct = BinderHelpers.IsLeafInStruct(newSource, Binding.Path);

        bool shouldBuildSourceSetter =
            CanWriteSource;

        if (shouldBuildSourceSetter && BinderHelpers.CanWritePath(newSource.GetType(), Binding.Path))
        {
            try
            {
                SourceSetter = BinderHelpers.BuildSetter(newSource.GetType(), Binding.Path);
            }
            catch (Exception)
            {
                // Keep this safe for edge-case expression failures, but do not rely on exceptions
                // for normal readonly binding behavior.
            }
        }
        else if (shouldBuildSourceSetter)
        {
            _unassignableStructMessage = BinderHelpers.GetUnassignableStructMessage(newSource.GetType(),
                Binding.Path, Mode.ToString(), TargetElement.GetType().Name, _targetPropertyName);
        }

        try
        {
            SourceGetter = BinderHelpers.BuildGetter(newSource.GetType(), Binding.Path);
        }
        catch (Exception)
        {
            // If we have binding errors in the source-getter we have nothing to update the target with.
            return;
        }

        // OneWayToSource hands the new source the value the control already holds, as WPF does.
        if (Mode is BindingMode.OneWayToSource)
        {
            UpdateSource();
        }
        else
        {
            UpdateTarget();
        }
    }

    [RequiresUnreferencedCode("Calls " + nameof(BinderHelpers.GetMissingPathMessage) + ".")]
    private void ReportMissingPathOnce(object source)
    {
        string? message = BinderHelpers.GetMissingPathMessage(source, Binding.Path, allowEventLeaf: false,
            TargetElement.GetType().Name, _targetPropertyName);
        if (message != null && ShouldReport(nameof(ReportMissingPathOnce), source.GetType()))
        {
            GumRuntime.ElementSaveExtensions.ReportDiagnostic(message);
        }
    }

    protected object? GetSourceValue()
    {
        if (SourceGetter is null || !PathObserver.HasResolution || TargetElement.BindingContext is not { } bindingContext)
        {
            // binding error: broken path
            return GumProperty.UnsetValue;
        }
        return SourceGetter(bindingContext);

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


    /// <summary>
    /// Writes the value to the source. Returns false when nothing was written (no setter or a broken path).
    /// </summary>
    protected bool SetSourceValue(object? value)
    {
        // A view model field is read, never written.
        if (SourceSetter is null || !PathObserver.HasResolution || IsLeafViewModelField || CurrentRoot is null)
        {
            if (_unassignableStructMessage is { } message && PathObserver.HasResolution && !IsLeafViewModelField)
            {
                ReportUnassignableStructOnce(message);
            }
            // binding error: broken path
            return false;
        }

        try
        {
            SourceSetter(CurrentRoot, value);
        }
        catch (BinderHelpers.StructWriteBackException exception)
        {
            // A struct past a segment resolved on the runtime type, whose holder turned out not to be assignable.
            ReportUnassignableStructOnce($"{exception.Message} The {Mode} binding of " +
                $"{TargetElement.GetType().Name}.{_targetPropertyName} cannot write to the view model.");
            return false;
        }
        return true;
    }

    private void ReportUnassignableStructOnce(string message)
    {
        if (ShouldReport(nameof(ReportUnassignableStructOnce), CurrentRoot?.GetType()))
        {
            GumRuntime.ElementSaveExtensions.ReportDiagnostic(message);
        }
    }

    // A converter that throws is caught so a bug in it cannot throw mid-frame, and reported once per direction
    // per binding and source type so it does not look like an empty value.
    protected object? Convert(IValueConverter converter, object? value, Type targetType)
    {
        try
        {
            return converter.Convert(value, targetType, Binding.ConverterParameter);
        }
        catch (Exception ex)
        {
            if (ShouldReport(nameof(IValueConverter.Convert), CurrentRoot?.GetType()))
            {
                string shown = Binding.FallbackValue is null ? "default" : nameof(Binding.FallbackValue);
                ReportConverterException(converter, nameof(IValueConverter.Convert), ex,
                    $"so the property shows its {shown}.");
            }
            return GumProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Runs <see cref="IValueConverter.ConvertBack"/>. Returns <see cref="GumProperty.UnsetValue"/> when it throws,
    /// which leaves the source unchanged, as WPF does.
    /// </summary>
    protected object? ConvertBack(IValueConverter converter, object? value, Type targetType)
    {
        try
        {
            return converter.ConvertBack(value, targetType, Binding.ConverterParameter);
        }
        catch (Exception ex)
        {
            if (ShouldReport(nameof(IValueConverter.ConvertBack), CurrentRoot?.GetType()))
            {
                ReportConverterException(converter, nameof(IValueConverter.ConvertBack), ex,
                    $"so {Binding.Path} was not changed.");
            }
            return GumProperty.UnsetValue;
        }
    }

    private void ReportConverterException(IValueConverter converter, string methodName, Exception ex, string outcome) =>
        GumRuntime.ElementSaveExtensions.ReportDiagnostic(
            $"Binding for {TargetElement.GetType().Name}.{_targetPropertyName}: {converter.GetType().Name}.{methodName} " +
            $"threw {ex.GetType().Name} ('{ex.Message}'), {outcome}");

    public override void Dispose()
    {
        TargetElement.BindingContextChanged -= OnTargetBindingContextChanged;
        PathObserver.ValueChanged -= UpdateTarget;
        PathObserver.Dispose();
        if (IsBindingContextTarget)
        {
            TargetElement.SetIsBindingContextBound(false);
        }
        base.Dispose();
    }
}
