using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

    /// <summary>
    /// Whether the target property has a setter of any visibility; SetValue reaches a non-public one.
    /// </summary>
    internal bool HasTargetSetter => TargetProperty.SetMethod != null;

    private object? DefaultTargetValue { get; }
    private object? LastTargetValue { get; set; }

    /// <summary>
    /// What the target held before the property was first bound (default(T) for a write-only target);
    /// <see cref="PropertyRegistry"/> carries it across rebinds. A cleared BindingContext or ClearBinding resets
    /// to it: Gum has no per-property default metadata, and this is the control's own value without a view model
    /// (an unchecked CheckBox, SelectedIndex -1, a visual's authored Text).
    /// </summary>
    internal object? UnboundTargetValue { get; set; }
    private bool _isUpdatingTarget;
    private bool _isUpdatingSource;
    private bool _isLostFocusTrigger;
    private bool _hasReportedFailedConversion;
    private bool _hasReportedFailedSourceConversion;
    // Text typed mid-edit ("", "-") often does not convert yet; a failure is reported only if it is still the
    // last write when the edit ends.
    private (object Value, Type SourceType)? _pendingFailedSourceConversion;
    private bool _hasReportedUnwritableSource;
    // The target's last change came from user input rather than from code; only such a change is reported
    // when the source cannot take it.
    private bool _isTargetValueFromInput;
    // A TextBox/PasswordBox edit is being written to the source with the PropertyChanged trigger.
    private bool _isPushingTextEdit;
    // The source sent a value back during that edit; shown when the edit ends.
    private bool _hasDeferredTargetUpdate;

    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Only value types reach Activator.CreateInstance here, and every value type " +
            "has a parameterless constructor that the trimmer cannot remove.")]
    public NpcBindingExpression(
        IBindingTarget target,
        string targetPropertyName,
        Binding binding) : base(target, targetPropertyName, binding)
    {
        PropertyInfo targetProperty = target.GetType().GetProperty(targetPropertyName)
            ?? throw new InvalidOperationException(
                $"Property '{targetPropertyName}' not found on {target.GetType().Name}");
        // Declared lookup, so a private setter on a base class is found for the read-only check, writes and ClearBinding.
        TargetProperty = BinderHelpers.AsDeclared(targetProperty);

        // OneWay and TwoWay write the source value to the target, so refuse a target that cannot take it here
        // rather than when a BindingContext arrives. SetValue reaches a non-public setter, so any setter will do.
        if (Mode is not BindingMode.OneWayToSource &&
            !HasTargetSetter)
        {
            throw new InvalidOperationException(BinderHelpers.GetReadOnlyTargetMessage(
                target.GetType().Name, targetPropertyName, Mode.ToString()));
        }

        DefaultTargetValue = TargetProperty.PropertyType.IsValueType
            ? Activator.CreateInstance(TargetProperty.PropertyType)
            : null;
        UnboundTargetValue = DefaultTargetValue;

        // A write-only target has no value to send back, so it binds OneWay even when the mode is
        // TwoWay (the default). OneWayToSource would never do anything, so it is refused.
        if (!TargetProperty.CanRead)
        {
            if (Mode is BindingMode.OneWayToSource)
            {
                throw new InvalidOperationException(
                    $"Property '{targetPropertyName}' on {target.GetType().Name} has no getter, so it cannot be " +
                    $"the target of a {nameof(BindingMode.OneWayToSource)} binding.");
            }
        }
        else
        {
            LastTargetValue = TargetProperty.GetValue(target);
            UnboundTargetValue = LastTargetValue;
        }

        if (Mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            HookUpdateSource(binding.UpdateSourceTrigger);
        }

        if (IsBindingContextTarget)
        {
            target.InheritedBindingContextChanged += OnInheritedBindingContextChanged;

            // The context the target held of its own, so clearing the binding lets it inherit again rather
            // than keep the parent's context it read here.
            UnboundTargetValue = target.BindingContext == target.InheritedBindingContext ? null : target.BindingContext;
        }
    }
    
    private bool ShouldUpdateSource =>
        Mode is not BindingMode.OneWay &&
        TargetProperty.CanRead &&
        TargetProperty.Name is not nameof(FrameworkElement.BindingContext);

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic relay when an inherited BindingContext changes further up the visual " +
            "tree. The trim risk is already surfaced wherever SetBinding was actually called to create " +
            "this binding.")]
    private void OnInheritedBindingContextChanged(object? sender, BindingContextChangedEventArgs e) =>
        AttachToSource(e.NewBindingContext);

    private void HookUpdateSource(UpdateSourceTrigger trigger)
    {
        if (trigger == UpdateSourceTrigger.Default)
        {
            trigger = Binding.ResolveDefaultUpdateTrigger(TargetElement.GetType(), TargetProperty.Name);
        }

        switch (trigger)
        {
            case UpdateSourceTrigger.PropertyChanged:
                TargetElement.PropertyChanged += OnTargetPropertyChanged;
                if (IsTextEditTarget && TargetElement is TextBoxBase textBox)
                {
                    textBox.LostFocus += OnTextEditEnded;
                    textBox.EditCommitted += OnTextEditEnded;
                }
                break;

            case UpdateSourceTrigger.LostFocus when ShouldUpdateSource:
                _isLostFocusTrigger = true;
                TargetElement.LostFocus += OnLostFocus;
                // Only notes whether each change came from user input; the write waits for focus to leave.
                TargetElement.PropertyChanged += OnTargetPropertyChanged;
                break;
        }
    }

    private void OnLostFocus(object? s, EventArgs e) => FlushPendingTargetValue();

    protected override void FlushPendingTargetValue()
    {
        if (!_isLostFocusTrigger || !ShouldUpdateSource)
        {
            return;
        }

        // Compared by value: a getter may build a new string or box a number on every read.
        object? targetValue = TargetProperty.GetValue(TargetElement);
        if (!Equals(targetValue, LastTargetValue))
        {
            // Remembered first, so a source that raises no PropertyChanged is not written again on the next
            // focus loss; a value the source sends back replaces it through UpdateTarget.
            LastTargetValue = targetValue;
            SetValueToSource(targetValue);
        }
    }

    private void OnTargetPropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != TargetProperty.Name)
        {
            return;
        }

        _isTargetValueFromInput = IsDispatchingInput;
        if (_isLostFocusTrigger)
        {
            return;
        }

        // While the user types, a value the view model sends back for this edit (a re-entrant
        // PropertyChanged, or the post-write read-back) would rewrite the text mid-typing ("1." becoming
        // "1"), so it waits until the edit ends, as WPF does.
        _isPushingTextEdit = TargetElement is TextBoxBase { IsFocused: true } && IsTextEditTarget;
        try
        {
            UpdateSource();
        }
        finally
        {
            _isPushingTextEdit = false;
        }
    }

