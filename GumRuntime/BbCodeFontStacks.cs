using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gum.DataTypes.Variables;

namespace Gum.Wireframe;

/// <summary>
/// The stack model that resolves which font a BBCode run uses: each open font tag
/// (<c>Font</c>, <c>FontSize</c>, <c>OutlineThickness</c>, <c>IsItalic</c>, <c>IsBold</c>,
/// <c>HasDropshadow</c>) pushes its value and each close tag pops it, so a run resolves to the font
/// implied by every tag open over it. Shared by the renderers (<c>CustomSetPropertyOnRenderable</c>
/// resolves each run's font while drawing) and by <c>FontReferenceCollector</c> (bakes those fonts
/// ahead of time), so the two cannot disagree about which fonts a text needs.
/// </summary>
public class BbCodeFontStacks
{
    public Stack<string?> FontName { get; } = new Stack<string?>();
    public Stack<float> FontSize { get; } = new Stack<float>();
    public Stack<int> OutlineThickness { get; } = new Stack<int>();
    public Stack<bool> UseFontSmoothing { get; } = new Stack<bool>();
    public Stack<bool> IsItalic { get; } = new Stack<bool>();
    public Stack<bool> IsBold { get; } = new Stack<bool>();
    public Stack<bool> UseCustomFont { get; } = new Stack<bool>();
    public Stack<bool> HasDropshadow { get; } = new Stack<bool>();

