using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;

namespace Gum.Forms;

/// <summary>
/// Applies design-time values for <see cref="DataTypes.Behaviors.BehaviorSave.FormsProperties"/>
/// declarations onto a <see cref="FrameworkElement"/> when its Visual is attached. This is the
/// runtime half of the Forms property promotion feature: design values authored in the Gum tool
/// flow through the wrapped Forms control's properties via reflection.
/// </summary>
internal static class BehaviorFormsPropertyApplier
{
    // Controls that already received the values their own build defines (see ApplyOnceAtBuild).
    private static readonly ConditionalWeakTable<FrameworkElement, object> _appliedAtBuild = new();

    /// <summary>
    /// Applies every tier the project defines to a control that a <c>DefaultFromFile*Runtime</c> just
    /// created, once, right where it is created. Code in a derived runtime's
    /// <c>AfterFullCreation</c> (after <c>base.AfterFullCreation()</c>) then runs after the defaults
    /// instead of being reset by them when the build ends. Does nothing before a project is registered
    /// with <see cref="FormsUtilities.RegisterFromFileFormRuntimeDefaults"/>.
    /// </summary>
    public static void ApplyToCreatedControl(InteractiveGue visual)
    {
        if (visual.FormsControlAsObject is FrameworkElement created && ElementSaveExtensions.InitialFormsPropertiesNotifier != null)
        {
            SnapshotInitialValues(created);
        }

        ElementSaveExtensions.InitialFormsPropertiesNotifier?.Invoke(visual, true);
    }

    /// <summary>
    /// Applies every tier to the control the first time it is called for that control, and does
    /// nothing after. The build's own pass calls this for the element it built, so a control that
    /// already got its values when it was created is not reset by it.
    /// </summary>
    public static void ApplyOnceAtBuild(FrameworkElement formsControl, GraphicalUiElement visual)
    {
        if (_appliedAtBuild.TryGetValue(formsControl, out _))
        {
            return;
        }

        if (Apply(formsControl, visual))
        {
            _appliedAtBuild.Add(formsControl, formsControl);
        }
    }

    /// <summary>
    /// Writes the FormsProperty values the project defines onto the control. By default that is
    /// every tier, which a control's own build uses once, before any code touches it. With
    /// <paramref name="authoredOverridesOnly"/> only a value its containing element explicitly
    /// authors for the instance applies, so a default never overwrites what code set since the
    /// control was built.
    /// </summary>
    /// <returns>False when the visual has no element or no project is loaded, so nothing could be applied.</returns>
    public static bool Apply(FrameworkElement formsControl, GraphicalUiElement visual, bool authoredOverridesOnly = false)
    {
        ElementSave? elementSave = visual.ElementSave;
        if (elementSave == null)
        {
            return false;
        }

        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return false;
        }

        Type formsType = formsControl.GetType();

        foreach (VariableSave declaration in EnumerateFormsPropertyDeclarations(elementSave, project))
        {
            if (string.IsNullOrEmpty(declaration.Name))
            {
                continue;
            }

            // Tier order: parent-instance override → own default → behavior FormsProperty
            // default. The third tier lets a behavior's declared default carry through when
            // neither component nor parent state authors a value (mirrors WPF
            // DependencyProperty.PropertyMetadata.DefaultValue).
            object? value = authoredOverridesOnly
                ? ReadAuthoredOverride(visual, declaration.Name)
                : ReadEffectiveValue(visual, elementSave, declaration.Name) ?? declaration.Value;
            if (value == null)
            {
                continue;
            }

            TrySet(formsControl, formsType, declaration.Name, value);
        }

