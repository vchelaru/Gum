using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
        XmlSerializer serializer = FileManager.GetXmlSerializer(typeof(XMLFont));
        using var reader = new StringReader(contents);
        var xmlFont = (XMLFont?)serializer.Deserialize(reader);

        if (xmlFont == null)
        {
            throw new InvalidOperationException("Unable to load XML Font file, deserialization failed!");
        }

        if (xmlFont.Info != null)
        {
            Info = new FontFileInfoLine(xmlFont);
        }

        if (xmlFont.Common != null)
        {
            Common = new FontFileCommonLine(xmlFont);
        }

        foreach (XMLFont.XMLChar charLine in xmlFont.Chars)
        {
            Chars.Add(new FontFileCharLine(charLine));
        }

        foreach (XMLFont.XMLKerning kerningLine in xmlFont.Kernings)
        {
            Kernings.Add(new FontFileKerningLine(kerningLine));
        }

        foreach (XMLFont.XMLPage page in xmlFont.Pages)
        {
            Pages.Add(new FontFilePage(page));
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

    public FontFilePage(XMLFont.XMLPage page)
    {
        Id = page.Id;
        File = page.File;
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

    public FontFileInfoLine(XMLFont xmlFont)
    {
        if (xmlFont.Info != null)
        {
            Outline = xmlFont.Info.Outline;
            Size = xmlFont.Info.Size;
        }
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

    public FontFileCommonLine(XMLFont xmlFont)
    {
        if (xmlFont.Common != null)
        {
            LineHeight = xmlFont.Common.LineHeight;
            Base = xmlFont.Common.Base;
        }
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

    public FontFileCharLine(XMLFont.XMLChar charLine)
    {
        Id = charLine.Id;
        X = charLine.X;
        Y = charLine.Y;
        Width = charLine.Width;
        Height = charLine.Height;
        XOffset = charLine.XOffset;
        YOffset = charLine.YOffset;
        XAdvance = charLine.XAdvance;
        Page = charLine.Page;
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

    public FontFileKerningLine(XMLFont.XMLKerning kerningLine)
    {
        First = kerningLine.First;
        Second = kerningLine.Second;
        Amount = kerningLine.Amount;
    }
}


// The below classes are entirely to import the BMFont XML format
// https://www.angelcode.com/products/bmfont/doc/file_format.html
[XmlRoot("font")]
public class XMLFont
{
    [XmlElement("info")]
    public XMLInfo Info { get; set; }

    [XmlElement("common")]
    public XMLCommon Common { get; set; }

    [XmlArray("pages")]
    [XmlArrayItem("page")]
    public List<XMLPage> Pages { get; set; }

    [XmlArray("chars")]
    [XmlArrayItem("char")]
    public List<XMLChar> Chars { get; set; }

    [XmlArray("kernings")]
    [XmlArrayItem("kerning")]
    public List<XMLKerning> Kernings { get; set; }

    [XmlType("info")]
    public class XMLInfo
    {
        [XmlAttribute("face")] public string Face { get; set; }
        [XmlAttribute("size")] public int Size { get; set; }
        [XmlAttribute("bold")] public int Bold { get; set; }
        [XmlAttribute("italic")] public int Italic { get; set; }
        [XmlAttribute("charset")] public string Charset { get; set; }
        [XmlAttribute("unicode")] public int Unicode { get; set; }
        [XmlAttribute("stretchH")] public int StretchH { get; set; }
        [XmlAttribute("smooth")] public int Smooth { get; set; }
        [XmlAttribute("aa")] public int Aa { get; set; }
        [XmlAttribute("padding")] public string Padding { get; set; }
        [XmlAttribute("spacing")] public string Spacing { get; set; }
        [XmlAttribute("outline")] public int Outline { get; set; }
    }

    [XmlType("common")]
    public class XMLCommon
    {
        [XmlAttribute("lineHeight")] public int LineHeight { get; set; }
        [XmlAttribute("base")] public int Base { get; set; }
        [XmlAttribute("scaleW")] public int ScaleW { get; set; }
        [XmlAttribute("scaleH")] public int ScaleH { get; set; }
        [XmlAttribute("pages")] public int Pages { get; set; }
        [XmlAttribute("packed")] public int Packed { get; set; }
        [XmlAttribute("alphaChnl")] public int AlphaChnl { get; set; }
        [XmlAttribute("redChnl")] public int RedChnl { get; set; }
        [XmlAttribute("greenChnl")] public int GreenChnl { get; set; }
        [XmlAttribute("blueChnl")] public int BlueChnl { get; set; }
    }

    [XmlType("page")]
    public class XMLPage
    {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("file")] public string File { get; set; }
    }

    [XmlType("char")]
    public class XMLChar
    {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("x")] public int X { get; set; }
        [XmlAttribute("y")] public int Y { get; set; }
        [XmlAttribute("width")] public int Width { get; set; }
        [XmlAttribute("height")] public int Height { get; set; }
        [XmlAttribute("xoffset")] public int XOffset { get; set; }
        [XmlAttribute("yoffset")] public int YOffset { get; set; }
        [XmlAttribute("xadvance")] public int XAdvance { get; set; }
        [XmlAttribute("page")] public int Page { get; set; }
        [XmlAttribute("chnl")] public int Chnl { get; set; }
    }

    [XmlType("kerning")]
    public class XMLKerning
    {
        [XmlAttribute("first")] public int First { get; set; }
        [XmlAttribute("second")] public int Second { get; set; }
        [XmlAttribute("amount")] public int Amount { get; set; }
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
