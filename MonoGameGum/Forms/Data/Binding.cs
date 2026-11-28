using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Gum.Wireframe;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;
#endif

/// <summary>
/// Defines the direction of data flow in a binding.
/// </summary>
public enum BindingMode
{
    /// <summary>
    /// Data flows from the source (the ViewModel property) to the target (the UI property) only.
    /// Changes in the source update the target, but changes in the target do not update the source.
    /// </summary>
    OneWay,

    /// <summary>
    /// Data flows in both directions between the source (the ViewModel property) and the target (the UI property).
    /// Changes in either one update the other.
    /// </summary>
    TwoWay,

    /// <summary>
    /// Data flows from the target (the UI property) to the source (the ViewModel property) only.
    /// Changes in the target update the source, but changes in the source do not update the target.
    /// </summary>
    OneWayToSource
}

/// <summary>
/// Defines when the source (the ViewModel property) is updated in a binding.
/// </summary>
public enum UpdateSourceTrigger
{
    /// <summary>
    /// Uses the default update behavior for the target (the UI property).
    /// The default behavior depends on the type of control being bound.
    /// </summary>
    Default,

    /// <summary>
    /// Updates the source (the ViewModel property) immediately whenever the target (the UI property) changes.
    /// For example, in a TextBox, the source updates with every keystroke.
    /// </summary>
    PropertyChanged,

    /// <summary>
    /// Updates the source (the ViewModel property) when the control that owns the target (the UI property) loses focus.
    /// For example, in a TextBox, the source updates only when the user clicks outside the TextBox
    /// or tabs to another control, not during typing.
    /// </summary>
    LostFocus,
}

/// <summary>
/// Provides a way to transform values as they flow between the source (the ViewModel property) and the target (the UI property) in a binding.
/// Implement this interface to create custom converters for data transformations.
/// </summary>
/// <example>
/// A converter that transforms a boolean to a color:
/// <code>
/// public class BoolToColorConverter : IValueConverter
/// {
///     public object? Convert(object? value, Type targetType, object? parameter)
///     {
///         if (value is bool isActive)
///             return isActive ? Color.Green : Color.Red;
///         return Color.Gray;
///     }
///
///     public object? ConvertBack(object? value, Type sourceType, object? parameter)
///     {
///         // Not typically implemented for one-way conversions like color
///         return Binding.DoNothing;
///     }
/// }
/// </code>
/// </example>
public interface IValueConverter
{
    /// <summary>
    /// Converts a value from the source (the ViewModel property) to the target (the UI property).
    /// This is called when data flows from source to target.
    /// </summary>
    /// <param name="value">The value of the source (the ViewModel property).</param>
    /// <param name="targetType">The type of the target (the UI property).</param>
    /// <param name="parameter">An optional parameter from <see cref="Binding.ConverterParameter"/>.</param>
    /// <returns>The converted value to set on the target (the UI property), or <see cref="Binding.DoNothing"/> to skip the update.</returns>
    object? Convert(object? value, Type targetType, object? parameter);

    /// <summary>
    /// Converts a value from the target (the UI property) back to the source (the ViewModel property).
    /// This is called when data flows from target to source in TwoWay or OneWayToSource bindings.
    /// </summary>
    /// <param name="value">The value of the target (the UI property).</param>
    /// <param name="sourceType">The type of the source (the ViewModel property).</param>
    /// <param name="parameter">An optional parameter from <see cref="Binding.ConverterParameter"/>.</param>
    /// <returns>The converted value to set on the source (the ViewModel property), or <see cref="Binding.DoNothing"/> to skip the update.</returns>
    object? ConvertBack(object? value, Type sourceType, object? parameter);
}

/// <summary>
/// Represents a data binding between a source (the ViewModel property) and a target (the UI property).
/// </summary>
/// <param name="path">The path of the source (the ViewModel property) to bind to.</param>
public class Binding(string path)
{
    /// <summary>
    /// A sentinel value that indicates no action should be taken during conversion.
    /// Return this from a converter to prevent updating the target (the UI property) or source (the ViewModel property).
    /// </summary>
    public static object DoNothing { get; } = new();

    /// <summary>
    /// Project-configurable defaults for <see cref="UpdateSourceTrigger.Default"/>, keyed by the
    /// control type and the name of the target (the UI property). Lets a project opt every binding of a given
    /// control/property pair into a trigger (e.g. <c>LostFocus</c> for every <c>TextBox.Text</c>)
    /// without setting <see cref="UpdateSourceTrigger"/> on each individual <see cref="Binding"/>.
    /// </summary>
    /// <remarks>
    /// Empty by default, so registering nothing reproduces today's behavior exactly (falls back to
    /// <see cref="Data.UpdateSourceTrigger.PropertyChanged"/>). A binding's own explicit
    /// <see cref="UpdateSourceTrigger"/> always takes precedence over anything registered here.
    /// Lookup walks up from the control's runtime type through its base types, so registering
    /// against a base control type (e.g. <c>TextBoxBase</c>) applies to all its subclasses unless a
    /// more derived type has its own registration.
    /// </remarks>
    /// <example>
    /// <code>
    /// Binding.DefaultUpdateTriggers[(typeof(TextBox), nameof(TextBox.Text))] = UpdateSourceTrigger.LostFocus;
    /// </code>
    /// </example>
    public static Dictionary<(Type ControlType, string PropertyName), UpdateSourceTrigger> DefaultUpdateTriggers { get; } = new();

