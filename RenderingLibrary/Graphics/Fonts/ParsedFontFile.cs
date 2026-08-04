using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilities;

namespace RenderingLibrary.Graphics;

public class ParsedFontFile
{
    public FontFileInfoLine Info { get; private set; }
    public FontFileCommonLine Common { get; private set; }
    public List<FontFileCharLine> Chars { get; } = new List<FontFileCharLine>(300);
    public List<FontFileKerningLine> Kernings { get; } = new List<FontFileKerningLine>(300);
    public List<FontFilePage> Pages { get; } = new List<FontFilePage>(10);

    /// <summary>
    /// Returns the Pages (List of texture filenames) as an array of strings
    /// </summary>
    public string[] GetPagesAsArrayOfStrings
    {
        get
        {
            List<string> texturesToLoad = new List<string>();
            foreach (var page in Pages)
            {
                texturesToLoad.Add(page.File);
            }
            return texturesToLoad.ToArray();
        }
    }

    public ParsedFontFile(string contents)
    {
        // Determine file type https://www.angelcode.com/products/bmfont/doc/file_format.html
        // Binary   starts with "BMF"
        // XML      starts with "<" (An opening XML tag)
        // Text     starts with "info"
        char firstChar = contents[0];
        if (firstChar == '<')
        {
            // Process XML file
            ParseXmlText(contents);
        }
        else if (firstChar == 66) // 66 = 'B'
        {
            // Process Binary File 
            throw new InvalidOperationException("Unable to load Binary Font files, please convert to XML or TEXT.");
        }
        else if (firstChar == 'i') // first word is "info"
        {
            ParsePlainText(contents);
        }
        else
        {
            // Error, unknown file type!
            throw new InvalidOperationException("Unknown Font File format! Please convert to XML or TEXT!");
        }

    }

    private void ParseXmlText(string contents)
    {
        XElement? root = XDocument.Parse(contents).Root;

        if (root == null)
        {
            throw new InvalidOperationException("Unable to load XML Font file, it has no root element!");
        }

        XElement? infoElement = root.Element("info");
        if (infoElement != null)
        {
            Info = new FontFileInfoLine(infoElement);
        }

        XElement? commonElement = root.Element("common");
        if (commonElement != null)
        {
            Common = new FontFileCommonLine(commonElement);
        }

        foreach (XElement charElement in root.Elements("chars").Elements("char"))
        {
            Chars.Add(new FontFileCharLine(charElement));
        }

        foreach (XElement kerningElement in root.Elements("kernings").Elements("kerning"))
        {
            Kernings.Add(new FontFileKerningLine(kerningElement));
        }

        foreach (XElement pageElement in root.Elements("pages").Elements("page"))
        {
            Pages.Add(new FontFilePage(pageElement));
        }

        if (Info == null || Common == null)
        {
            throw new InvalidOperationException("Font file did not have an info or common tag");
        }
    }

    private void ParseBinaryText(string contents)
    {

    }

    private void ParsePlainText(string contents)
    {

        var index = 0;
        while (index < contents.Length)
        {
            var (parsedLine, nextIndex) = ParsedFontLine.Parse(contents, index);
            index = nextIndex;
            if (parsedLine != null)
            {
                switch (parsedLine.Tag)
                {
                    case "info":
                        Info = new FontFileInfoLine(parsedLine);
                        break;

                    case "common":
                        Common = new FontFileCommonLine(parsedLine);
                        break;

                    case "char":
                        Chars.Add(new FontFileCharLine(parsedLine));
                        break;

                    case "kerning":
                        Kernings.Add(new FontFileKerningLine(parsedLine));
                        break;

                    default:
                        break; // ignore unknown tags
                }
            }
        }

        GetFontFileTextures(contents);

        if (Info == null || Common == null)
        {
            throw new InvalidOperationException("Font file did not have an info or common tag");
        }
    }

    private void GetFontFileTextures(string fontPattern)
    {
        int currentIndexIntoFile = fontPattern.IndexOf("page id=");

        while (currentIndexIntoFile != -1)
        {
            // Right now we'll assume that the pages come in order and they're sequential
            // If this isn' the case then the logic may need to be modified to support this
            // instead of just returning a string[].
            int page = StringFunctions.GetIntAfter("page id=", fontPattern, currentIndexIntoFile);

            int openingQuotesIndex = fontPattern.IndexOf('"', currentIndexIntoFile);

            int closingQuotesIndex = fontPattern.IndexOf('"', openingQuotesIndex + 1);

            string textureName = fontPattern.Substring(openingQuotesIndex + 1, closingQuotesIndex - openingQuotesIndex - 1);

            Pages.Add(new FontFilePage(page, textureName));

            currentIndexIntoFile = fontPattern.IndexOf("page id=", closingQuotesIndex);
        }
    }
}

public class FontFilePage
{
    public int Id { get; set; }
    public string File { get; set; }

    public FontFilePage(int id, string file)
    {
        Id = id;
        File = file;
    }

    public FontFilePage(XElement pageElement)
    {
        Id = (int?)pageElement.Attribute("id") ?? 0;
        File = (string?)pageElement.Attribute("file") ?? "";
    }
}

public class FontFileInfoLine
{
    public int Outline { get; set; }
    public int Size { get; set; }

