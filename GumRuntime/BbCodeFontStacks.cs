using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// Applies one tag. A tag with a parseable argument pushes; one without (a closing tag, or an
    /// opening tag whose value does not parse) pops. A pop never removes the seeded base value.
    /// </summary>
    /// <param name="name">The tag name, in any case.</param>
    /// <param name="argument">The tag's argument, null on a closing tag.</param>
    /// <param name="pushed">True when the tag pushed a value, false when it popped.</param>
    /// <returns>True when the tag is one of the font tags, false when it does not touch the stacks.</returns>
    public bool Apply(string name, string? argument, out bool pushed)
    {
        pushed = false;
        switch (CanonicalTagName(name))
        {
            case "Font":
                if (!string.IsNullOrEmpty(argument))
                {
                    // tolerate ".fnt" suffix
                    if (argument!.EndsWith(".fnt"))
                    {
                        argument = argument.Substring(0, argument.Length - ".fnt".Length);
                    }
                    FontName.Push(argument);
                    pushed = true;
                }
                else
                {
                    Pop(FontName);
                }
                return true;
            case "FontSize":
                if (float.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedSize))
                {
                    FontSize.Push(parsedSize);
                    pushed = true;
                }
                else
                {
                    Pop(FontSize);
                }
                return true;
            case "OutlineThickness":
                if (int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedOutline))
                {
                    OutlineThickness.Push(parsedOutline);
                    pushed = true;
                }
                else
                {
                    Pop(OutlineThickness);
                }
                return true;
            case "IsItalic":
                pushed = PushOrPop(IsItalic, argument);
                return true;
            case "IsBold":
                pushed = PushOrPop(IsBold, argument);
                return true;
            case "UseCustomFont":
                // Never fires today (UseCustomFont is not a BbCodeParser.KnownTag).
                pushed = PushOrPop(UseCustomFont, argument);
                return true;
            case "HasDropshadow":
                pushed = PushOrPop(HasDropshadow, argument);
                return true;
            default:
                return false;
        }
    }

    private static bool PushOrPop(Stack<bool> stack, string? argument)
    {
        if (bool.TryParse(argument, out bool parsed))
        {
            stack.Push(parsed);
            return true;
        }

        Pop(stack);
        return false;
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
