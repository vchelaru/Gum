using System;
using System.Collections.Generic;
using System.Drawing;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc cref="ICompositeMemberRegistry"/>
public class CompositeMemberRegistry : ICompositeMemberRegistry
{
    /// <inheritdoc/>
    public IReadOnlyList<CompositeMemberDescriptor> Descriptors { get; }

    /// <summary>Creates the registry pre-populated with the built-in descriptors (color, corner radius).</summary>
    public CompositeMemberRegistry()
    {
        Descriptors = new List<CompositeMemberDescriptor>
        {
            CreateColorDescriptor(),
            CreateCornerRadiusDescriptor(),
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

    private CompositeMemberDescriptor CreateCornerRadiusDescriptor()
    {
        // Rectangle only - no affixed variants (no StrokeCornerRadius / CornerRadius2), so the
        // composite name format has no prefix/suffix content beyond the token itself.
        return new CompositeMemberDescriptor(
            ChannelRootNames: new[]
            {
                "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
                "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
            },
            CompositeNameFormat: "{prefix}CornerRadius{suffix}",
            Displayer: typeof(Gum.Controls.DataUi.CornerRadiusDisplay),
            CompositeType: typeof(CornerRadiusComposite),
            Compose: ComposeCornerRadius,
            Decompose: DecomposeCornerRadius);
    }

    private object ComposeCornerRadius(IReadOnlyList<object?> channels)
    {
        float uniform = ToFloat(channels[0]);
        return new CornerRadiusComposite(
            uniform,
            ToNullableFloat(channels[1]),
            ToNullableFloat(channels[2]),
            ToNullableFloat(channels[3]),
            ToNullableFloat(channels[4]));
    }

    private object?[] DecomposeCornerRadius(object value)
    {
        CornerRadiusComposite composite = (CornerRadiusComposite)value;
        return new object?[] { composite.Uniform, composite.TopLeft, composite.TopRight, composite.BottomLeft, composite.BottomRight };
    }

    private float ToFloat(object? value) => value is float asFloat ? asFloat : 0f;

    private float? ToNullableFloat(object? value) => value as float?;
}
