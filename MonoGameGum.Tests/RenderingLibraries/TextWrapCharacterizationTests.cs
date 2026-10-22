using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins the wrapped lines, measured width and height of <see cref="Text"/> for a table of inputs, so
/// the allocation work in #6420 cannot change what wraps where. The expected values were recorded from
/// the implementation before that change.
/// </summary>
public class TextWrapCharacterizationTests : IDisposable
{
    private readonly bool _originalMidWord = Text.IsMidWordLineBreakEnabled;

    public void Dispose() => Text.IsMidWordLineBreakEnabled = _originalMidWord;

    public sealed record Case(
        string Name,
        string? Raw,
        float? Width = null,
        int? MaxLines = null,
        bool Ellipsis = false,
        bool MidWord = true,
        bool FontScaleRun = false,
        float? TruncateHeight = null);

    public static readonly Case[] Cases =
    {
        new("plain-w100", "the quick brown fox jumps over the lazy dog", Width: 100),
        new("plain-w60", "the quick brown fox jumps over the lazy dog", Width: 60),
        new("fits-one-line", "short text", Width: 1000),
        new("no-width", "the quick brown fox", Width: null),
        new("newlines", "line one\nline two\n\nline four", Width: 1000),
        new("newlines-wrapped", "line one is long\nline two is long too\n\nfour", Width: 70),
        new("crlf", "line one\r\nline two", Width: 60),
        new("leading-newline", "\nstart here", Width: 40),
        new("trailing-newline", "end here\n", Width: 1000),
        new("only-newline", "\n", Width: 1000),
        new("double-newline", "abc\n\ndef", Width: 30),
        new("long-word-midword", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa short", Width: 50),
        new("long-word-no-midword", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa short", Width: 50, MidWord: false),
        new("long-word-exact-fit", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", Width: 60),
        new("trailing-space-fits", "trailing space ", Width: 1000),
        new("trailing-space-wrapped", "trailing space ", Width: 40),
        new("leading-double-spaces", "  leading and  double  spaces ", Width: 60),
        new("zero-width-space", "abcdefghij\u200Bklmnopqrstuvwxyz", Width: 60),
        new("hyphens", "well-known state-of-the-art thing", Width: 70),
        new("max-2-lines", "the quick brown fox jumps over the lazy dog", Width: 60, MaxLines: 2),
        new("max-2-lines-ellipsis", "the quick brown fox jumps over the lazy dog", Width: 60, MaxLines: 2, Ellipsis: true),
        new("max-1-line-ellipsis-long-word", "supercalifragilisticexpialidocious word", Width: 60, MaxLines: 1, Ellipsis: true),
        new("max-lines-newline", "a\nb\nc\nd", Width: 1000, MaxLines: 2),
        new("truncate-by-height", "the quick brown fox jumps over the lazy dog", Width: 60, TruncateHeight: 45),
        new("font-scale-run", "the quick brown fox jumps over", Width: 80, FontScaleRun: true),
        new("empty", "", Width: 60),
        new("null", null, Width: 60),
    };

    public static IEnumerable<object[]> CaseNames => Cases.Select(c => new object[] { c.Name });

    public static readonly Dictionary<string, string> Expected = new()
    {
        ["plain-w100"] = "the quick |brown fox |jumps over the |lazy dog #4 w=100 h=80",
        ["plain-w60"] = "the |quick |brown |fox |jumps |over the |lazy dog #7 w=59 h=140",
        ["fits-one-line"] = "short text #1 w=67 h=20",
        ["no-width"] = "the quick brown fox #1 w=135 h=20",
        ["newlines"] = "line one\\n|line two\\n|\\n|line four #4 w=64 h=80",
        ["newlines-wrapped"] = "line one |is long\\n|line two |is long |too\\n|\\n|four #7 w=60 h=140",
        ["crlf"] = "line one\\n|line two #2 w=56 h=40",
        ["leading-newline"] = "\\n|start |here #3 w=40 h=60",
        ["trailing-newline"] = "end here\\n| #2 w=53 h=40",
        ["only-newline"] = "\\n| #2 w=0 h=40",
        ["double-newline"] = "abc\\n|\\n|def #3 w=24 h=60",
        ["long-word-midword"] = "aaaaaaa|aaaaaaa|aaaaaaa|aaaaaaa|aaaaaaa|aaaa |short #7 w=49 h=140",
        ["long-word-no-midword"] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa |short #2 w=277 h=40",
        ["long-word-exact-fit"] = "bbbbbbb|bbbbbbb|bbbbbbb|bbbbbbb|bb #5 w=56 h=100",
        ["trailing-space-fits"] = "trailing space  #1 w=104 h=20",
        ["trailing-space-wrapped"] = "trail|ing |space | #4 w=42 h=80",
        ["leading-double-spaces"] = "  leading |and  |double  |spaces  #4 w=62 h=80",
        ["zero-width-space"] = "abcdefgh|ij|klmnopqr|stuvwxyz #4 w=60 h=80",
        ["hyphens"] = "well-know|n |state-of-|the-art |thing #5 w=70 h=100",
        ["max-2-lines"] = "the |quick  #2 w=43 h=40",
        ["max-2-lines-ellipsis"] = "the |quic... #2 w=54 h=40",
        ["max-1-line-ellipsis-long-word"] = "supe... #1 w=53 h=20",
        ["max-lines-newline"] = "a\\n|b\\n #2 w=8 h=40",
        ["truncate-by-height"] = "the |quick  #2 w=43 h=40",
        ["font-scale-run"] = "the |quick |brow|n fox |jumps over #5 w=86 h=160",
        ["empty"] = " #0 w=0 h=0",
        ["null"] = " #0 w=0 h=0",
    };

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void UpdateWrappedText_ProducesRecordedLinesAndSize(string name)
    {
        Case testCase = Cases.Single(c => c.Name == name);

        string actual = Describe(Build(testCase));

        Expected.ShouldContainKey(name);
        actual.ShouldBe(Expected[name]);
    }

    [Fact]
    public void UpdateWrappedText_AfterWidthAndTextChanges_MatchesAFreshText()
    {
        string[] texts =
        {
            "the quick brown fox jumps over the lazy dog",
            "pack my box with five dozen liquor jugs now",
            "the quick brown fox jumps over the lazy dog",
            "pack my box with five\ndozen liquor jugs now",
            "x",
            "the quick brown fox jumps over the lazy dog",
        };
        float[] widths = { 200, 60, 100, 60, 90, 45 };

        Text reused = new() { BitmapFont = CreateFont() };
        for (int step = 0; step < texts.Length; step++)
        {
            reused.Width = widths[step];
            reused.RawText = texts[step];

            Text fresh = new() { BitmapFont = CreateFont() };
            fresh.Width = widths[step];
            fresh.RawText = texts[step];

            reused.WrappedText.ShouldBe(fresh.WrappedText, $"step {step}");
            reused.WrappedTextWidth.ShouldBe(fresh.WrappedTextWidth, $"step {step}");
            reused.WrappedTextHeight.ShouldBe(fresh.WrappedTextHeight, $"step {step}");
        }
    }

    protected Text Build(Case testCase)
    {
        Text.IsMidWordLineBreakEnabled = testCase.MidWord;
        Text text = new() { BitmapFont = CreateFont() };
        text.Width = testCase.Width;
        text.MaxNumberOfLines = testCase.MaxLines;
        text.IsTruncatingWithEllipsisOnLastLine = testCase.Ellipsis;
        if (testCase.TruncateHeight != null)
        {
            text.Height = testCase.TruncateHeight.Value;
            text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        }
        if (testCase.FontScaleRun)
        {
            text.InlineVariables.Add(new InlineVariable
            {
                VariableName = "FontScale",
                Value = 2f,
                StartIndex = 4,
                CharacterCount = 11
            });
        }
        text.RawText = testCase.Raw;
        if (testCase.FontScaleRun)
        {
            text.UpdateWrappedText();
        }
        return text;
    }

    public static string Describe(Text text)
    {
        string lines = string.Join("|", text.WrappedText.Select(l => l.Replace("\n", "\\n").Replace("\u200B", "\\u200B")));
        return $"{lines} #{text.WrappedText.Count} w={text.WrappedTextWidth} h={text.WrappedTextHeight}";
    }

    public static BitmapFont CreateFont()
    {
        StringBuilder sb = new();
        sb.AppendLine("info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0");
        sb.AppendLine("common lineHeight=20 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
        sb.AppendLine("page id=0 file=\"x.png\"");
        string alphabet = " abcdefghijklmnopqrstuvwxyz0123456789-,.";
        sb.AppendLine($"chars count={alphabet.Length + 1}");
        foreach (char c in alphabet)
        {
            int advance = c == ' ' ? 4 : 6 + (c % 4);
            sb.AppendLine($"char id={(int)c} x=0 y=0 width={advance} height=13 xoffset=0 yoffset=4 xadvance={advance} page=0 chnl=15");
        }
        sb.AppendLine("char id=8203 x=0 y=0 width=0 height=13 xoffset=0 yoffset=4 xadvance=0 page=0 chnl=15");
        BitmapFont font = new((Texture2D)null!, sb.ToString());
        font.SetFontPattern(256, 256);
        return font;
    }
}
