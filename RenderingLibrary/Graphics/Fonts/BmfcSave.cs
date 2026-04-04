using System.Reflection;
using ToolsUtilities;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Represents a BMFont configuration file (.bmfc) used for bitmap font generation.
/// Contains font properties (name, size, style) and character range settings.
/// Also provides a static API for customizing the default character ranges
/// used when no Gum project is loaded.
/// </summary>
public class BmfcSave
{
    /// <summary>
    /// The font family name (e.g., "Arial", "Times New Roman").
    /// </summary>
    public string FontName = "Arial";

    /// <summary>
    /// The font size in points.
    /// </summary>
    public int FontSize = 20;

    /// <summary>
    /// The thickness of the font outline in pixels. Zero means no outline.
    /// </summary>
    public int OutlineThickness = 0;

    /// <summary>
    /// Whether to use font smoothing (anti-aliasing) when rendering the font.
    /// </summary>
    public bool UseSmoothing = true;

    /// <summary>
    /// Whether the font should be rendered in italic style.
    /// </summary>
    public bool IsItalic = false;

    /// <summary>
    /// Whether the font should be rendered in bold style.
    /// </summary>
    public bool IsBold = false;

    /// <summary>
    /// Horizontal spacing between characters in pixels.
    /// </summary>
    public int SpacingHorizontal = 1;

    /// <summary>
    /// Vertical spacing between characters in pixels.
    /// </summary>
    public int SpacingVertical = 1;

    /// <summary>
    /// The default character ranges included in generated fonts.
    /// Covers ASCII printable characters (32-126) and Latin-1 Supplement (160-255).
    /// </summary>
    public const string DefaultRanges = "32-126,160-255";

    /// <summary>
    /// The character ranges to include in this font configuration.
    /// Uses a comma-separated format of individual codepoints or "start-end" ranges
    /// (e.g., "32-126,160-255,9472-9580").
    /// </summary>
    public string Ranges = GetEffectiveDefaultRanges();

    /// <summary>
    /// The width of the output font texture in pixels.
    /// </summary>
    public int OutputWidth = 512;

    /// <summary>
    /// The height of the output font texture in pixels.
    /// </summary>
    public int OutputHeight = 256;

    private static readonly List<string> _additionalRanges = new List<string>();

    /// <summary>
    /// Appends additional character ranges to the effective default font ranges.
    /// These are combined with <see cref="DefaultRanges"/> when no Gum project
    /// provides explicit ranges. Ranges use comma-separated format of individual
    /// codepoints or "start-end" pairs (e.g., "9472-9580" or "9472-9580,9600-9631").
    /// </summary>
    /// <param name="range">A comma-separated string of codepoint ranges to add.</param>
    public static void AddFontRange(string range)
    {
        _additionalRanges.Add(range);
    }

    /// <summary>
    /// Appends individual characters to the effective default font ranges by
    /// converting each character to its Unicode codepoint. This is a convenience
    /// method for adding specific characters without manually looking up codepoints.
    /// </summary>
    /// <param name="characters">A string whose characters will each be added as individual codepoints.</param>
    public static void AddCharacters(string characters)
    {
        foreach(var c in characters)
        {
            _additionalRanges.Add(((int)c).ToString());
        }
    }

