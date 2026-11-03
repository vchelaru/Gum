using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The one Variables tab row of a float vector shader parameter (<c>Tint</c>): its X, Y, Z and W fields sit side by
/// side, and the row is set, made default and (later) exposed as a whole. Each field is still the ordinary row of one
/// saved component variable, so editing one saves every component of the parameter as one undo step.
/// </summary>
public static class ShaderVectorRow
{
    /// <summary>Creates the row over <paramref name="components"/>, one per component in X, Y, Z, W order.</summary>
    /// <param name="parameterName">The shader parameter's name, shown as the row's label.</param>
    /// <param name="components">The component rows; the first one's reset clears the whole parameter.</param>
    /// <param name="undoManager">Coalesces what a menu command changes into one undo step.</param>
    public static CompositeInstanceMember Create(string parameterName, IReadOnlyList<InstanceMember> components, IUndoManager undoManager)
    {
        CompositeInstanceMember row = new CompositeInstanceMember(
            parameterName,
            components,
            typeof(float[]),
            values => values.Select(value => value is float component ? component : 0f).ToArray(),
            value => ((float[])value).Select(component => (object?)component).ToArray())
        {
            PreferredDisplayer = typeof(GumDisplayers.ShaderVector),
            DisplayName = parameterName,
            // The reset below clears the whole parameter, so it is the row's own command rather than the per-member one.
            SupportsMakeDefault = false,
        };

        row.ContextMenuEvents.Add("Make Default", (_, _) => MakeDefault(components, undoManager));
        return row;
    }

    private static void MakeDefault(IReadOnlyList<InstanceMember> components, IUndoManager undoManager)
    {
        // A component's Make Default clears every component of the parameter (and what the legacy text holds), so one
        // that is not saved yet is skipped: a definition that saves only some components still resets.
        InstanceMember? saved = components.FirstOrDefault(component => !component.IsDefault);
        if (saved == null)
        {
            return;
        }

        using IDisposable undoLock = undoManager.RequestLock();
        saved.IsDefault = true;
    }
}
