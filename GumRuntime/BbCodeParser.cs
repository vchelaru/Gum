

using System;
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
    public string Argument;

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

public static class BbCodeParser
{
    private struct Tag
    {
        public bool IsOpening;
        public int StartIndex;
        public int Count;
        public string Name;
        public string Argument;
        public int StartStrippedIndex;

        public override string ToString()
        {
            return $"{(IsOpening ? "Open" : "Close")} {Name} {Argument}";
        }
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

        int accumulatedTagLetterCount = 0;

        while (index < text.Length)
        {
            var nullableTag = GetTagAtIndex(text, availableTags, index, accumulatedTagLetterCount);
            if (nullableTag != null)
            {
                var tag = nullableTag.Value;
                if (tag.IsOpening)
                {
                    activeTags.Add(tag);
                    accumulatedTagLetterCount += tag.Count;
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
                            StartStrippedIndex = foundTag.StartStrippedIndex,
                            Name = tag.Name
                        };

                        var closeStripped = tag.StartIndex - accumulatedTagLetterCount;

                        var close = new TagInfo()
                        {
                            StartIndex = tag.StartIndex,
                            Count = tag.Count,
                            StartStrippedIndex = closeStripped,
                            Name = tag.Name
                        };
                        accumulatedTagLetterCount += close.Count;
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

        return results.OrderBy(x => x.Open.StartIndex).ToList();
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

    private static Tag? GetTagAtIndex(string text, HashSet<string> tags, int startIndex, int accumulatedTagLetterCount)
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
                            Argument = argument,
                            StartStrippedIndex = startIndex - accumulatedTagLetterCount
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
