using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;
using System.Buffers.Binary;
using System.Text;
using static Raylib_cs.Raylib;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #6567: a TTF loaded with LoadFontEx has no .fnt metrics, so its baseline must come from the font itself.
/// raylib scales a TTF so ascent minus descent (the hhea table) spans the requested size and draws the baseline
/// at the truncated scaled ascent; the expected value here is computed from the file's hhea table directly.
/// </summary>
public class TextTtfBaselineTests : BaseTestClass
{
    [Theory]
    [InlineData("Orbitron-Black.ttf", 24)]
    [InlineData("Orbitron-Black.ttf", 48)]
    [InlineData("OpenSans-Regular.ttf", 24)]
    [InlineData("OpenSans-Regular.ttf", 37)]
    public void Baseline_FontFromLoadFontEx_EqualsTheTtfScaledAscent(string fileName, int size)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", fileName);
        (short ascent, short descent) = ReadHheaAscentDescent(File.ReadAllBytes(path));
        int expectedBaselineY = (int)(ascent * (size / (float)(ascent - descent)));

        Font font = LoadFontEx(path, size, null, 0);
        try
        {
            font.BaseSize.ShouldBe(size);
            TextRuntime runtime = new();
            Text text = (Text)runtime.RenderableComponent!;
            text.Font = font;

            text.LineHeightInPixels.ShouldBe(size);
            (text.LineHeightInPixels - text.DescenderHeight).ShouldBe(expectedBaselineY);
        }
        finally
        {
            UnloadFont(font);
        }
    }

    [Fact]
    public void DescenderHeight_RaylibDefaultFont_StaysTwo()
    {
        TextRuntime runtime = new();
        Text text = (Text)runtime.RenderableComponent!;
        text.Font = GetFontDefault();

        text.DescenderHeight.ShouldBe(2);
    }

    private static (short ascent, short descent) ReadHheaAscentDescent(byte[] bytes)
    {
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4));
        for (int i = 0; i < tableCount; i++)
        {
            int record = 12 + 16 * i;
            if (Encoding.ASCII.GetString(bytes, record, 4) == "hhea")
            {
                int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(record + 8));
                return (BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(offset + 4)),
                    BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(offset + 6)));
            }
        }
        throw new InvalidDataException("No hhea table");
    }
}
