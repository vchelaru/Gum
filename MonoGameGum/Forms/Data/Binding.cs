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
    /// Data flows from the source to the target only.
    /// Changes in the source (usually the ViewModel) update the target (usually the View),
    /// but changes in the target do not update the source.
    /// </summary>
    OneWay,

    /// <summary>
    /// Data flows in both directions between the source and target.
    /// Changes in either the source (usually the ViewModel) or target (usually the View) update the other.
    /// </summary>
    TwoWay,

    /// <summary>
    /// Data flows from the target to the source only.
    /// Changes in the target (usually the View) update the source (usually the ViewModel),
    /// but changes in the source do not update the target.
    /// </summary>
    OneWayToSource
}

/// <summary>
/// Defines when the source (usually the ViewModel) is updated in a binding.
/// </summary>
public enum UpdateSourceTrigger
{
    /// <summary>
    /// Uses the default update behavior for the target property.
    /// The default behavior depends on the type of control being bound.
    /// </summary>
    Default,

    /// <summary>
    /// Updates the source immediately whenever the target property value changes.
    /// For example, in a TextBox, the source updates with every keystroke.
    /// </summary>
    PropertyChanged,

    /// <summary>
    /// Updates the source when the target control loses focus.
    /// For example, in a TextBox, the source updates only when the user clicks outside the TextBox
    /// or tabs to another control, not during typing.
    /// </summary>
    LostFocus,
}

/// <summary>
/// Provides a way to transform values as they flow between the source and target in a binding.
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
    /// Converts a value from the source (usually ViewModel) to the target (usually View).
    /// This is called when data flows from source to target.
    /// </summary>
    /// <param name="value">The value from the source property.</param>
    /// <param name="targetType">The type of the target property.</param>
    /// <param name="parameter">An optional parameter from <see cref="Binding.ConverterParameter"/>.</param>
    /// <returns>The converted value to set on the target property, or <see cref="Binding.DoNothing"/> to skip the update.</returns>
    object? Convert(object? value, Type targetType, object? parameter);

    /// <summary>
    /// Converts a value from the target (usually View) back to the source (usually ViewModel).
    /// This is called when data flows from target to source in TwoWay or OneWayToSource bindings.
    /// </summary>
    /// <param name="value">The value from the target property.</param>
    /// <param name="sourceType">The type of the source property.</param>
    /// <param name="parameter">An optional parameter from <see cref="Binding.ConverterParameter"/>.</param>
    /// <returns>The converted value to set on the source property, or <see cref="Binding.DoNothing"/> to skip the update.</returns>
    object? ConvertBack(object? value, Type sourceType, object? parameter);
}

/// <summary>
/// Represents a data binding between a source property (usually on a ViewModel) and a target property (usually on a View control).
/// </summary>
/// <param name="path">The property path on the source object to bind to.</param>
public class Binding(string path)
{
    /// <summary>
    /// A sentinel value that indicates no action should be taken during conversion.
    /// Return this from a converter to prevent updating the target or source.
    /// </summary>
    public static object DoNothing { get; } = new();

    /// <summary>
    /// Creates a binding using a lambda expression to specify the property path.
    /// This provides compile-time safety and refactoring support compared to using a string path.
    /// </summary>
    /// <param name="propertyExpression">A lambda expression that references the property to bind to.
    /// For example: <c>vm => vm.UserName</c> or <c>vm => vm.Address.City</c>.</param>
    public Binding(LambdaExpression propertyExpression) : this(BinderHelpers.ExtractPath(propertyExpression)) { }

    /// <summary>
    /// Gets the property path on the source object to bind to.
    /// For example, "UserName" or "Address.City".
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Gets or sets the direction of data flow in the binding.
    /// Default is <see cref="BindingMode.TwoWay"/>.
    /// </summary>
    public BindingMode Mode { get; init; } = BindingMode.TwoWay;

    /// <summary>
    /// Gets or sets when the source is updated in a binding.
    /// Default is <see cref="Data.UpdateSourceTrigger.Default"/>.
    /// </summary>
    public UpdateSourceTrigger UpdateSourceTrigger { get; init; } = UpdateSourceTrigger.Default;

    /// <summary>
    /// Gets or sets the value to use when the binding cannot retrieve a value from the source.
    /// For example, if binding to a property that doesn't exist or if the source is null,
    /// the FallbackValue will be displayed in the target control instead.
    /// </summary>
    public object? FallbackValue { get; init; }

    /// <summary>
    /// Gets or sets the value to use when the source property value is null.
    /// For example, you might set TargetNullValue to "(none)" to display in a TextBox
    /// when the bound property is null, instead of showing an empty string.
    /// </summary>
    public object? TargetNullValue { get; init; }

    /// <summary>
    /// Gets or sets a converter to transform values between the source and target.
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
/// A strongly-typed binding that provides compile-time type safety for the source object.
/// This class exists to allow creating bindings with a typed lambda expression without casting,
/// making the code more readable and type-safe.
/// </summary>
/// <typeparam name="T">The type of the source object (usually the ViewModel type).</typeparam>
/// <param name="propertyExpression">A lambda expression that references the property to bind to.
/// For example: <c>new Binding&lt;MyViewModel&gt;(vm => vm.UserName)</c>.</param>
public class Binding<T>(Expression<Func<T, object?>> propertyExpression)
    : Binding(BinderHelpers.ExtractPath(propertyExpression));
