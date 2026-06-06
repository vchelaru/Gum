using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Declares how a set of sibling "channel" variables (matched by their root names) collapse into a
/// single composite row in the variable grid, and how to compose/decompose between the channel values
/// and the composite value. Color is the first consumer (Red/Green/Blue -&gt; a single swatch); other
/// composites (e.g. Vector2) can be added by registering another descriptor with no new control flow.
/// </summary>
/// <param name="ChannelRootNames">
/// The root variable names that form the composite, in compose/decompose order (e.g. Red, Green, Blue).
/// Matched against each variable's root via <c>ObjectFinder.GetRootVariable</c>, never by display-name text.
/// </param>
/// <param name="CompositeNameFormat">
/// The composite member name template, where <c>{prefix}</c>/<c>{suffix}</c> are the affix surrounding the
/// channel root name in the underlying variable name (e.g. <c>Stroke</c> from <c>StrokeRed</c>).
/// </param>
/// <param name="Displayer">The WPF control type used to render the composite row.</param>
/// <param name="CompositeType">The type of the composed value (used to select/feed the displayer).</param>
/// <param name="Compose">Builds the composite value from the channel values, in <see cref="ChannelRootNames"/> order.</param>
/// <param name="Decompose">Splits the composite value back into one value per channel, in <see cref="ChannelRootNames"/> order.</param>
public record CompositeMemberDescriptor(
    string[] ChannelRootNames,
    string CompositeNameFormat,
    Type Displayer,
    Type CompositeType,
    Func<IReadOnlyList<object?>, object> Compose,
    Func<object, object?[]> Decompose)
{
    /// <summary>
    /// The literal token at the heart of <see cref="CompositeNameFormat"/> — the format with the
    /// <c>{prefix}</c>/<c>{suffix}</c> placeholders removed (e.g. <c>Color</c> from <c>{prefix}Color{suffix}</c>).
    /// </summary>
    private string CompositeToken => CompositeNameFormat
        .Replace("{prefix}", string.Empty)
        .Replace("{suffix}", string.Empty);

    /// <summary>
    /// If <paramref name="compositeName"/> is an instance of this descriptor's name format — the
    /// <see cref="CompositeToken"/> surrounded by some prefix/suffix (e.g. <c>StrokeColor</c>, <c>Color2</c>) —
    /// outputs the matching channel variable names in <see cref="ChannelRootNames"/> order (e.g.
    /// <c>StrokeColor</c> -&gt; StrokeRed/StrokeGreen/StrokeBlue) and returns true; otherwise returns false.
    /// This is the inverse of how <c>CompositeMemberLogic</c> builds the composite name from the channels.
    /// </summary>
    public bool TryGetChannelNames(string compositeName, out IReadOnlyList<string> channelNames)
    {
        string compositeToken = CompositeToken;
        int index = compositeName.IndexOf(compositeToken, StringComparison.Ordinal);
        if (index < 0)
        {
            channelNames = Array.Empty<string>();
            return false;
        }

        string prefix = compositeName.Substring(0, index);
        string suffix = compositeName.Substring(index + compositeToken.Length);

        channelNames = ChannelRootNames.Select(token => prefix + token + suffix).ToArray();
        return true;
    }
}
