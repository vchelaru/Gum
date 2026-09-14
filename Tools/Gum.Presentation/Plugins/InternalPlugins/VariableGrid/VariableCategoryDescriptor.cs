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
    /// default/no color. See <see cref="GetHeaderColor"/>.
    /// </summary>
    public string? HeaderColorHex { get; set; }

    /// <summary>
    /// <see cref="HeaderColorHex"/> as a color: "#AARRGGBB" or "#RRGGBB" (opaque). Null when unset or
    /// not valid hex.
    /// </summary>
    public System.Drawing.Color? GetHeaderColor()
    {
        string? hex = HeaderColorHex?.TrimStart('#');
        if (string.IsNullOrEmpty(hex) || (hex.Length != 6 && hex.Length != 8) ||
            !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out uint value))
        {
            return null;
        }

        if (hex.Length == 6)
        {
            value |= 0xFF000000;
        }

        return System.Drawing.Color.FromArgb(unchecked((int)value));
    }

    public VariableCategoryDescriptor(string name)
    {
        Name = name;
    }
}