        return true;
    }

    // What each property held the first time the project wrote it, so a variable the project later removes
    // (and the behavior declares no default for) returns to the control's own default.
    private static readonly ConditionalWeakTable<FrameworkElement, Dictionary<string, object?>> _initialValues = new();

    private static readonly Dictionary<Type, PropertyInfo[]> _simpleProperties = new();

    // The control's own defaults, taken before any project value is written, for a variable removed later.
    private static void SnapshotInitialValues(FrameworkElement control)
    {
        Type type = control.GetType();
        PropertyInfo[]? properties;
        lock (_simpleProperties)
        {
            if (!_simpleProperties.TryGetValue(type, out properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && IsSimple(p.PropertyType))
                    .ToArray();
                _simpleProperties[type] = properties;
            }
        }

        Dictionary<string, object?> initial = _initialValues.GetOrCreateValue(control);
        foreach (PropertyInfo property in properties)
        {
            try
            {
                initial.TryAdd(property.Name, property.GetValue(control));
            }
            catch (Exception)
            {
                // A getter that needs state the control does not have yet has no default to remember.
            }
        }
    }

    private static bool IsSimple(Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;
        return target.IsPrimitive || target.IsEnum || target == typeof(string) || target == typeof(object);
    }

    private static void TrySet(FrameworkElement formsControl, Type formsType, string name, object? value)
    {
        PropertyInfo? prop = formsType.GetProperty(name);
        if (prop == null || !prop.CanWrite)
        {
            return;
        }

        Dictionary<string, object?> initial = _initialValues.GetOrCreateValue(formsControl);
        if (value != null && !initial.ContainsKey(name))
        {
            initial[name] = prop.GetValue(formsControl);
        }

        if (value == null)
        {
            // A removed variable with no behavior default: back to what the control held before the project wrote it.
            try
            {
                // A reference or nullable property holds null until a project writes it; the snapshot may already
                // hold the component's value when the control was created after the variable was applied.
                if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                {
                    prop.SetValue(formsControl, null);
                }
                else if (initial.TryGetValue(name, out object? original))
                {
                    prop.SetValue(formsControl, original);
                }
            }
            catch (Exception)
            {
                // Same as below: a property that cannot take the value is left as it is.
            }
            return;
        }

        try
        {
            Type target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            // IsAssignableFrom preserves the boxed-enum path for state-authored values
            // from the variable grid. The IsEnum branch handles raw strings — e.g. a
            // behavior default <Value xsi:type="xsd:string">Auto</Value> from a .behx,
            // which Convert.ChangeType cannot coerce onto an enum target.
            object coerced = prop.PropertyType.IsAssignableFrom(value.GetType())
                ? value
                : target.IsEnum
                    ? Enum.Parse(target, value.ToString()!, ignoreCase: true)
                    : Convert.ChangeType(value, target);
            prop.SetValue(formsControl, coerced);
        }
        catch (Exception)
        {
            // Coercion or set failed; the FormsProperty type doesn't line up with the
            // FrameworkElement's property. Silently skip rather than crash construction.
        }
    }

    /// <summary>
    /// After a hot reload: writes onto the control each Forms property whose effective project value changed
    /// (or was removed, which returns it to the behavior default) between the version the visual was built
    /// from and the reloaded one. A property the reload left alone keeps what code set. Writing through the
    /// property also refreshes the control's own record of values it must keep across a state re-apply.
    /// </summary>
    internal static void ReapplyChanged(
        FrameworkElement formsControl, GraphicalUiElement visual, Func<string, ElementSave?> oldElement)
    {
        ElementSave? newOwn = visual.ElementSave;
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (newOwn == null || project == null)
        {
            return;
        }

        ElementSave oldOwn = oldElement(newOwn.Name) ?? newOwn;
        GraphicalUiElement? container = visual.ElementGueContainingThis as GraphicalUiElement ?? visual.Parent as GraphicalUiElement;
        ElementSave? newContainer = container?.ElementSave;
        ElementSave? oldContainer = newContainer == null ? null : oldElement(newContainer.Name) ?? newContainer;

        Type formsType = formsControl.GetType();
        foreach (VariableSave declaration in EnumerateFormsPropertyDeclarations(newOwn, project))
        {
            if (string.IsNullOrEmpty(declaration.Name))
            {
                continue;
            }

            object? oldValue = Effective(visual, oldContainer, oldOwn, declaration.Name);
            object? newValue = Effective(visual, newContainer, newOwn, declaration.Name);
            if (Equals(oldValue, newValue) || oldValue?.ToString() == newValue?.ToString())
            {
                continue;
            }

            TrySet(formsControl, formsType, declaration.Name, newValue ?? declaration.Value);
        }
    }

    private static object? Effective(GraphicalUiElement visual, ElementSave? container, ElementSave own, string propertyName)
    {
        if (container?.DefaultState != null && !string.IsNullOrEmpty(visual.Name))
        {
            // A plain lookup: the recursive finder falls through to the instance's component in the NEW project,
            // which would make the old and new values equal whenever the component is what the reload edited.
            object? overrideValue = container.DefaultState.GetValue(visual.Name + "." + propertyName);
            if (overrideValue != null)
            {
                return overrideValue;
            }
        }
        // A plain lookup, as a Forms property is not a variable of the standard element the finder knows.
        ElementSave? current = own;
        while (current != null)
        {
            object? value = current.DefaultState?.GetValue(propertyName);
            if (value != null)
            {
                return value;
            }
            current = string.IsNullOrEmpty(current.BaseType) ? null : ObjectFinder.Self.GetBaseElement(current);
        }
        return null;
    }

    private static object? ReadEffectiveValue(GraphicalUiElement visual, ElementSave elementSave, string propertyName)
    {
        // Prefer a parent-level instance-qualified override (e.g. screen state's
        // "ButtonInstance.ToolTip") over the element's own default. This matches how Gum
        // resolves instance variables at design time.
        GraphicalUiElement? container = visual.ElementGueContainingThis as GraphicalUiElement
            ?? visual.Parent as GraphicalUiElement;

        if (container?.ElementSave != null && !string.IsNullOrEmpty(visual.Name))
        {
            string qualified = visual.Name + "." + propertyName;
            object? overrideValue = new RecursiveVariableFinder(container.ElementSave.DefaultState!).GetValue(qualified);
            if (overrideValue != null)
            {
                return overrideValue;
            }
        }

        return new RecursiveVariableFinder(elementSave.DefaultState!).GetValue(propertyName)
            ?? Effective(visual, null, elementSave, propertyName);
    }

    private static object? ReadAuthoredOverride(GraphicalUiElement visual, string propertyName)
    {
        ElementSave? container = (visual.ElementGueContainingThis as GraphicalUiElement)?.ElementSave;
        if (container == null || string.IsNullOrEmpty(visual.Name))
        {
            return null;
        }

        // Only a state the containing element (or a base of it) authors counts. Unlike
        // RecursiveVariableFinder this does not fall through to the instance's own component,
        // whose values were applied when the control was built.
        string qualified = visual.Name + "." + propertyName;
        ElementSave? current = container;
        while (current != null)
        {
            object? value = current.DefaultState?.GetValue(qualified);
            if (value != null)
            {
                return value;
            }
            current = string.IsNullOrEmpty(current.BaseType) ? null : ObjectFinder.Self.GetBaseElement(current);
        }

        return null;
    }

    private static IEnumerable<VariableSave> EnumerateFormsPropertyDeclarations(
        ElementSave element, GumProjectSave project)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        ElementSave? current = element;
        while (current != null)
        {
            foreach (var behaviorRef in current.Behaviors)
            {
                var behavior = project.Behaviors.FirstOrDefault(b => b.Name == behaviorRef.BehaviorName);
                if (behavior == null)
                {
                    continue;
                }

                foreach (var formsProperty in behavior.FormsProperties)
                {
                    if (formsProperty.Name != null && seen.Add(formsProperty.Name))
                    {
                        yield return formsProperty;
                    }
                }
            }

            if (string.IsNullOrEmpty(current.BaseType))
            {
                yield break;
            }
            current = ObjectFinder.Self.GetBaseElement(current);
        }
    }
}
