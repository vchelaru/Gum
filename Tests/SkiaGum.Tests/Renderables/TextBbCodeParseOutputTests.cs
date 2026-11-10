using System.Text;
using Gum.Wireframe;
using Shouldly;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Pins what a Skia <c>Text</c> parses and splits BBCode into: the tags, the styled runs and the revealed runs
/// (#7846). The expected strings were recorded before the parse and run buffers were reused between changes, so a
/// reused buffer that keeps anything from the previous text fails here. Only values that do not depend on the
/// platform font are recorded; line layout is compared with a fresh Text instead.
/// </summary>
public class TextBbCodeParseOutputTests : BaseTestClass
{
    internal static readonly string[] Cases =
    {
        "",
        "plain text",
        "no tags [here] or [there",
        "[Unknown=1]unknown tag[/Unknown]",
        "[Color=Red]HP[/Color] [IsBold=true]12[/IsBold] [FontSize=24]of[/FontSize] [IsItalic=true]999[/IsItalic]",
        "[Color=Red]outer [IsBold=true]nested [IsItalic=true]deep[/IsItalic] back[/IsBold] tail[/Color] after",
        "[Color=0xFF00FF00]hex[/Color] [Red=200]r[/Red][Green=100]g[/Green][Blue=50]b[/Blue] [OutlineThickness=2]out[/OutlineThickness]",
        "[Color=Red]never closed [IsBold=true]bold[/IsBold] text",
        "a [[Color=Red]escaped[/Color]] bracket",
        "first line\n[FontSize=30]second[/FontSize] line\nthird",
        "[Font=Courier New]mono[/Font] and [FontScale=2]scaled[/FontScale]",
        "Hel[Custom=Bump]lo w[/Custom]orld",
        "[Custom=Swap]ab[/Custom] emoji \U0001F600[Custom=Bump]\U0001F600x[/Custom] done",
        "\U0001F600 [Color=Blue]\U0001F600\U0001F600[/Color] [Glyph=128512]!",
        "[Color=Red]a[/Color]",
        "[Color=Blue]bad[/Color] [FontSize=abc]no size[/FontSize] [IsBold=maybe]x[/IsBold]",
        "tabs\tand\r\nline [Color=Red]breaks[/Color]\r\nend",
    };

    private static readonly int[] RevealCounts = { 0, 1, 3, 7, 12, 40 };

    internal static string Describe(Text text)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("tags:");
        if (text.ParsedBbCodeTags == null)
        {
            builder.Append("null");
        }
        else
        {
            foreach (FoundTag tag in text.ParsedBbCodeTags)
            {
                builder.Append($"{tag.Name}={tag.Open.Argument}@{tag.Open.StartIndex}+{tag.Open.Count}/{tag.Open.StartStrippedIndex}"
                    + $"..{tag.Close.StartIndex}+{tag.Close.Count}/{tag.Close.StartStrippedIndex},");
            }
        }

        builder.Append(" runs:");
        foreach (Text.StyledTextRun run in text.GetStyledRuns())
        {
            AppendRun(builder, run);
        }

