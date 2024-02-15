

using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Gum.Wireframe
{

    public struct TagInfo
    {
        public int StartIndex;
        public int Count;
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
            return $"{Name}";
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

        readonly static Tag defaultTag = new Tag();

        public static List<FoundTag> Parse(HashSet<string> tags, string text)
        {
            var results = new List<FoundTag>();
            if (tags.Count == 0 || string.IsNullOrWhiteSpace(text))
            {
                return results;
            }

            var index = 0;
            var activeTags = new List<Tag>();

            int accumulatedTagLetterCount = 0;

            while (index < text.Length)
            {
                var nullableTag = GetTagAtIndex(text, tags, index, accumulatedTagLetterCount);
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
                        Tag foundTag = activeTags.FirstOrDefault(
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
}
