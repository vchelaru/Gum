using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Headless heir to <c>WpfDataUi.DataTypes.MemberCategory</c> - a named group of
/// <see cref="VariableGridEntry"/> rows for the Variables tab. A WPF-side mapper
/// (<c>PropertyGridManager.ToWpf</c>) materializes each descriptor into a real
/// <c>MemberCategory</c> wrapping <c>StateReferencingInstanceMember</c> adapters. See ADR-0005 and
/// the "ui-decoupling-plan.md" known-gotchas list.
/// </summary>
public class VariableCategoryDescriptor
{
    public string Name { get; }

    public List<VariableGridEntry> Members { get; } = new();

    /// <summary>
    /// The category header color as a hex string (e.g. <c>"#204300FF"</c>), or null for the
    /// default/no color. Kept as a hex string rather than a WPF <c>Brush</c> so this class stays
    /// headless; the WPF mapper converts it via <c>BrushConverter</c>.
    /// </summary>
    public string? HeaderColorHex { get; set; }

    public VariableCategoryDescriptor(string name)
    {
        Name = name;
    }
}
