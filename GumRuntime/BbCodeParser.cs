

using System;
using System.Globalization;
using System.Linq;
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
    /// The tag name as written in the text, such as "Link" for <c>[Link=shop]</c>.
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
    /// </remarks>
    public static HashSet<string> KnownTags { get; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "alpha",
        "red",
        "blue",
        "green",
        "color",
        "font",
        "fontsize",
        "outlinethickness",
        "isitalic",
        "isbold",
        "usefontsmoothing",
        "fontscale",
        "lineheightmultiplier",
        "custom",
        "state",
        "hasdropshadow",
        "link",
    };

    private struct Tag
    {
        public bool IsOpening;
        public int StartIndex;
        public int Count;
        public string Name;
        public string? Argument;

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
        if (availableTags.Count == 0 || string.IsNullOrWhiteSpace(text))
        {
            return results;
        }

        var index = 0;
        var activeTags = new List<Tag>();

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
                    // October 17, 2025 - need to do
                    // LastOrDefault so we pop the last 
                    // index of this tag rather that the 
                    // first, or else they do not stack properly.
                    //Tag foundTag = activeTags.FirstOrDefault(
                    Tag foundTag = activeTags.LastOrDefault(
                        item => item.Name.Equals(tag.Name, StringComparison.InvariantCultureIgnoreCase));
                    if(foundTag.Name == tag.Name)
                    {
                        // Matching closing tag was at the top of the stack
                        var open = new TagInfo()
                        {
                            StartIndex = foundTag.StartIndex,
                            Count = foundTag.Count,
                            Argument = foundTag.Argument,
                            Name = tag.Name
                        };

                        var close = new TagInfo()
                        {
                            StartIndex = tag.StartIndex,
                            Count = tag.Count,
                            Name = tag.Name
                        };
                        results.Add(new FoundTag
                        {
                            Name = tag.Name,
                            Open = open,
                            Close = close,
                        });

                        activeTags.Remove(foundTag);
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

        return results.OrderBy(x => x.Open.StartIndex).ToList();
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

    public static string AddTags(string text, List<FoundTag> tags, int strippedStringPaddingCount = 0)
    {
        var allTags = new List<TagInfo>(tags.Count * 2);
        HashSet<TagInfo> openingTags = new HashSet<TagInfo>();
        for (int i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];

            var isTagInString = (tag.Open.StartStrippedIndex >= strippedStringPaddingCount && tag.Open.StartStrippedIndex < text.Length + strippedStringPaddingCount) ||
                                (tag.Close.StartStrippedIndex > strippedStringPaddingCount && tag.Close.StartStrippedIndex <= text.Length + strippedStringPaddingCount);

            if(isTagInString)
            {
                allTags.Add(tag.Open);
                allTags.Add(tag.Close);
                openingTags.Add(tag.Open);
            }
        }

        var sorted = allTags
            .OrderBy(item => item.StartIndex).ToArray();

        var stringBuilder = new StringBuilder(text);

        int characterCountForTags = 0;

        foreach (var tag in sorted)
        {
            var isOpening = openingTags.Contains(tag);
            var tagText = isOpening 
                ? $"[{tag.Name}={tag.Argument}]"
                : $"[/{tag.Name}]";

            var desiredStrippedInsertionIndex = 
                Math.Max(0, tag.StartStrippedIndex - strippedStringPaddingCount);

            if (desiredStrippedInsertionIndex + characterCountForTags >= stringBuilder.Length)
            {
                stringBuilder.Append(tagText);
            }
            
            else
            {
                stringBuilder.Insert(desiredStrippedInsertionIndex + characterCountForTags, tagText);
            }


            characterCountForTags += tagText.Length;
            
        }

        return stringBuilder.ToString();
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
        while (peekIndex < text.Length)
        {
            switch (text[peekIndex])
            {
                case ' ':
                    if (equalsIndex == null)
                    {
                        return null; // space isn't valid unless after an equals sign
                    }

                    break;

                case '[':
                    return null; // `[` isn't valid inside a tag definition

                case '=':
                    equalsIndex = peekIndex;
                    break;

                case ']':
                    {
                        var nameEndIndex = equalsIndex ?? peekIndex;
                        if (startIndex + 1 == nameEndIndex)
                        {
                            // No tag name provided
                            return null;
                        }

                        var tagName = text.Substring(nameStartIndex, nameEndIndex - nameStartIndex);
                        if (!tags.Contains(tagName))
                        {
                            // Not a known tag name
                            return null;
                        }

                        // Valid tag
                        var argument = equalsIndex != null
                            ? text.Substring(equalsIndex.Value + 1, peekIndex - equalsIndex.Value - 1)
                            : null;

                        return new Tag
                        {
                            IsOpening = isOpeningTag,
                            StartIndex = startIndex,
                            Count = peekIndex - startIndex + 1,
                            Name = tagName,
                            Argument = argument
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
        var allTags = new List<TagInfo>(tags.Count * 2);
        for (int i = 0; i < tags.Count; i++)
        {
            allTags.Add(tags[i].Open);
            allTags.Add(tags[i].Close);

        }

        var sorted = allTags.OrderBy(item => item.StartStrippedIndex).ToArray();

        var stringBuilder = new StringBuilder(text);

        foreach (var tag in sorted)
        {
            stringBuilder.Remove(tag.StartStrippedIndex, tag.Count);
        }

        return stringBuilder.ToString();
    }
}