#if FRB
    // FlatRedBall dispatches input through its own GuiManager, which raises no such flag, so no change is
    // known to come from the user there.
    private static bool IsDispatchingInput => false;
#else
    private static bool IsDispatchingInput => InteractiveGue.IsDispatchingInput;
#endif

    private bool IsTextEditTarget =>
        TargetElement is TextBoxBase &&
        TargetProperty.Name is nameof(TextBox.Text) or nameof(PasswordBox.Password);

    private void OnTextEditEnded(object? sender, EventArgs e)
    {
        // The last write of the edit failed, so the text the user left does not convert.
        if (_pendingFailedSourceConversion is { } failed)
        {
            _pendingFailedSourceConversion = null;
            ReportFailedSourceConversionOnce(failed.Value, failed.SourceType);
        }

        if (_hasDeferredTargetUpdate)
        {
            _hasDeferredTargetUpdate = false;
            RefreshTarget();
        }
    }

    /// <summary>
    /// Pushes the current source value to the target. Does nothing while detached from a source, so an
    /// absent view model leaves the target alone, and skips a BindingContext binding, whose value the
    /// control already carries.
    /// </summary>
    public void RefreshTarget()
    {
        if (CurrentRoot != null && TargetProperty.Name is not nameof(FrameworkElement.BindingContext))
        {
            UpdateTarget();
        }
    }

    public override void UpdateTarget()
    {
        if (_isUpdatingTarget || Mode is BindingMode.OneWayToSource)
        {
            return;
        }

        if (_isPushingTextEdit)
        {
            _hasDeferredTargetUpdate = true;
            return;
        }
        _hasDeferredTargetUpdate = false;

        object? value = TargetProperty.Name switch
        {
            nameof(FrameworkElement.BindingContext) => GetRootSourceValue(),
            _ => GetSourceValue()
        };

        bool isPathBroken = value == GumProperty.UnsetValue;
        if (isPathBroken)
        {
            // treat binding errors as null
            value = null;
        }

        // A resolved null shows TargetNullValue; a broken path (or a null with no TargetNullValue) shows
        // FallbackValue. Neither runs through the converter.
        bool isFallback = false;
        if (value is null && !isPathBroken && Binding.TargetNullValue is not null)
        {
            value = Binding.TargetNullValue;
        }
        else if (value is null && Binding.FallbackValue is not null)
        {
            value = Binding.FallbackValue;
            isFallback = true;
        }
        else if (Binding.Converter is not null && value != Binding.DoNothing) // we don't run fallbacks through the converter
        {
            value = Convert(Binding.Converter, value, TargetProperty.PropertyType);
        }

        if (value == Binding.DoNothing)
        {
            return;
        }

        // A value that cannot reach the target (a converter that failed or returned the wrong type, a
        // StringFormat the value cannot take, a name no enum member has) shows FallbackValue when one is set,
        // as WPF does. Without one, each failure keeps its own outcome: the default, or a throw.
        bool canFallBack = !isFallback && Binding.FallbackValue is not null;
        value = ToTargetValue(value, canFallBack);
        if (value == GumProperty.UnsetValue && canFallBack)
        {
            value = ToTargetValue(Binding.FallbackValue, canFallBack: false);
        }

        if (value == GumProperty.UnsetValue)
        {
            // this will result in default(T), which is the best we can get
            // without more robust property metadata that could define its own default
            value = DefaultTargetValue;
        }

        SetTargetValue(value);
    }

    /// <summary>
    /// Shows FallbackValue (not converted), or <see cref="UnboundTargetValue"/> when there is none or it cannot
    /// reach the target. A OneWayToSource binding never writes the target, and a BindingContext binding is left alone.
    /// </summary>
    protected override void ResetTarget()
    {
        if (Mode is BindingMode.OneWayToSource ||
            TargetProperty.Name is nameof(FrameworkElement.BindingContext))
        {
            return;
        }

        object? value = Binding.FallbackValue is null
            ? GumProperty.UnsetValue
            : ToTargetValue(Binding.FallbackValue, canFallBack: true);

        SetTargetValue(value == GumProperty.UnsetValue ? UnboundTargetValue : value);
    }

    private void SetTargetValue(object? value)
    {
        _isUpdatingTarget = true;
        SuppressAttach = true;
        try
        {
            TargetProperty.SetValue(TargetElement, value);
        }
        finally
        {
            SuppressAttach = false;
            _isUpdatingTarget = false;
        }
        LastTargetValue = value;
    }

    /// <summary>
    /// Formats or converts a value for the target property. Returns <see cref="GumProperty.UnsetValue"/> when
    /// it cannot; with <paramref name="canFallBack"/> false, a format or enum-name failure throws instead.
    /// </summary>
    private object? ToTargetValue(object? value, bool canFallBack)
    {
        if (value == GumProperty.UnsetValue)
        {
            return value;
        }

        if (TargetProperty.PropertyType == typeof(string) &&
            Binding.StringFormat is not null &&
            value is not null)
        {
            try
            {
                return string.Format(Binding.StringFormat, value);
            }
            catch (FormatException) when (canFallBack)
            {
                return GumProperty.UnsetValue;
            }
        }

        if (IsValidForType(value, TargetProperty.PropertyType))
        {
            return value;
        }

        object? converted = TryConvert(value, TargetProperty.PropertyType);

        // An enum target refuses a value that names no member rather than showing its default.
        if (converted == GumProperty.UnsetValue && !canFallBack && value is not null &&
            GetEnumType(TargetProperty.PropertyType) is { } enumType)
        {
            throw new InvalidOperationException(BinderHelpers.GetUnmatchedEnumMessage(
                value, enumType, TargetElement.GetType().Name, TargetProperty.Name));
        }

        if (converted == GumProperty.UnsetValue && value is not null)
        {
            ReportFailedConversionOnce(value, canFallBack);
        }

        return converted;
    }

    // A value the target cannot take shows FallbackValue or the default instead of throwing, so it is reported
    // once per binding rather than going unseen.
    private void ReportFailedConversionOnce(object value, bool showsFallback)
    {
        if (_hasReportedFailedConversion)
        {
            return;
        }
        _hasReportedFailedConversion = true;
        string shown = showsFallback ? nameof(Binding.FallbackValue) : "default";
        GumRuntime.ElementSaveExtensions.ReportDiagnostic(
            $"Binding for {TargetElement.GetType().Name}.{TargetProperty.Name} could not convert the value " +
            $"'{value}' ({value.GetType().Name}) to {TargetProperty.PropertyType.Name}, so the property shows its {shown}.");
    }

    // An edit the source cannot take is dropped, so it is reported once per binding rather than going unseen.
    private void ReportFailedSourceConversionOnce(object value, Type sourceType)
    {
        if (_hasReportedFailedSourceConversion)
        {
            return;
        }
        _hasReportedFailedSourceConversion = true;
        string source = CurrentRoot is null ? Binding.Path : $"{CurrentRoot.GetType().Name}.{Binding.Path}";
        GumRuntime.ElementSaveExtensions.ReportDiagnostic(
            $"Binding for {TargetElement.GetType().Name}.{TargetProperty.Name} could not convert the value " +
            $"'{value}' ({value.GetType().Name}) to {sourceType.Name} for {source}, so the view model was not changed.");
    }

    public override void UpdateSource()
    {
        if (!ShouldUpdateSource)
        {
            return;
        }
        WriteTargetToSource();
    }

    /// <summary>
    /// Writes the target's value to the source when the target asks for it (a visual's PushValueToViewModel).
    /// Unlike a PropertyChanged trigger, this also writes for a binding that left its mode unset.
    /// </summary>
    public void PushTargetToSource()
    {
        if (!CanWriteSource || !TargetProperty.CanRead ||
            TargetProperty.Name is nameof(FrameworkElement.BindingContext))
        {
            return;
        }
        // A field or a property with no setter ignores the push; the target asked for the write, so say why once.
        if (!_isUpdatingTarget)
        {
            ReportUnwritableSourceOnce("pushed its value", advise: false);
        }
        WriteTargetToSource();
    }

    /// <summary>
    /// Reports once per binding that a value meant for the source was dropped because the source is a field or a
    /// property with no setter. Does nothing when the source can be written.
    /// </summary>
    private void ReportUnwritableSourceOnce(string whatTargetDid, bool advise)
    {
        if (_hasReportedUnwritableSource || CurrentRoot is null || !(IsSourceField || IsSourceResolvedWithoutSetter))
        {
            return;
        }
        _hasReportedUnwritableSource = true;
        string reason = IsSourceField ? "is a field, which bindings never write" : "has no setter";
        string advice = !advise ? string.Empty
            : IsSourceField
                ? " Bind with Mode = OneWay for a display-only control, or make it a property with a setter."
                : " Bind with Mode = OneWay for a display-only control, or give the property a setter.";
        GumRuntime.ElementSaveExtensions.ReportDiagnostic(
            $"{TargetElement.GetType().Name}.{TargetProperty.Name} {whatTargetDid}, but " +
            $"{CurrentRoot.GetType().Name}.{Binding.Path} {reason}, so the view model was not changed.{advice}");
    }

    private void WriteTargetToSource()
    {
        // During a BindingContext change, a binding that has not re-attached yet still points at the
        // outgoing view model; a target change raised by a sibling binding must not be written there (#7856).
        if (_isUpdatingSource || _isUpdatingTarget || !IsAttachedToCurrentBindingContext)
        {
            return;
        }
        _isUpdatingSource = true;
        try
        {
            object? targetValue = TargetProperty.GetValue(TargetElement);
            SetValueToSource(targetValue);
        }
        finally
        {
            _isUpdatingSource = false;
        }
    }

    private void SetValueToSource(object? value)
    {
        if (LeafType is not { } sourceType)
        {
            return;
        }
        _pendingFailedSourceConversion = null;

        // TargetNullValue is what the target shows for a null source, so the target holding it means null, as
        // in WPF. Like the way in, it skips the converter.
        if (Binding.TargetNullValue is not null && Equals(value, Binding.TargetNullValue))
        {
            value = null;
        }
        else if (Binding.Converter is { } converter)
        {
            value = ConvertBack(converter, value, sourceType);
        }

        // UnsetValue (a ConvertBack that threw or produced no value) leaves the source unchanged, as WPF does;
        // FallbackValue is a target-side value and never reaches the source.
        if (value == Binding.DoNothing || value == GumProperty.UnsetValue) return;

        if (!IsValidForType(value, sourceType))
        {
            object? converted = TryConvert(value, sourceType);
            if (converted == GumProperty.UnsetValue)
            {
                // The source keeps its value; a null into a value type stays silent, as on the way to the target.
                // While the user types, the report waits for the edit to end (OnTextEditEnded).
                if (value is not null && _isPushingTextEdit)
                {
                    _pendingFailedSourceConversion = (value, sourceType);
                }
                else if (value is not null)
                {
                    ReportFailedSourceConversionOnce(value, sourceType);
                }
                return;
            }
            value = converted;
        }

        if (!SetSourceValue(value))
        {
            // A value set from code is left alone: a TwoWay binding to a get-only property is the usual
            // display-only binding, and only a user's edit shows that the view model was expected to change.
            if (_isTargetValueFromInput)
            {
                ReportUnwritableSourceOnce("was changed by the user", advise: true);
            }
            return;
        }

        // A setter may keep another value (clamping to the value it already holds) without raising
        // PropertyChanged, so read the source back and show it when it differs, as WPF does since .NET 4.
        // Read once, never written again. Skipped while a BindingContext change is flushing a pending edit:
        // the incoming context refreshes the target.
        if (IsAttachedToCurrentBindingContext &&
            !Equals(GetRootSourceValue(), value))
        {
            UpdateTarget();
        }
    }

    public override void Dispose()
    {
        TargetElement.PropertyChanged -= OnTargetPropertyChanged;
        TargetElement.LostFocus -= OnLostFocus;
        TargetElement.LostFocus -= OnTextEditEnded;
        if (TargetElement is TextBoxBase textBox)
        {
            textBox.EditCommitted -= OnTextEditEnded;
        }
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
    static Type? GetEnumType(Type type) =>
        (Nullable.GetUnderlyingType(type) ?? type) is { IsEnum: true } enumType ? enumType : null;

    static object? TryConvert(object? source, Type targetType)
    {
        // Convert.ChangeType does not reach enums; names and numbers convert as WPF's binding does.
        if (source is not null && GetEnumType(targetType) is { } enumType)
        {
            return BinderHelpers.ConvertToEnum(source, enumType);
        }

        try
        {
            // Integral Parse rejects group separators, so a number shown as "1,234" would never come back.
            // Parsed as a decimal with the current culture's separators instead; a fraction is refused.
            if (source is string text && IsIntegral(targetType))
            {
                return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal number) &&
                       decimal.Truncate(number) == number
                    ? System.Convert.ChangeType(number, targetType, CultureInfo.InvariantCulture)
                    : GumProperty.UnsetValue;
            }

            return System.Convert.ChangeType(source, targetType);
        }
        catch
        {
            return GumProperty.UnsetValue;
        }
    }

    static bool IsIntegral(Type type) => Type.GetTypeCode(type) is
        TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
        TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;
}