using System;
using System.Collections.Generic;
using System.Drawing;
using GumRuntime;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc cref="ICompositeMemberRegistry"/>
public class CompositeMemberRegistry : ICompositeMemberRegistry
{
    /// <inheritdoc/>
    public IReadOnlyList<CompositeMemberDescriptor> Descriptors { get; }

    /// <summary>Creates the registry pre-populated with the built-in descriptors (color, corner radius, border scale).</summary>
    public CompositeMemberRegistry()
    {
        Descriptors = new List<CompositeMemberDescriptor>
        {
            CreateColorDescriptor(),
            CreateCornerRadiusDescriptor(),
            CreateBorderScaleDescriptor(),
        };
    }

    private CompositeMemberDescriptor CreateColorDescriptor()
    {
        // Alpha is deliberately excluded: it stays an independent row so it can be animated separately
        // from color. Compose builds an opaque color; Decompose writes only R/G/B.
        // Channel names are sourced from ElementSaveExtensions' composite-reference table (GumRuntime)
        // so this UI-facing registry and apply-time reference expansion can't drift apart.
        return new CompositeMemberDescriptor(
            ChannelRootNames: ElementSaveExtensions.GetCompositeChannelRootNames("Color")!,
            CompositeNameFormat: "{prefix}Color{suffix}",
            Displayer: typeof(GumDisplayers.Color),
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
            ChannelRootNames: ElementSaveExtensions.GetCompositeChannelRootNames("CornerRadius")!,
            CompositeNameFormat: "{prefix}CornerRadius{suffix}",
            Displayer: typeof(GumDisplayers.CornerRadius),
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
        return DecomposeUniformWithOverrides(
            composite.Uniform, composite.TopLeft, composite.TopRight, composite.BottomLeft, composite.BottomRight);
    }

    private CompositeMemberDescriptor CreateBorderScaleDescriptor()
    {
        // NineSlice only; same shape as the corner radius: a uniform value plus four optional overrides.
        return new CompositeMemberDescriptor(
            ChannelRootNames: ElementSaveExtensions.GetCompositeChannelRootNames("BorderScale")!,
            CompositeNameFormat: "{prefix}BorderScale{suffix}",
            Displayer: typeof(GumDisplayers.BorderScale),
            CompositeType: typeof(BorderScaleComposite),
            Compose: ComposeBorderScale,
            Decompose: DecomposeBorderScale);
    }

    private object ComposeBorderScale(IReadOnlyList<object?> channels)
    {
        return new BorderScaleComposite(
            ToFloat(channels[0]),
            ToNullableFloat(channels[1]),
            ToNullableFloat(channels[2]),
            ToNullableFloat(channels[3]),
            ToNullableFloat(channels[4]));
    }

    private object?[] DecomposeBorderScale(object value)
    {
        BorderScaleComposite composite = (BorderScaleComposite)value;
        return DecomposeUniformWithOverrides(
            composite.Uniform, composite.Left, composite.Top, composite.Right, composite.Bottom);
    }

    // The channels of a composite that is one value plus four optional overrides; negatives floor at 0.
    private static object?[] DecomposeUniformWithOverrides(float uniform, float? first, float? second, float? third, float? fourth)
    {
        return new object?[]
        {
            ClampToZeroMin(uniform),
            ClampNullableToZeroMin(first),
            ClampNullableToZeroMin(second),
            ClampNullableToZeroMin(third),
            ClampNullableToZeroMin(fourth)
        };
    }

    private static float ClampToZeroMin(float value) => Math.Max(0f, value);

    private static float? ClampNullableToZeroMin(float? value) => value.HasValue ? Math.Max(0f, value.Value) : null;

    private float ToFloat(object? value) => value is float asFloat ? asFloat : 0f;

    private float? ToNullableFloat(object? value) => value as float?;
}