    // BbCodeParser recognizes tag names ignoring case and carries a name as written, but callers switch
    // on the PascalCase variable names (also the InlineVariable names the renderers read), so a
    // lowercase [color=Red] was stripped and never applied (#7106).
    private static readonly Dictionary<string, string> CanonicalTagNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Red"] = "Red",
            ["Green"] = "Green",
            ["Blue"] = "Blue",
            ["Color"] = "Color",
            ["FontScale"] = "FontScale",
            ["Custom"] = "Custom",
            ["State"] = "State",
            ["Font"] = "Font",
            ["FontSize"] = "FontSize",
            ["OutlineThickness"] = "OutlineThickness",
            ["IsItalic"] = "IsItalic",
            ["IsBold"] = "IsBold",
            ["HasDropshadow"] = "HasDropshadow",
        };

    public static string CanonicalTagName(string name) =>
        CanonicalTagNames.TryGetValue(name, out string? canonicalName) ? canonicalName : name;

    /// <summary>
    /// Replaces every stack with the base text's own font values, so the first run starts from them.
    /// </summary>
    public void Seed(string? fontName, float fontSize, int outlineThickness, bool useFontSmoothing,
        bool isItalic, bool isBold, bool useCustomFont, bool hasDropshadow)
    {
        FontName.Clear();
        FontName.Push(fontName);
        FontSize.Clear();
        FontSize.Push(fontSize);
        OutlineThickness.Clear();
        OutlineThickness.Push(outlineThickness);
        UseFontSmoothing.Clear();
        UseFontSmoothing.Push(useFontSmoothing);
        IsItalic.Clear();
        IsItalic.Push(isItalic);
        IsBold.Clear();
        IsBold.Push(isBold);
        UseCustomFont.Clear();
        UseCustomFont.Push(useCustomFont);
        HasDropshadow.Clear();
        HasDropshadow.Push(hasDropshadow);
    }

    /// <summary>
    /// Applies one tag. An opening tag pushes its value and a closing tag pops. An opening tag whose
    /// value does not parse pushes the enclosing value again, so it styles nothing and its closing tag
    /// restores the enclosing style, the way SkiaGum ignores such a tag. A pop never removes the seeded
    /// base value.
    /// </summary>
    /// <param name="name">The tag name, in any case.</param>
    /// <param name="argument">The tag's argument, null on a closing tag or an opening tag with no value.</param>
    /// <param name="isOpening">True for an opening tag, false for a closing tag.</param>
    /// <param name="pushed">True on an opening tag, false on a closing tag.</param>
    /// <returns>True when the tag can change the run's font: a font tag that pushed a parsed value or
    /// popped. False for any other tag and for an opening font tag whose value does not parse.</returns>
    public bool Apply(string name, string? argument, bool isOpening, out bool pushed)
    {
        pushed = isOpening;
        switch (CanonicalTagName(name))
        {
            case "Font":
                // tolerate ".fnt" suffix
                string? fontName = argument?.EndsWith(".fnt") == true
                    ? argument.Substring(0, argument.Length - ".fnt".Length)
                    : argument;
                return PushOrPop(FontName, isOpening, !string.IsNullOrEmpty(fontName), fontName);
            case "FontSize":
                return PushOrPop(FontSize, isOpening,
                    float.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedSize), parsedSize);
            case "OutlineThickness":
                return PushOrPop(OutlineThickness, isOpening,
                    int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedOutline), parsedOutline);
            case "IsItalic":
                return PushOrPop(IsItalic, isOpening, bool.TryParse(argument, out bool parsedItalic), parsedItalic);
            case "IsBold":
                return PushOrPop(IsBold, isOpening, bool.TryParse(argument, out bool parsedBold), parsedBold);
            case "UseCustomFont":
                // Never fires today (UseCustomFont is not a BbCodeParser.KnownTag).
                return PushOrPop(UseCustomFont, isOpening, bool.TryParse(argument, out bool parsedCustom), parsedCustom);
            case "HasDropshadow":
                return PushOrPop(HasDropshadow, isOpening, bool.TryParse(argument, out bool parsedDropshadow), parsedDropshadow);
            default:
                pushed = false;
                return false;
        }
    }

    /// <summary>
    /// Fills <paramref name="into"/> with every opening and closing tag of <paramref name="tags"/> in text
    /// order, the order <see cref="Apply"/> must see them in.
    /// </summary>
    public static void FillInTextOrder(List<FoundTag> tags, List<(TagInfo Tag, bool IsOpening)> into)
    {
        into.Clear();
        foreach (FoundTag tag in tags)
        {
            into.Add((tag.Open, true));
            into.Add((tag.Close, false));
        }
        into.Sort((a, b) => a.Tag.StartIndex - b.Tag.StartIndex);
    }

    /// <summary>
    /// Parses a <c>[Color=..]</c> argument: <c>0xAARRGGBB</c> hex or a known color name (<c>Red</c>,
    /// <c>CornflowerBlue</c>, ...). Returns null, so the run keeps the base color, for anything else.
    /// Every runtime calls this, so they agree on which values are colors.
    /// </summary>
    public static System.Drawing.Color? ParseColor(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return null;
        }

        if (argument!.StartsWith("0x"))
        {
            return int.TryParse(argument.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int hex)
                ? System.Drawing.Color.FromArgb(hex)
                : null;
        }

        // FromName returns a non-known color with ARGB 0 for an unrecognized name, which would hide the run.
        System.Drawing.Color named = System.Drawing.Color.FromName(argument);
        return named.IsKnownColor ? named : null;
    }

    /// <summary>
    /// Variable names a <c>[state=Name]</c> BBCode tag is allowed to apply to the substring it wraps.
    /// A state can hold arbitrary variables (X, Y, Width, etc.), but only these have per-run meaning, so
    /// anything else on the state is silently skipped rather than applied to the whole element.
    /// </summary>
    public static readonly HashSet<string> StateTagVariableAllowlist = new HashSet<string>
    {
        "Color", "Red", "Green", "Blue", "FontScale",
        "Font", "FontSize", "OutlineThickness", "IsItalic", "IsBold", "UseCustomFont", "HasDropshadow",
    };

    /// <summary>
    /// Formats an already-typed <see cref="VariableSave.Value"/> back into the string
    /// <see cref="TagInfo.Argument"/> form the per-tag parsing expects, so a decomposed state variable can
    /// be re-dispatched through that unchanged parsing. Lossless for every allowlisted type: bool/int/float
    /// format invariantly, and Color round-trips through 8-digit ARGB hex (not
    /// <see cref="System.Drawing.Color.Name"/>, which is lossy for non-named colors).
    /// </summary>
    public static string? FormatStateVariableArgument(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is System.Drawing.Color color)
        {
            return "0x" + color.ToArgb().ToString("X8", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Expands every <c>[state=Name]</c> <see cref="FoundTag"/> in <paramref name="results"/> into one
    /// synthetic <see cref="FoundTag"/> per allowlisted variable on the named state, each spanning the
    /// state tag's own open/close range. Shared by the renderers (which resolve the state on the Text's
    /// <c>GraphicalUiElement</c>) and by <c>FontReferenceCollector</c> (which resolves it on the Text's
    /// element), so both see the same font tags. An unknown state name, or a state with no allowlisted
    /// variables, contributes nothing - a no-op, not an error. State variables are never themselves
    /// expanded, so a state cannot apply another state. Must run AFTER <see cref="BbCodeParser.RemoveTags"/>
    /// has stripped the source text using the original (unexpanded) <paramref name="results"/>: the
    /// synthetic entries share character ranges.
    /// </summary>
    /// <param name="results">The parsed tags of the Text value.</param>
    /// <param name="findState">Resolves a state by its bare name, or null when there is none.</param>
    public static List<FoundTag> ExpandStateTags(List<FoundTag> results, Func<string, StateSave?> findState)
    {
        List<FoundTag>? expanded = null;

        foreach (FoundTag tag in results)
        {
            if (CanonicalTagName(tag.Name) != "State")
            {
                continue;
            }

            expanded ??= new List<FoundTag>(results.Where(item => CanonicalTagName(item.Name) != "State"));

            StateSave? state = findState(tag.Open.Argument ?? string.Empty);
            if (state == null)
            {
                continue;
            }

            foreach (VariableSave variable in state.Variables)
            {
                if (variable.SetsValue && StateTagVariableAllowlist.Contains(variable.Name))
                {
                    string? argument = FormatStateVariableArgument(variable.Value);
                    if (argument != null)
                    {
                        expanded.Add(new FoundTag
                        {
                            Name = variable.Name,
                            Open = new TagInfo
                            {
                                StartIndex = tag.Open.StartIndex,
                                Count = tag.Open.Count,
                                StartStrippedIndex = tag.Open.StartStrippedIndex,
                                Argument = argument,
                                Name = variable.Name,
                            },
                            Close = new TagInfo
                            {
                                StartIndex = tag.Close.StartIndex,
                                Count = tag.Close.Count,
                                StartStrippedIndex = tag.Close.StartStrippedIndex,
                                Name = variable.Name,
                            },
                        });
                    }
                }
            }
        }

        return expanded ?? results;
    }

    // Returns whether the font can have changed; an unparseable opening tag re-pushes the enclosing value.
    private static bool PushOrPop<T>(Stack<T> stack, bool isOpening, bool parsed, T value)
    {
        if (!isOpening)
        {
            Pop(stack);
            return true;
        }

        stack.Push(parsed ? value : stack.Peek());
        return parsed;
    }

    // A pop with only the seeded base value left would empty the stack and make the next Peek throw.
    private static void Pop<T>(Stack<T> stack)
    {
        if (stack.Count > 1)
        {
            stack.Pop();
        }
    }
}
