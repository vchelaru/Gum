

using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace Gum.Wireframe;


public struct TagInfo
{
    /// <summary>
    /// The index that this tag begins in text that includes all bbcode tags
    /// </summary>
    public int StartIndex;
    public int Count;
    /// <summary>
    /// The index that this tag begins after tags have been stripped from the text.
    /// </summary>
    public int StartStrippedIndex;
    public string? Argument;

    /// <summary>
    /// True when <see cref="Argument"/> holds a built-in effect's space-separated parameters
    /// (<c>[Wave amp=50 freq=5]</c>) rather than the text after an equals sign.
    /// </summary>
    internal bool HasOptions;

    // even though FoundTag has Name, we add the name here so we can
    // sort open/close in one list
    public string Name;

    public override string ToString()
    {
        if(!string.IsNullOrEmpty(Argument))
        {
            return $"{Name}={Argument}";
        }
        else
        {
            return Name;
        }
    }
}

public struct FoundTag
{
    public string Name;
    public TagInfo Open;
    public TagInfo Close;

    public override string ToString()
    {
        return Open.ToString();
    }
}

/// <summary>
/// A BBCode tag's range in the text as displayed (tags removed), returned by
/// <c>TextRuntime.GetTagAtIndex</c>. The indexes are in the same space as
/// <c>TextRuntime.GetCharacterIndexAtPosition</c>.
/// </summary>
public readonly struct TextTagSpan
{
    /// <summary>
    /// The canonical spelling of the tag name (not the casing written in the text), such as "Link" for <c>[Link=shop]</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The text after the equals sign, such as "shop" for <c>[Link=shop]</c>, or null if the tag has none.
    /// </summary>
    public string? Argument { get; }

    /// <summary>
    /// The index of the first character inside the tag.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// The index just past the last character inside the tag (exclusive), so a character index
    /// is inside the tag when <c>index &gt;= StartIndex &amp;&amp; index &lt; EndIndex</c>.
    /// </summary>
    public int EndIndex { get; }

    public TextTagSpan(string name, string? argument, int startIndex, int endIndex)
    {
        Name = name;
        Argument = argument;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    public override string ToString() => $"{Name}={Argument} [{StartIndex}, {EndIndex})";
}

public static class BbCodeParser
{
    /// <summary>
    /// The canonical set of BbCode tag names this parser understands when applying values
    /// onto a renderable (color/font/scale/etc.). Callers that want to use the default tag
    /// set — instead of hand-rolling their own <c>HashSet&lt;string&gt;</c> — pass this to
    /// <see cref="Parse"/>. Used by <c>DialogBox</c>'s typewriter logic so it can count
    /// visible characters by stripping known tags from the source text.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="System.StringComparer.OrdinalIgnoreCase"/> — <c>InvariantCulture</c>
    /// has been observed to crash when uploaded to itch from a Blazor build.
    /// The built-in effect tags such as <c>[Wave]</c> are not in this set: <see cref="Parse"/> treats them as known
    /// whenever the loaded project's version supports them.
    /// </remarks>
    public static HashSet<string> KnownTags { get; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Alpha",
        "Red",
        "Blue",
        "Green",
        "Color",
        "Font",
        "FontSize",
        "OutlineThickness",
        "IsItalic",
        "IsBold",
        "UseFontSmoothing",
        "FontScale",
        "Custom",
        "State",
        "HasDropshadow",
        "Link",
    };

    internal struct Tag
    {
        public bool IsOpening;
        public int StartIndex;
        public int Count;
        public string Name;
        public string? Argument;
        public bool HasOptions;

        public override string ToString()
        {
            return $"{(IsOpening ? "Open" : "Close")} {Name} {Argument}";
        }
    }

    /// <summary>
    /// Replaces every <c>[Glyph=...]</c> tag with the character for its code point: decimal
    /// (<c>[Glyph=57344]</c>) or hex with a 0x prefix (<c>[Glyph=0xE000]</c>). A code point above U+FFFF
    /// becomes a surrogate pair. A tag whose value is not a valid code point is removed, along with a <c>[/Glyph]</c> that follows it. Call this on the
    /// text before <see cref="Parse"/> so the indexes of the other tags are computed against the
    /// final text. Whether the character draws depends on the font.
    /// </summary>
    /// <param name="text">The text which may contain Glyph tags.</param>
    /// <returns>The text with Glyph tags replaced, or the same string if it has none.</returns>
    public static string ReplaceGlyphTags(string text)
    {
        if (text.IndexOf("glyph", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return text;
        }

        StringBuilder? builder = null;
        int copiedUpTo = 0;
        int openTagsAwaitingClose = 0;
        int searchFrom = 0;

        while (searchFrom < text.Length)
        {
            int tagIndex = text.IndexOf('[', searchFrom);
            if (tagIndex < 0)
            {
                break;
            }

            int closeIndex = text.IndexOf(']', tagIndex);
            if (closeIndex < 0)
            {
                break;
            }

            // A '[' inside the brackets means this one does not start a tag.
            if (text.IndexOf('[', tagIndex + 1, closeIndex - tagIndex - 1) >= 0)
            {
                searchFrom = tagIndex + 1;
                continue;
            }

            string inner = text.Substring(tagIndex + 1, closeIndex - tagIndex - 1);
            bool isCloseTag = inner.StartsWith("/", StringComparison.Ordinal);
            int equalsIndex = inner.IndexOf('=');
            string name = isCloseTag
                ? inner.Substring(1)
                : (equalsIndex < 0 ? inner : inner.Substring(0, equalsIndex));

            bool isGlyphTag = name.Equals("glyph", StringComparison.OrdinalIgnoreCase) &&
                (!isCloseTag || equalsIndex < 0);

            // Like any other orphan close tag, a close with no open before it stays as text.
            if (!isGlyphTag || (isCloseTag && openTagsAwaitingClose == 0))
            {
                searchFrom = tagIndex + 1;
                continue;
            }

            builder ??= new StringBuilder(text.Length);
            builder.Append(text, copiedUpTo, tagIndex - copiedUpTo);
            copiedUpTo = closeIndex + 1;
            searchFrom = closeIndex + 1;

            if (isCloseTag)
            {
                openTagsAwaitingClose--;
            }
            else
            {
                openTagsAwaitingClose++;
                string argument = equalsIndex < 0 ? "" : inner.Substring(equalsIndex + 1);
                if (TryParseGlyphCodePoint(argument, out int codePoint))
                {
                    builder.Append(char.ConvertFromUtf32(codePoint));
                }
            }
        }

        if (builder == null)
        {
            return text;
        }

        builder.Append(text, copiedUpTo, text.Length - copiedUpTo);
        return builder.ToString();
    }

    private static bool TryParseGlyphCodePoint(string argument, out int codePoint)
    {
        string number = argument.Trim();
        NumberStyles styles = NumberStyles.None;
        if (number.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            number = number.Substring(2);
            styles = NumberStyles.AllowHexSpecifier;
        }

        bool isSurrogateHalf(int value) => value >= 0xD800 && value <= 0xDFFF;

        return int.TryParse(number, styles, CultureInfo.InvariantCulture, out codePoint) &&
            codePoint > 0 &&
            codePoint <= 0x10FFFF &&
            !isSurrogateHalf(codePoint);
    }

    /// <summary>
    /// Returns a list of FoundTags in the argument text. Only tags in the availableTags set are returned.
    /// </summary>
    /// <param name="text">The text populated with tags.</param>
    /// <param name="availableTags">The available tags for parsing. Tags can be lower-case.</param>
    /// <returns>The list of found tags.</returns>
    public static List<FoundTag> Parse(string text, HashSet<string> availableTags)
    {
        var results = new List<FoundTag>();
        Parse(text, availableTags, results, null);
        return results;
    }

    /// <summary>
    /// <see cref="Parse(string, HashSet{string})"/> into a list the caller keeps and clears for the next text, with
    /// the stack of open tags passed in too, so a text parsed on every change allocates neither list (#7846).
    /// </summary>
    internal static void Parse(string text, HashSet<string> availableTags, List<FoundTag> results, List<Tag>? activeTags)
    {
        results.Clear();
        if (availableTags.Count == 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var index = 0;
        if (activeTags == null)
        {
            activeTags = new List<Tag>();
        }
        else
        {
            activeTags.Clear();
        }

        while (index < text.Length)
        {
            var nullableTag = GetTagAtIndex(text, availableTags, index);
            if (nullableTag != null)
            {
                var tag = nullableTag.Value;
                if (tag.IsOpening)
                {
                    activeTags.Add(tag);
                }
                else if(activeTags.Count > 0)
                {
                    // October 17, 2025 - need to pop the last
                    // index of this tag rather that the
                    // first, or else they do not stack properly.
                    // Names are compared ignoring case, the same as when a tag is recognized,
                    // so [Color=Red]a[/color] pairs (#7094).
                    int foundIndex = -1;
                    for (int i = activeTags.Count - 1; i >= 0; i--)
                    {
                        if (activeTags[i].Name.Equals(tag.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            foundIndex = i;
                            break;
                        }
                    }

                    if(foundIndex >= 0)
                    {
                        Tag foundTag = activeTags[foundIndex];
                        // Both tags carry the opening tag's name: consumers push on the open tag's Name
                        // and pop on the close tag's Name, so the two must be identical.
                        var open = new TagInfo()
                        {
                            StartIndex = foundTag.StartIndex,
                            Count = foundTag.Count,
                            Argument = foundTag.Argument,
                            HasOptions = foundTag.HasOptions,
                            Name = foundTag.Name
                        };

                        var close = new TagInfo()
                        {
                            StartIndex = tag.StartIndex,
                            Count = tag.Count,
                            Name = foundTag.Name
                        };
                        results.Add(new FoundTag
                        {
                            Name = foundTag.Name,
                            Open = open,
                            Close = close,
                        });

                        activeTags.RemoveAt(foundIndex);
                    }
                }

                index += tag.Count;
            }
            else
            {
                index++;
            }
        }

        // An opening tag with no closing tag stays in the text, so a tag's stripped index can only be
        // known once every tag has been matched: it is the tag's index minus the length of every
        // matched tag before it.
        for (int i = 0; i < results.Count; i++)
        {
            FoundTag found = results[i];
            found.Open.StartStrippedIndex = found.Open.StartIndex - CountMatchedTagLettersBefore(results, found.Open.StartIndex);
            found.Close.StartStrippedIndex = found.Close.StartIndex - CountMatchedTagLettersBefore(results, found.Close.StartIndex);
            results[i] = found;
        }

        // A stable sort in place, the same order OrderBy gave, without its per-call buffers (#7818).
        for (int i = 1; i < results.Count; i++)
        {
            FoundTag found = results[i];
            int j = i - 1;
            while (j >= 0 && results[j].Open.StartIndex > found.Open.StartIndex)
            {
                results[j + 1] = results[j];
                j--;
            }
            results[j + 1] = found;
        }
    }

    private static int CountMatchedTagLettersBefore(List<FoundTag> matchedTags, int index)
    {
        int count = 0;
        for (int i = 0; i < matchedTags.Count; i++)
        {
            FoundTag found = matchedTags[i];
            if (found.Open.StartIndex < index)
            {
                count += found.Open.Count;
            }
            if (found.Close.StartIndex < index)
            {
                count += found.Close.Count;
            }
        }
        return count;
    }

    /// <summary>
    /// Writes back into <paramref name="text"/>, a piece of the stripped text starting at stripped index
    /// <paramref name="strippedStringPaddingCount"/>, every tag that covers any of its characters, so the piece
    /// styles exactly as the same characters do in the whole text. A tag opened before the piece opens at its start,
    /// and one that closes after it closes at its end.
    /// </summary>
    public static string AddTags(string text, List<FoundTag> tags, int strippedStringPaddingCount = 0) =>
        AddTags(new[] { text }, new[] { strippedStringPaddingCount }, strippedStringPaddingCount + text.Length, tags);

    /// <summary>
    /// Joins <paramref name="lines"/> with '\n' and writes every tag that covers any of their characters back in, so the
    /// result styles exactly as the same characters do in the whole text: paging never changes how markup renders.
    /// </summary>
    /// <param name="lines">The text shown for each line. A line may be shorter than its stripped range (a trailing space removed).</param>
    /// <param name="lineStarts">The stripped index at which each line starts, ascending.</param>
    /// <param name="end">The stripped index just past the last line.</param>
    /// <param name="tags">Tags parsed from the whole text, whose stripped indexes count no newlines.</param>
    internal static string AddTags(IReadOnlyList<string> lines, IReadOnlyList<int> lineStarts, int end, List<FoundTag> tags)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        int[] lineOffsets = new int[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                stringBuilder.Append('\n');
            }
            lineOffsets[i] = stringBuilder.Length;
            stringBuilder.Append(lines[i]);
        }

        int start = lineStarts[0];

        int lineEnd(int line) => line + 1 < lines.Count ? lineStarts[line + 1] : end;

        int toPosition(int line, int strippedIndex) =>
            lineOffsets[line] + Math.Min(strippedIndex - lineStarts[line], lines[line].Length);

        // An open tag goes on the line holding its first character, a close tag on the line holding its last.
        int openPosition(int strippedIndex)
        {
            int line = lines.Count - 1;
            while (line > 0 && lineStarts[line] > strippedIndex)
            {
                line--;
            }
            return toPosition(line, strippedIndex);
        }

        int closePosition(int strippedIndex)
        {
            int line = 0;
            while (line < lines.Count - 1 && lineEnd(line) < strippedIndex)
            {
                line++;
            }
            return toPosition(line, strippedIndex);
        }

        // Sorted by position in the joined text, then by the tag's index in the source, so tags at one position
        // keep their original order and nesting stays intact.
        var insertions = new List<(int Position, int SourceIndex, string Text)>(tags.Count * 2);
        for (int i = 0; i < tags.Count; i++)
        {
            FoundTag tag = tags[i];
            int open = tag.Open.StartStrippedIndex;
            int close = tag.Close.StartStrippedIndex;

            bool coversPiece = open < end && close > start;
            bool isEmptyInPiece = open == close && open >= start && open < end;
            if (!coversPiece && !isEmptyInPiece)
            {
                continue;
            }

            int openAt = openPosition(Math.Max(open, start));
            int closeAt = isEmptyInPiece ? openAt : closePosition(Math.Min(close, end));

            // A tag written without a value goes back without '=', since [Color=] parses as an empty color (#8313).
            // An effect tag's parameters go back space-separated ([Wave amp=50]), not after '='.
            string openText = tag.Open.Argument == null
                ? $"[{tag.Open.Name}]"
                : tag.Open.HasOptions
                    ? $"[{tag.Open.Name} {tag.Open.Argument}]"
                    : $"[{tag.Open.Name}={tag.Open.Argument}]";

            insertions.Add((openAt, tag.Open.StartIndex, openText));
            insertions.Add((closeAt, tag.Close.StartIndex, $"[/{tag.Close.Name}]"));
        }

        insertions.Sort((a, b) => a.Position != b.Position
            ? a.Position.CompareTo(b.Position)
            : a.SourceIndex.CompareTo(b.SourceIndex));

        int inserted = 0;
        foreach (var insertion in insertions)
        {
            stringBuilder.Insert(insertion.Position + inserted, insertion.Text);
            inserted += insertion.Text.Length;
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Returns the set's own spelling of the tag name found at <paramref name="start"/> (so a tag written in any case
    /// reports one canonical name and no string is copied out of <paramref name="text"/>), or null when the set does not
    /// contain it. A set that is not case-insensitive is asked with a copied string, as before.
    /// </summary>
    // A name in tags, or a built-in effect name while the loaded project's version supports effects; returns the
    // spelling in the set (PascalCase for an effect).
    private static string? FindKnownTagName(HashSet<string> tags, string text, int start, int length)
    {
        string? found = FindTagNameIn(tags, text, start, length);
        if (found == null && BbCodeTextEffect.AreEnabled)
        {
            found = FindTagNameIn(BbCodeTextEffect.EffectTagSet, text, start, length);
        }
        return found;
    }

    private static string? FindTagNameIn(HashSet<string> tags, string text, int start, int length)
    {
        if (ReferenceEquals(tags.Comparer, StringComparer.OrdinalIgnoreCase))
        {
            ReadOnlySpan<char> name = text.AsSpan(start, length);
            foreach (string known in tags)
            {
                if (name.Equals(known.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return known;
                }
            }

            return null;
        }

        string copied = text.Substring(start, length);
        return tags.Contains(copied) ? copied : null;
    }

    private static Tag? GetTagAtIndex(string text, HashSet<string> tags, int startIndex)
    {
        if (text[startIndex] != '[' || text.Length <= startIndex + 1)
        {
            return null;
        }

        var peekIndex = startIndex + 1;
        var equalsIndex = (int?)null;
        var isOpeningTag = true;
        if (text[peekIndex] == '/')
        {
            isOpeningTag = false;
            peekIndex++;
        }

        var nameStartIndex = peekIndex;
        // A built-in effect's parameters ([Wave amp=50 freq=5]) start after the first space, and only when the
        // effect is in the tag set. Every other tag still rejects a space before '=', so its parse is unchanged.
        int? optionsStartIndex = null;
        while (peekIndex < text.Length)
        {
            if (optionsStartIndex != null && text[peekIndex] != ']' && text[peekIndex] != '[')
            {
                peekIndex++;
                continue;
            }

            switch (text[peekIndex])
            {
                case ' ':
                    if (equalsIndex == null)
                    {
                        if (isOpeningTag
                            && FindKnownTagName(tags, text, nameStartIndex, peekIndex - nameStartIndex) is string effectName
                            && BbCodeTextEffect.IsEffectTagName(effectName))
                        {
                            optionsStartIndex = peekIndex + 1;
                            break;
                        }
                        return null; // space isn't valid unless after an equals sign
                    }

                    break;

                case '[':
                    return null; // `[` isn't valid inside a tag definition

                case '=':
                    // The first '=' ends the name; later ones belong to the value (a URL query, #8311).
                    equalsIndex ??= peekIndex;
                    break;

                case ']':
                    {
                        var nameEndIndex = optionsStartIndex != null
                            ? optionsStartIndex.Value - 1
                            : equalsIndex ?? peekIndex;
                        if (startIndex + 1 == nameEndIndex)
                        {
                            // No tag name provided
                            return null;
                        }

                        string? tagName = FindKnownTagName(tags, text, nameStartIndex, nameEndIndex - nameStartIndex);
                        if (tagName == null)
                        {
                            // Not a known tag name
                            return null;
                        }

                        // An effect takes named parameters only; [Wave=...] is not a tag, like any other malformed one.
                        if (optionsStartIndex == null && equalsIndex != null && BbCodeTextEffect.IsEffectTagName(tagName))
                        {
                            return null;
                        }

                        // Valid tag
                        string? argument;
                        if (optionsStartIndex != null)
                        {
                            argument = text.Substring(optionsStartIndex.Value, peekIndex - optionsStartIndex.Value).Trim();
                        }
                        else
                        {
                            argument = equalsIndex != null
                                ? text.Substring(equalsIndex.Value + 1, peekIndex - equalsIndex.Value - 1)
                                : null;
                        }

                        return new Tag
                        {
                            IsOpening = isOpeningTag,
                            StartIndex = startIndex,
                            Count = peekIndex - startIndex + 1,
                            Name = tagName,
                            Argument = argument,
                            HasOptions = optionsStartIndex != null,
                        };
                    }

                default:
                    break;

            }

            peekIndex++;
        }

        // We didn't find a proper tag
        return null;
    }

    public static string RemoveTags(string text, List<FoundTag> tags)
    {
        TagInfo[]? sortScratch = null;
        return RemoveTags(text, tags, ref sortScratch, null);
    }

    /// <summary>
    /// <see cref="RemoveTags(string, List{FoundTag})"/> with the sort array and the string builder passed in, so a text
    /// parsed on every change allocates only the resulting string (#7846). <paramref name="sortScratch"/> is replaced
    /// when it is too small; <paramref name="builder"/> is cleared and refilled.
    /// </summary>
    internal static string RemoveTags(string text, List<FoundTag> tags, ref TagInfo[]? sortScratch, StringBuilder? builder)
    {
        // Sorted stably by stripped index, the order OrderBy gave, without its per-call buffers (#7818).
        int sortedCount = tags.Count * 2;
        TagInfo[] sorted = sortScratch != null && sortScratch.Length >= sortedCount ? sortScratch : new TagInfo[sortedCount];
        sortScratch = sorted;
        for (int i = 0; i < sortedCount; i++)
        {
            TagInfo tag = i % 2 == 0 ? tags[i / 2].Open : tags[i / 2].Close;
            int j = i - 1;
            while (j >= 0 && sorted[j].StartStrippedIndex > tag.StartStrippedIndex)
            {
                sorted[j + 1] = sorted[j];
                j--;
            }
            sorted[j + 1] = tag;
        }

        StringBuilder stringBuilder;
        if (builder == null)
        {
            stringBuilder = new StringBuilder(text);
        }
        else
        {
            stringBuilder = builder;
            stringBuilder.Clear();
            stringBuilder.Append(text);
        }

        for (int i = 0; i < sortedCount; i++)
        {
            stringBuilder.Remove(sorted[i].StartStrippedIndex, sorted[i].Count);
        }

        return stringBuilder.ToString();
    }
}
