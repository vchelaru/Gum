using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Supplies the set of <see cref="CompositeMemberDescriptor"/>s that the variable grid uses to collapse
/// sibling channel variables into single composite rows. Registered as an app-wide singleton; consumed by
/// the composite build pass in the variable grid. See
/// <see cref="Gum.Plugins.InternalPlugins.VariableGrid.CompositeMemberRegistry"/> for the concrete
/// implementation (tool project) - it stays tool-side because it references the WPF displayer control
/// types (<c>ColorDisplay</c>, <c>CornerRadiusDisplay</c>) used to render each composite.
/// </summary>
public interface ICompositeMemberRegistry
{
    /// <summary>The registered composite descriptors, evaluated in order against each category.</summary>
    IReadOnlyList<CompositeMemberDescriptor> Descriptors { get; }
}
