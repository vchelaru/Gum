using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;

namespace Gum.Forms;

/// <summary>
/// Applies design-time values for <see cref="DataTypes.Behaviors.BehaviorSave.FormsProperties"/>
/// declarations onto a <see cref="FrameworkElement"/> when its Visual is attached. This is the
/// runtime half of the Forms property promotion feature: design values authored in the Gum tool
/// flow through the wrapped Forms control's properties via reflection.
/// </summary>
internal static class BehaviorFormsPropertyApplier
{
    public static void Apply(FrameworkElement formsControl, GraphicalUiElement visual)
    {
        ElementSave? elementSave = visual?.ElementSave;
        if (elementSave == null)
        {
            return;
        }

        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return;
        }

        Type formsType = formsControl.GetType();

        foreach (VariableSave declaration in EnumerateFormsPropertyDeclarations(elementSave, project))
        {
            if (string.IsNullOrEmpty(declaration.Name))
            {
                continue;
            }

            object? value = new RecursiveVariableFinder(elementSave.DefaultState).GetValue(declaration.Name);
            if (value == null)
            {
                continue;
            }

            PropertyInfo? prop = formsType.GetProperty(declaration.Name);
            if (prop == null || !prop.CanWrite)
            {
                continue;
            }

            try
            {
                object coerced = prop.PropertyType.IsAssignableFrom(value.GetType())
                    ? value
                    : Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                prop.SetValue(formsControl, coerced);
            }
            catch (Exception)
            {
                // Coercion or set failed; the FormsProperty type doesn't line up with the
                // FrameworkElement's property. Silently skip rather than crash construction.
            }
        }
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
            current = ObjectFinder.Self.GetElementSave(current.BaseType);
        }
    }
}