    public FontFileInfoLine(ParsedFontLine line)
    {
        if (line.NumericAttributes.ContainsKey("outline"))
        {
            Outline = line.NumericAttributes["outline"];
        }
        if (line.NumericAttributes.ContainsKey("size"))
        {
            Size = System.Math.Abs(line.NumericAttributes["size"]);
        }
    }

    public FontFileInfoLine(XElement infoElement)
    {
        Outline = (int?)infoElement.Attribute("outline") ?? 0;
        Size = System.Math.Abs((int?)infoElement.Attribute("size") ?? 0);
    }
}

public class FontFileCommonLine
{
    public int LineHeight { get; set; }
    public int Base { get; set; }

    public FontFileCommonLine(ParsedFontLine line)
    {
        LineHeight = line.NumericAttributes["lineheight"];
        Base = line.NumericAttributes["base"];
    }

    public FontFileCommonLine(XElement commonElement)
    {
        LineHeight = (int?)commonElement.Attribute("lineHeight") ?? 0;
        Base = (int?)commonElement.Attribute("base") ?? 0;
    }
}

public class FontFileCharLine
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int XAdvance { get; set; }
    public int Page { get; set; }

    public FontFileCharLine() { }

    public FontFileCharLine(ParsedFontLine line)
    {
        Id = line.NumericAttributes["id"];
        X = line.NumericAttributes["x"];
        Y = line.NumericAttributes["y"];
        Width = line.NumericAttributes["width"];
        Height = line.NumericAttributes["height"];
        XOffset = line.NumericAttributes["xoffset"];
        YOffset = line.NumericAttributes["yoffset"];
        XAdvance = line.NumericAttributes["xadvance"];
        if (line.NumericAttributes.ContainsKey("page"))
        {
            Page = line.NumericAttributes["page"];
        }
    }

    public FontFileCharLine(XElement charElement)
    {
        Id = (int?)charElement.Attribute("id") ?? 0;
        X = (int?)charElement.Attribute("x") ?? 0;
        Y = (int?)charElement.Attribute("y") ?? 0;
        Width = (int?)charElement.Attribute("width") ?? 0;
        Height = (int?)charElement.Attribute("height") ?? 0;
        XOffset = (int?)charElement.Attribute("xoffset") ?? 0;
        YOffset = (int?)charElement.Attribute("yoffset") ?? 0;
        XAdvance = (int?)charElement.Attribute("xadvance") ?? 0;
        Page = (int?)charElement.Attribute("page") ?? 0;
    }

    public override string ToString()
    {
        return (char)Id + " on page " + Page;
    }
}

public class FontFileKerningLine
{
    public int First { get; set; }
    public int Second { get; set; }
    public int Amount { get; set; }

    public FontFileKerningLine(ParsedFontLine line)
    {
        First = line.NumericAttributes["first"];
        Second = line.NumericAttributes["second"];
        Amount = line.NumericAttributes["amount"];
    }

    public FontFileKerningLine(XElement kerningElement)
    {
        First = (int?)kerningElement.Attribute("first") ?? 0;
        Second = (int?)kerningElement.Attribute("second") ?? 0;
        Amount = (int?)kerningElement.Attribute("amount") ?? 0;
    }
}

public class ParsedFontLine
{
    public string Tag { get; }
    public Dictionary<string, int> NumericAttributes { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private ParsedFontLine(string tag)
    {
        Tag = tag;
    }

    public static (ParsedFontLine line, int nextIndex) Parse(string contents, int startIndex)
    {
        var parsedLine = (ParsedFontLine)null;
        var currentAttributeName = (string)null;
        var wordStartIndex = (int?)null;
        var isInQuotes = false;
        var index = startIndex;

        void ProcessWord()
        {
            if (wordStartIndex == null)
            {
                return;
            }

            var length = index - wordStartIndex.Value;
            var word = contents.Substring(wordStartIndex.Value, length);
            if (parsedLine == null)
            {
                parsedLine = new ParsedFontLine(word);
            }
            else if (currentAttributeName == null)
            {
                currentAttributeName = word;
            }
            else
            {
                if (int.TryParse(word, out var number))
                {
                    parsedLine.NumericAttributes[currentAttributeName] = number;
                }
                else if (int.TryParse(word, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out number))
                {
                    // Fallback for other cultures that expect a comma, but a period was found
                    parsedLine.NumericAttributes[currentAttributeName] = number;
                }

                currentAttributeName = null;
            }

            wordStartIndex = null;
        }

        while (index < contents.Length)
        {
            var character = contents[index];
            if (char.IsWhiteSpace(character) || character == '=')
            {
                if (!isInQuotes && wordStartIndex != null)
                {
                    // Hit the end of a word
                    ProcessWord();
                }

                if (character == '\r' || character == '\n')
                {
                    return (parsedLine, index + 1);
                }
            }
            else
            {
                wordStartIndex = wordStartIndex ?? index;
                if (character == '"' && !isInQuotes)
                {
                    isInQuotes = true;
                }
                else if (character == '"' && isInQuotes)
                {
                    isInQuotes = false;
                    currentAttributeName = null;
                    wordStartIndex = null; // ignore string attributes for now, we only use numerics
                }
            }

            index++;
        }

        // Hit the end of the string
        ProcessWord();

        return (parsedLine, index);
    }
}