    /// <summary>
    /// Returns the effective default font ranges, combining <see cref="DefaultRanges"/>
    /// with any additional ranges registered via <see cref="AddFontRange"/> or
    /// <see cref="AddCharacters"/>. The result is a merged, deduplicated range string.
    /// This is used as the fallback when no Gum project provides explicit font ranges.
    /// </summary>
    /// <returns>A comma-separated range string covering all default and additional characters.</returns>
    public static string GetEffectiveDefaultRanges()
    {
        if(_additionalRanges.Count == 0)
        {
            return DefaultRanges;
        }

        var combined = DefaultRanges + "," + string.Join(",", _additionalRanges);
        var allChars = ParseCharRanges(combined);
        var blocks = ConvertToRanges(allChars);

        var builder = new StringBuilder();
        for(int i = 0; i < blocks.Count; i++)
        {
            if(i > 0)
            {
                builder.Append(',');
            }

            var block = blocks[i];
            if(block.start == block.end)
            {
                builder.Append(block.start);
            }
            else
            {
                builder.Append(block.start).Append('-').Append(block.end);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Clears all additional font ranges previously added via
    /// <see cref="AddFontRange"/> or <see cref="AddCharacters"/>.
    /// </summary>
    public static void ClearAdditionalRanges()
    {
        _additionalRanges.Clear();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{FontName} {FontSize}";
    }

    /// <summary>
    /// Saves this configuration as a .bmfc file by substituting values into
    /// the BmfcTemplate.bmfc template file.
    /// </summary>
    /// <param name="fileName">The output file path for the generated .bmfc file.</param>
    public void Save(string fileName)
    {
        var assembly2 = Assembly.GetEntryAssembly();

        string directory = FileManager.GetDirectory(assembly2.Location);

        var bmfcTemplateFullPath =
            directory + "Content/BmfcTemplate.bmfc";

        if(!System.IO.File.Exists(bmfcTemplateFullPath))
        {
            throw new FileNotFoundException(bmfcTemplateFullPath);
        }

        string template = FileManager.FromFileText(bmfcTemplateFullPath);

        template = template.Replace("FontNameVariable", FontName);
        template = template.Replace("FontSizeVariable", FontSize.ToString());
        template = template.Replace("OutlineThicknessVariable", OutlineThickness.ToString());
        template = template.Replace("{UseSmoothing}", UseSmoothing ? "1" : "0");
        template = template.Replace("{IsItalic}", IsItalic ? "1" : "0");
        template = template.Replace("{IsBold}", IsBold ? "1" : "0");

        template = template.Replace("{SpacingHorizontal}", SpacingHorizontal.ToString());
        template = template.Replace("{SpacingVertical}", SpacingVertical.ToString());
        template = template.Replace("{OutputWidth}", OutputWidth.ToString());
        template = template.Replace("{OutputHeight}", OutputHeight.ToString());



        //alphaChnl=alphaChnlValue
        //redChnl=redChnlValue
        //greenChnl=greenChnlValue
        //blueChnl=blueChnlValue
        if (OutlineThickness == 0)
        {
            template = template.Replace("alphaChnlValue", "0");
            template = template.Replace("redChnlValue", "4");
            template = template.Replace("greenChnlValue", "4");
            template = template.Replace("blueChnlValue", "4");
        }
        else
        {
            template = template.Replace("alphaChnlValue", "1");
            template = template.Replace("redChnlValue", "0");
            template = template.Replace("greenChnlValue", "0");
            template = template.Replace("blueChnlValue", "0");
        }

        var newRange = Ranges;

        var isValidRange = GetIfIsValidRange(newRange);

        if(!isValidRange)
        {
            newRange = GetEffectiveDefaultRanges();
        }

        else
        {
            newRange = EnsureRangesContainSpace(newRange);
        }

        var charsReplacement = GenerateSplitRangesString(newRange);
        template = template.Replace("chars=32-126,160-255", charsReplacement);

        FileManager.SaveText(template, fileName);
    }

    /// <summary>
    /// Generates a bmfc-compatible character range string with line wrapping.
    /// Splits ranges across multiple "chars=" lines to stay within bmfont format limits.
    /// </summary>
    /// <param name="ranges">A comma-separated range string (e.g., "32-126,160-255").</param>
    /// <param name="maxBlocksPerLine">Maximum number of range blocks per "chars=" line.</param>
    /// <returns>A bmfc-formatted string with one or more "chars=" lines.</returns>
    private static string GenerateSplitRangesString(string ranges, int maxBlocksPerLine = 10)
    {
        var allChars = ParseCharRanges(ranges);
        var blocks = ConvertToRanges(allChars);

        var builder = new StringBuilder();
        for(int i = 0; i < blocks.Count; i++)
        {
            if(i % maxBlocksPerLine == 0)
            {
                if(i > 0)
                {
                    builder.Append(Environment.NewLine);
                }
                builder.Append("chars=");
            }
            else
            {
                builder.Append(',');
            }

            var block = blocks[i];
            if(block.start == block.end)
            {
                builder.Append(block.start);
            }
            else
            {
                builder.Append(block.start).Append('-').Append(block.end);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Parses a comma-separated range string into a list of individual character codepoints.
    /// Supports both individual values ("65") and ranges ("32-126").
    /// </summary>
    /// <param name="charsStr">A comma-separated string of codepoints or ranges.</param>
    /// <returns>A list of all individual codepoints covered by the ranges.</returns>
    public static List<int> ParseCharRanges(string charsStr)
    {
        var allChars = new List<int>();
        var ranges = charsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach(var part in ranges)
        {
            if(part.Contains('-'))
            {
                var split = part.Split('-');
                if(int.TryParse(split[0], out int start) && int.TryParse(split[1], out int end))
                {
                    for(int i = start; i <= end; i++)
                    {
                        allChars.Add(i);
                    }
                }
            }
            else if(int.TryParse(part, out int value))
            {
                allChars.Add(value);
            }
        }
        return allChars;
    }

    /// <summary>
    /// Converts a sorted list of individual codepoints into a list of contiguous ranges.
    /// Adjacent codepoints are merged into a single (start, end) tuple.
    /// </summary>
    /// <param name="codes">A list of codepoints to consolidate into ranges.</param>
    /// <returns>A list of (start, end) tuples representing contiguous ranges.</returns>
    static List<(int start, int end)> ConvertToRanges(List<int> codes)
    {
        var ranges = new List<(int start, int end)>();
        if(codes.Count == 0)
        {
            return ranges;
        }

        codes.Sort();
        int start = codes[0];
        int prev = codes[0];
        for(int i = 1; i < codes.Count; i++)
        {
            int codepoint = codes[i];
            if(codepoint == prev + 1)
            {
                prev = codepoint;
            }
            else
            {
                ranges.Add((start, prev));
                start = prev = codepoint;
            }
        }
        ranges.Add((start, prev));
        return ranges;
    }

    /// <summary>
    /// Ensures the given range string includes the space character (codepoint 32).
    /// If the space character is not present, it is prepended to the range.
    /// BMFont requires the space character to be present for proper rendering.
    /// </summary>
    /// <param name="ranges">The range string to check.</param>
    /// <returns>The range string, with the space character added if it was missing.</returns>
    public static string EnsureRangesContainSpace(string ranges)
    {
        const int spaceChar = (int)' ';

        if (string.IsNullOrEmpty(ranges))
        {
            return spaceChar.ToString();
        }

        bool containsSpace = ranges.Split(',').Any(part =>
        {
            if (part.Contains('-'))
            {
                var split = part.Split('-');
                if (int.TryParse(split[0], out var start) && int.TryParse(split[1], out var end))
                {
                    return spaceChar >= start && spaceChar <= end;
                }
            }
            else if (int.TryParse(part, out var value))
            {
                return value == spaceChar;
            }
            return false;
        });

        if (!containsSpace)
        {
            ranges = spaceChar + "," + ranges;
        }

        return ranges;
    }

    /// <summary>
    /// Validates whether a range string is well-formed. A valid range contains
    /// comma-separated entries that are either individual integers or "start-end"
    /// pairs where start is less than end. Spaces are not allowed.
    /// </summary>
    /// <param name="newRange">The range string to validate.</param>
    /// <returns>True if the range is valid; false otherwise.</returns>
    public static bool GetIfIsValidRange(string newRange)
    {
        try
        {
            if(newRange?.Contains(" ") == true)
            {
                return false; // no spaces allowed, bmfontgenerator doesn't like it
            }
            var individualRanges = newRange.Split(',');

            if(individualRanges.Length == 0)
            {
                return false;
            }
            foreach(var individualRange in individualRanges)
            {
                if(individualRange.Contains("-"))
                {
                    var splitNumbers = individualRange.Split('-');

                    if(splitNumbers.Length != 2)
                    {
                        return false;
                    }
                    else
                    {
                        var firstParsed = int.TryParse(splitNumbers[0], out int result1);
                        var secondParsed = int.TryParse(splitNumbers[1], out int result2);

                        if(result1 >= result2)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // this should be a regular number:
                    var didParseCorrectly = int.TryParse(individualRange, out int parseResult);

                    if(!didParseCorrectly)
                    {
                        return false;
                    }
                }
            }

            return true;

        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to fix a malformed range string by removing spaces.
    /// </summary>
    /// <param name="oldRange">The potentially malformed range string.</param>
    /// <returns>The range string with spaces removed, or an empty string if the input was null or empty.</returns>
    public static string TryFixRange(string oldRange)
    {
        string newRange = string.Empty;
        if(!string.IsNullOrEmpty(oldRange))
        {
            newRange = oldRange.Replace(" ", string.Empty);
        }
        return newRange;
    }

    /// <summary>
    /// Generates a character range string from the unique characters found in a text file.
    /// Reads the file, collects all unique codepoints, and produces a compact range string.
    /// </summary>
    /// <param name="fileName">The path to the text file to analyze.</param>
    /// <returns>A comma-separated range string covering all characters found in the file.</returns>
    public static string GenerateRangesFromFile(string fileName)
    {
        var text = System.IO.File.ReadAllText(fileName);
        var uniqueValues = new System.Collections.Generic.HashSet<int>();
        foreach (var c in text)
        {
            uniqueValues.Add((int)c);
        }

        var ordered = uniqueValues.OrderBy(item => item).ToList();
        var builder = new System.Text.StringBuilder();

        void AppendRange(int start, int end)
        {
            if(builder.Length > 0)
            {
                builder.Append(',');
            }
            if(start == end)
            {
                builder.Append(start);
            }
            else
            {
                builder.Append(start).Append('-').Append(end);
            }
        }

        int currentStart = -1;
        int previous = -1;
        foreach(var val in ordered)
        {
            if(currentStart == -1)
            {
                currentStart = previous = val;
            }
            else if(val == previous + 1)
            {
                previous = val;
            }
            else
            {
                AppendRange(currentStart, previous);
                currentStart = previous = val;
            }
        }

        if(currentStart != -1)
        {
            AppendRange(currentStart, previous);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the font cache file name for this configuration, based on all
    /// font properties that affect rendering (size, name, outline, smoothing, style).
    /// </summary>
    public string FontCacheFileName
    {
        get
        {
            return GetFontCacheFileNameFor(FontSize, FontName, OutlineThickness, UseSmoothing, IsItalic, IsBold);
        }

    }

    /// <summary>
    /// Generates a deterministic font cache file name based on font properties.
    /// The file name encodes all properties that affect rendering so that
    /// different font configurations produce different cache files.
    /// </summary>
    /// <param name="fontSize">The font size in points.</param>
    /// <param name="fontName">The font family name.</param>
    /// <param name="outline">The outline thickness.</param>
    /// <param name="useFontSmoothing">Whether font smoothing is enabled.</param>
    /// <param name="isItalic">Whether the font is italic.</param>
    /// <param name="isBold">Whether the font is bold.</param>
    /// <returns>A relative file path under "FontCache/" suitable for caching this font.</returns>
    public static string GetFontCacheFileNameFor(int fontSize, string fontName, int outline, bool useFontSmoothing,
        bool isItalic = false, bool isBold = false)
    {
        string fileName = null;


        // don't allow some charactersin the file name:
        fontName = fontName.Replace(' ', '_');

        fileName = "Font" + fontSize + fontName;
        if (outline != 0)
        {
            fileName += "_o" + outline;
        }

        if(useFontSmoothing == false)
        {
            fileName += "_noSmooth";
        }

        if(isItalic)
        {
            fileName += "_Italic";
        }

        if(isBold)
        {
            fileName += "_Bold";
        }

        fileName += ".fnt";

        fileName = System.IO.Path.Combine("FontCache", fileName);

        return fileName;
    }


}