        foreach (int count in RevealCounts)
        {
            text.MaxLettersToShow = count;
            builder.Append($" reveal{count}:");
            foreach (Text.StyledTextRun run in text.GetVisibleStyledRuns())
            {
                builder.Append($"[{run.Text.Replace("\n", "\\n")}|{run.Style.FontSize:0.##}|{run.Style.TextColor}]");
            }
        }
        text.MaxLettersToShow = null;
        return builder.ToString();
    }

    private static void AppendRun(StringBuilder builder, Text.StyledTextRun run)
    {
        builder.Append($"[{run.Text.Replace("\n", "\\n")}|{run.Style.FontSize:0.##}|{run.Style.TextColor}|{run.Style.FontWeight}"
            + $"|{(run.Style.FontItalic ? "i" : "-")}|{run.Style.HaloWidth:0.##}|{run.Style.FontFamily}");
        if (!run.Letter.IsIdentity)
        {
            builder.Append($"|{run.Letter.XOffset:0.##},{run.Letter.YOffset:0.##}");
        }
        builder.Append(run.IsHeightRun ? "|H]" : "]");
    }

    private static void RegisterCustomizations()
    {
        Text.Customizations["Bump"] = (index, block) => new LetterCustomization { YOffset = 3, XOffset = 1 };
        Text.Customizations["Swap"] = (index, block) => new LetterCustomization
        {
            ReplacementCharacter = '*',
            Color = System.Drawing.Color.Green,
        };
    }

    private static Text CreateText(string rawText)
    {
        RegisterCustomizations();
        Text text = new Text();
        text.RawText = rawText;
        text.FontSize = 24;
        text.Width = 10000;
        return text;
    }

    [Fact]
    public void Describe_EachCase_MatchesRecordedOutput()
    {
        for (int i = 0; i < Cases.Length; i++)
        {
            Describe(CreateText(Cases[i])).ShouldBe(Expected[i], $"case {i}: {Cases[i]}");
        }
    }

    [Fact]
    public void Describe_OneTextChangedThroughEveryCaseTwice_MatchesRecordedOutput()
    {
        Text changed = CreateText(Cases[4]);
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < Cases.Length; i++)
            {
                changed.RawText = Cases[i];
                Describe(changed).ShouldBe(Expected[i], $"pass {pass}, case {i}: {Cases[i]}");
            }
        }
    }

    [Fact]
    public void Describe_CasesInReverseOrder_MatchesRecordedOutput()
    {
        Text changed = CreateText(Cases[0]);
        for (int i = Cases.Length - 1; i >= 0; i--)
        {
            changed.RawText = Cases[i];
            Describe(changed).ShouldBe(Expected[i], $"case {i}: {Cases[i]}");
        }
    }

    [Fact]
    public void LineBreaks_OneTextChangedThroughEveryCase_MatchesAFreshText()
    {
        Text changed = CreateText(Cases[4]);
        changed.Width = 90;
        for (int i = 0; i < Cases.Length; i++)
        {
            changed.RawText = Cases[i];
            Text fresh = CreateText(Cases[i]);
            fresh.Width = 90;
            changed.GetCachedTextBlock().Lines.Select(line => (line.Start, line.Length)).ToArray()
                .ShouldBe(fresh.GetCachedTextBlock().Lines.Select(line => (line.Start, line.Length)).ToArray(), $"case {i}: {Cases[i]}");
            changed.GetStyledRuns().Select(run => (run.Text, run.Style.LineHeight)).ToArray()
                .ShouldBe(fresh.GetStyledRuns().Select(run => (run.Text, run.Style.LineHeight)).ToArray(), $"case {i}: {Cases[i]}");

            changed.MaxLettersToShow = 9;
            fresh.MaxLettersToShow = 9;
            changed.GetVisibleStyledRuns().Select(run => (run.Text, run.Style.LineHeight)).ToArray()
                .ShouldBe(fresh.GetVisibleStyledRuns().Select(run => (run.Text, run.Style.LineHeight)).ToArray(), $"reveal, case {i}: {Cases[i]}");
            changed.MaxLettersToShow = null;
        }
    }

    private static readonly string[] Expected =
    {
        "tags:null runs:[|24|#ffffffff|400|-|0|Arial] reveal0: reveal1: reveal3: reveal7: reveal12: reveal40:",
        "tags:null runs:[plain text|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[p|24|#ffffffff] reveal3:[pla|24|#ffffffff] reveal7:[plain t|24|#ffffffff] reveal12:[plain text|24|#ffffffff] reveal40:[plain text|24|#ffffffff]",
        "tags: runs:[no tags [here] or [there|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[n|24|#ffffffff] reveal3:[no |24|#ffffffff] reveal7:[no tags|24|#ffffffff] reveal12:[no tags [her|24|#ffffffff] reveal40:[no tags [here] or [there|24|#ffffffff]",
        "tags: runs:[[Unknown=1]unknown tag[/Unknown]|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[[|24|#ffffffff] reveal3:[[Un|24|#ffffffff] reveal7:[[Unknow|24|#ffffffff] reveal12:[[Unknown=1]u|24|#ffffffff] reveal40:[[Unknown=1]unknown tag[/Unknown]|24|#ffffffff]",
        "tags:Color=Red@0\u002B11/0..13\u002B8/2,IsBold=true@22\u002B13/3..37\u002B9/5,FontSize=24@47\u002B13/6..62\u002B11/8,IsItalic=true@74\u002B15/9..92\u002B11/12, runs:[HP|24|#ffff0000|400|-|0|Arial][ |24|#ffffffff|400|-|0|Arial][12|24|#ffffffff|700|-|0|Arial][ |24|#ffffffff|400|-|0|Arial][of|24|#ffffffff|400|-|0|Arial|H][ |24|#ffffffff|400|-|0|Arial][999|24|#ffffffff|400|i|0|Arial] reveal0: reveal1:[H|24|#ffff0000] reveal3:[HP|24|#ffff0000][ |24|#ffffffff] reveal7:[HP|24|#ffff0000][ |24|#ffffffff][12|24|#ffffffff][ |24|#ffffffff][o|24|#ffffffff] reveal12:[HP|24|#ffff0000][ |24|#ffffffff][12|24|#ffffffff][ |24|#ffffffff][of|24|#ffffffff][ |24|#ffffffff][999|24|#ffffffff] reveal40:[HP|24|#ffff0000][ |24|#ffffffff][12|24|#ffffffff][ |24|#ffffffff][of|24|#ffffffff][ |24|#ffffffff][999|24|#ffffffff]",
        "tags:Color=Red@0\u002B11/0..86\u002B8/27,IsBold=true@17\u002B13/6..72\u002B9/22,IsItalic=true@37\u002B15/13..56\u002B11/17, runs:[outer |24|#ffff0000|400|-|0|Arial][nested |24|#ffff0000|700|-|0|Arial][deep|24|#ffff0000|700|i|0|Arial][ back|24|#ffff0000|700|-|0|Arial][ tail|24|#ffff0000|400|-|0|Arial][ after|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[o|24|#ffff0000] reveal3:[out|24|#ffff0000] reveal7:[outer |24|#ffff0000][n|24|#ffff0000] reveal12:[outer |24|#ffff0000][nested|24|#ffff0000] reveal40:[outer |24|#ffff0000][nested |24|#ffff0000][deep|24|#ffff0000][ back|24|#ffff0000][ tail|24|#ffff0000][ after|24|#ffffffff]",
        "tags:Color=0xFF00FF00@0\u002B18/0..21\u002B8/3,Red=200@30\u002B9/4..40\u002B6/5,Green=100@46\u002B11/5..58\u002B8/6,Blue=50@66\u002B9/6..76\u002B7/7,OutlineThickness=2@84\u002B20/8..107\u002B19/11, runs:[hex|24|#ff00ff00|400|-|0|Arial][ |24|#ffffffff|400|-|0|Arial][r|24|#ffc8ffff|400|-|0|Arial][g|24|#ffff64ff|400|-|0|Arial][b|24|#ffffff32|400|-|0|Arial][ |24|#ffffffff|400|-|0|Arial][out|24|#ffffffff|400|-|4|Arial] reveal0: reveal1:[h|24|#ff00ff00] reveal3:[hex|24|#ff00ff00] reveal7:[hex|24|#ff00ff00][ |24|#ffffffff][r|24|#ffc8ffff][g|24|#ffff64ff][b|24|#ffffff32] reveal12:[hex|24|#ff00ff00][ |24|#ffffffff][r|24|#ffc8ffff][g|24|#ffff64ff][b|24|#ffffff32][ |24|#ffffffff][out|24|#ffffffff] reveal40:[hex|24|#ff00ff00][ |24|#ffffffff][r|24|#ffc8ffff][g|24|#ffff64ff][b|24|#ffffff32][ |24|#ffffffff][out|24|#ffffffff]",
        "tags:IsBold=true@24\u002B13/24..41\u002B9/28, runs:[[Color=Red]never closed |24|#ffffffff|400|-|0|Arial][bold|24|#ffffffff|700|-|0|Arial][ text|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[[|24|#ffffffff] reveal3:[[Co|24|#ffffffff] reveal7:[[Color=|24|#ffffffff] reveal12:[[Color=Red]n|24|#ffffffff] reveal40:[[Color=Red]never closed |24|#ffffffff][bold|24|#ffffffff][ text|24|#ffffffff]",
        "tags:Color=Red@3\u002B11/3..21\u002B8/10, runs:[a [|24|#ffffffff|400|-|0|Arial][escaped|24|#ffff0000|400|-|0|Arial][] bracket|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[a|24|#ffffffff] reveal3:[a [|24|#ffffffff] reveal7:[a [|24|#ffffffff][esca|24|#ffff0000] reveal12:[a [|24|#ffffffff][escaped|24|#ffff0000][] |24|#ffffffff] reveal40:[a [|24|#ffffffff][escaped|24|#ffff0000][] bracket|24|#ffffffff]",
        "tags:FontSize=30@11\u002B13/11..30\u002B11/17, runs:[first line\\n|24|#ffffffff|400|-|0|Arial][second|30|#ffffffff|400|-|0|Arial|H][ line\\nthird|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[f|24|#ffffffff] reveal3:[fir|24|#ffffffff] reveal7:[first l|24|#ffffffff] reveal12:[first line\\n|24|#ffffffff][s|30|#ffffffff] reveal40:[first line\\n|24|#ffffffff][second|30|#ffffffff][ line\\n|24|#ffffffff][third|24|#ffffffff]",
        "tags:Font=Courier New@0\u002B18/0..22\u002B7/4,FontScale=2@34\u002B13/9..53\u002B12/15, runs:[mono|24|#ffffffff|400|-|0|Courier New|H][ and |24|#ffffffff|400|-|0|Arial][scaled|48|#ffffffff|400|-|0|Arial|H] reveal0: reveal1:[m|24|#ffffffff] reveal3:[mon|24|#ffffffff] reveal7:[mono|24|#ffffffff][ an|24|#ffffffff] reveal12:[mono|24|#ffffffff][ and |24|#ffffffff][sca|48|#ffffffff] reveal40:[mono|24|#ffffffff][ and |24|#ffffffff][scaled|48|#ffffffff]",
        "tags:Custom=Bump@3\u002B13/3..20\u002B9/7, runs:[Hel|24|#ffffffff|400|-|0|Arial][l|24|#ffffffff|400|-|0|Arial|1,3][o|24|#ffffffff|400|-|0|Arial|1,3][ |24|#ffffffff|400|-|0|Arial|1,3][w|24|#ffffffff|400|-|0|Arial|1,3][orld|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[H|24|#ffffffff] reveal3:[Hel|24|#ffffffff] reveal7:[Hel|24|#ffffffff][l|24|#ffffffff][o|24|#ffffffff][ |24|#ffffffff][w|24|#ffffffff] reveal12:[Hel|24|#ffffffff][l|24|#ffffffff][o|24|#ffffffff][ |24|#ffffffff][w|24|#ffffffff][orld|24|#ffffffff] reveal40:[Hel|24|#ffffffff][l|24|#ffffffff][o|24|#ffffffff][ |24|#ffffffff][w|24|#ffffffff][orld|24|#ffffffff]",
        "tags:Custom=Swap@0\u002B13/0..15\u002B9/2,Custom=Bump@33\u002B13/11..49\u002B9/14, runs:[*|24|#ff008000|400|-|0|Arial][*|24|#ff008000|400|-|0|Arial][ emoji \uD83D\uDE00|24|#ffffffff|400|-|0|Arial][\uD83D\uDE00|24|#ffffffff|400|-|0|Arial|1,3][x|24|#ffffffff|400|-|0|Arial|1,3][ done|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[*|24|#ff008000] reveal3:[*|24|#ff008000][*|24|#ff008000][ |24|#ffffffff] reveal7:[*|24|#ff008000][*|24|#ff008000][ emoj|24|#ffffffff] reveal12:[*|24|#ff008000][*|24|#ff008000][ emoji \uD83D\uDE00|24|#ffffffff][\uD83D\uDE00|24|#ffffffff] reveal40:[*|24|#ff008000][*|24|#ff008000][ emoji \uD83D\uDE00|24|#ffffffff][\uD83D\uDE00|24|#ffffffff][x|24|#ffffffff][ done|24|#ffffffff]",
        "tags:Color=Blue@3\u002B12/3..19\u002B8/7, runs:[\uD83D\uDE00 |24|#ffffffff|400|-|0|Arial][\uD83D\uDE00\uD83D\uDE00|24|#ff0000ff|400|-|0|Arial][ \uD83D\uDE00!|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[\uD83D\uDE00|24|#ffffffff] reveal3:[\uD83D\uDE00 |24|#ffffffff] reveal7:[\uD83D\uDE00 |24|#ffffffff][\uD83D\uDE00\uD83D\uDE00|24|#ff0000ff] reveal12:[\uD83D\uDE00 |24|#ffffffff][\uD83D\uDE00\uD83D\uDE00|24|#ff0000ff][ \uD83D\uDE00!|24|#ffffffff] reveal40:[\uD83D\uDE00 |24|#ffffffff][\uD83D\uDE00\uD83D\uDE00|24|#ff0000ff][ \uD83D\uDE00!|24|#ffffffff]",
        "tags:Color=Red@0\u002B11/0..12\u002B8/1, runs:[a|24|#ffff0000|400|-|0|Arial] reveal0: reveal1:[a|24|#ffff0000] reveal3:[a|24|#ffff0000] reveal7:[a|24|#ffff0000] reveal12:[a|24|#ffff0000] reveal40:[a|24|#ffff0000]",
        "tags:Color=Blue@0\u002B12/0..15\u002B8/3,FontSize=abc@24\u002B14/4..45\u002B11/11,IsBold=maybe@57\u002B14/12..72\u002B9/13, runs:[bad|24|#ff0000ff|400|-|0|Arial][ no size x|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[b|24|#ff0000ff] reveal3:[bad|24|#ff0000ff] reveal7:[bad|24|#ff0000ff][ no |24|#ffffffff] reveal12:[bad|24|#ff0000ff][ no size |24|#ffffffff] reveal40:[bad|24|#ff0000ff][ no size x|24|#ffffffff]",
        "tags:Color=Red@14\u002B11/14..31\u002B8/20, runs:[tabs\tand\\nline |24|#ffffffff|400|-|0|Arial][breaks\\n|24|#ffff0000|400|-|0|Arial][end|24|#ffffffff|400|-|0|Arial] reveal0: reveal1:[t|24|#ffffffff] reveal3:[tab|24|#ffffffff] reveal7:[tabs\tan|24|#ffffffff] reveal12:[tabs\tand\\n|24|#ffffffff][lin|24|#ffffffff] reveal40:[tabs\tand\\n|24|#ffffffff][line |24|#ffffffff][breaks\\n|24|#ffff0000][end|24|#ffffffff]",
    };
}
