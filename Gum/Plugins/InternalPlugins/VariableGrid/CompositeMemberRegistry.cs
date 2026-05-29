using System;
using System.Collections.Generic;
using System.Drawing;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Supplies the set of <see cref="CompositeMemberDescriptor"/>s that the variable grid uses to collapse
/// sibling channel variables into single composite rows. Registered as an app-wide singleton; consumed by
/// the composite build pass in the variable grid.
/// </summary>
public interface ICompositeMemberRegistry
{
    /// <summary>The registered composite descriptors, evaluated in order against each category.</summary>
    IReadOnlyList<CompositeMemberDescriptor> Descriptors { get; }
}

/// <inheritdoc/>
public class CompositeMemberRegistry : ICompositeMemberRegistry
{
    /// <inheritdoc/>
    public IReadOnlyList<CompositeMemberDescriptor> Descriptors { get; }

    /// <summary>Creates the registry pre-populated with the built-in descriptors (currently color).</summary>
    public CompositeMemberRegistry()
    {
        Descriptors = new List<CompositeMemberDescriptor>
        {
            CreateColorDescriptor(),
        };
    }

    private CompositeMemberDescriptor CreateColorDescriptor()
    {
        // Alpha is deliberately excluded: it stays an independent row so it can be animated separately
        // from color. Compose builds an opaque color; Decompose writes only R/G/B.
        return new CompositeMemberDescriptor(
            ChannelRootNames: new[] { "Red", "Green", "Blue" },
            CompositeNameFormat: "{prefix}Color{suffix}",
            Displayer: typeof(Gum.Controls.DataUi.ColorDisplay),
            CompositeType: typeof(Color),
            Compose: ComposeColor,
            Decompose: DecomposeColor);
    }

    private object ComposeColor(IReadOnlyList<object?> channels)
    {
        int red = ToChannelByte(channels[0]);
        int green = ToChannelByte(channels[1]);
        int blue = ToChannelByte(channels[2]);
        return Color.FromArgb(red, green, blue);
    }

    private object?[] DecomposeColor(object value)
    {
        Color color = (Color)value;
        return new object?[] { (int)color.R, (int)color.G, (int)color.B };
    }

    private int ToChannelByte(object? value)
    {
        int raw = value is int asInt ? asInt : 0;
        return Math.Max(0, Math.Min(255, raw));
    }
}