    /// <summary>
    /// Resolves <see cref="Data.UpdateSourceTrigger.Default"/> to a concrete trigger for the given
    /// control type and property, consulting <see cref="DefaultUpdateTriggers"/> before falling back
    /// to <see cref="Data.UpdateSourceTrigger.PropertyChanged"/>.
    /// </summary>
    internal static UpdateSourceTrigger ResolveDefaultUpdateTrigger(Type controlType, string propertyName)
    {
        for (Type? type = controlType; type is not null; type = type.BaseType)
        {
            if (DefaultUpdateTriggers.TryGetValue((type, propertyName), out UpdateSourceTrigger trigger))
            {
                return trigger;
            }
        }

        return UpdateSourceTrigger.PropertyChanged;
    }

    /// <summary>
    /// Creates a binding using a lambda expression to specify the property path.
    /// This provides compile-time safety and refactoring support compared to using a string path.
    /// </summary>
    /// <param name="propertyExpression">A lambda expression that references the property to bind to.
    /// For example: <c>vm => vm.UserName</c> or <c>vm => vm.Address.City</c>.</param>
    public Binding(LambdaExpression propertyExpression) : this(BinderHelpers.ExtractPath(propertyExpression)) { }

    /// <summary>
    /// Gets the path of the source (the ViewModel property) to bind to.
    /// For example, "UserName" or "Address.City".
    /// </summary>
    public string Path { get; } = path;

    private readonly BindingMode? _mode;

    /// <summary>
    /// Gets or sets the direction of data flow in the binding. When it is not set, a binding on a Forms control is
    /// <see cref="BindingMode.TwoWay"/>, and a binding on a visual is <see cref="BindingMode.OneWay"/> except that
    /// the visual's PushValueToViewModel still writes to the source. Reads as TwoWay when not set.
    /// </summary>
    public BindingMode Mode
    {
        get => _mode ?? BindingMode.TwoWay;
        init => _mode = value;
    }

    /// <summary>Whether <see cref="Mode"/> was set, rather than left to the target's default.</summary>
    internal bool IsModeSet => _mode.HasValue;

    /// <summary>
    /// Gets or sets when the source (the ViewModel property) is updated in a binding.
    /// Default is <see cref="Data.UpdateSourceTrigger.Default"/>.
    /// </summary>
    public UpdateSourceTrigger UpdateSourceTrigger { get; init; } = UpdateSourceTrigger.Default;

    /// <summary>
    /// Gets or sets the value to use when the binding cannot retrieve a value from the source (the ViewModel property).
    /// For example, if binding to a property that doesn't exist or if the source is null,
    /// the FallbackValue will be displayed in the target (the UI property) instead.
    /// </summary>
    public object? FallbackValue { get; init; }

    /// <summary>
    /// Gets or sets the value to use when the source (the ViewModel property) value is null.
    /// For example, you might set TargetNullValue to "(none)" to display in a TextBox
    /// when the bound property is null, instead of showing an empty string.
    /// </summary>
    public object? TargetNullValue { get; init; }

    /// <summary>
    /// Gets or sets a converter to transform values between the source (the ViewModel property) and the target (the UI property).
    /// For example, a converter might transform a boolean to a color (true = green, false = red)
    /// or convert a DateTime to a formatted string.
    /// </summary>
    public IValueConverter? Converter { get; init; }

    /// <summary>
    /// Gets or sets an optional parameter to pass to the <see cref="Converter"/>.
    /// This can be used to customize the converter's behavior without creating multiple converter classes.
    /// </summary>
    public object? ConverterParameter { get; init; }

    /// <summary>
    /// Gets or sets a format string to apply to the value.
    /// For example, "{0:C}" formats a number as currency, or "{0:MM/dd/yyyy}" formats a DateTime.
    /// </summary>
    public string? StringFormat { get; init; }
}

/// <summary>
/// A strongly-typed binding that provides compile-time type safety for the ViewModel.
/// This class exists to allow creating bindings with a typed lambda expression without casting,
/// making the code more readable and type-safe.
/// </summary>
/// <typeparam name="T">The ViewModel type, which declares the source (the ViewModel property).</typeparam>
/// <param name="propertyExpression">A lambda expression that references the property to bind to.
/// For example: <c>new Binding&lt;MyViewModel&gt;(vm => vm.UserName)</c>.</param>
public class Binding<T>(Expression<Func<T, object?>> propertyExpression)
    : Binding(BinderHelpers.ExtractPath(propertyExpression));
