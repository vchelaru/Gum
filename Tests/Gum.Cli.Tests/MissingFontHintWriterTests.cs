using Gum.Cli.Commands;
using Shouldly;

namespace Gum.Cli.Tests;

public class MissingFontHintWriterTests
{
    private const string Warning =
        "[Gum] No usable font could be resolved for 'FontCache\\{0}' (in-memory creator, disk cache, and font service all failed) - falling back to the default font.";

    [Fact]
    public void WriteLine_PassesEveryLineThroughUnchanged()
    {
        StringWriter inner = new StringWriter();
        MissingFontHintWriter writer = new MissingFontHintWriter(inner);

        writer.WriteLine("other");
        writer.WriteLine(string.Format(Warning, "Font14Arial.fnt"));

        inner.ToString().ShouldBe("other" + Environment.NewLine
            + string.Format(Warning, "Font14Arial.fnt") + Environment.NewLine);
    }

    [Fact]
    public void MissingFonts_RepeatedWarnings_ListsEachFileOnce()
    {
        MissingFontHintWriter writer = new MissingFontHintWriter(new StringWriter());

        writer.WriteLine(string.Format(Warning, "Font14Arial.fnt"));
        writer.WriteLine(string.Format(Warning, "Font14Arial.fnt"));
        writer.WriteLine(string.Format(Warning, "Font28Arial_Bold.fnt"));

        writer.MissingFonts.ShouldBe(new[] { "FontCache\\Font14Arial.fnt", "FontCache\\Font28Arial_Bold.fnt" });
    }

    [Fact]
    public void MissingFonts_WarningWrittenInPieces_IsStillDetected()
    {
        MissingFontHintWriter writer = new MissingFontHintWriter(new StringWriter());

        writer.Write("[Gum] No usable font could be resolved for 'FontCache\\Font14Arial.fnt' (x)");
        writer.Write('\n');

        writer.MissingFonts.Count.ShouldBe(1);
    }

    [Fact]
    public void MissingFonts_UnrelatedLines_IsEmpty()
    {
        MissingFontHintWriter writer = new MissingFontHintWriter(new StringWriter());

        writer.WriteLine("[Gum] something else failed for 'x.fnt'");

        writer.MissingFonts.ShouldBeEmpty();
    }

    [Fact]
    public void BuildHint_NamesFilesAndTheFontsCommand()
    {
        string hint = MissingFontHintWriter.BuildHint(
            new[] { "FontCache\\Font14Arial.fnt", "FontCache\\Font28Arial_Bold.fnt" }, "C:\\p\\My Game.gumx");

        hint.ShouldBe(
            "hint: 2 font files are missing from the project's FontCache, so text drew in the default font "
            + "(FontCache\\Font14Arial.fnt, FontCache\\Font28Arial_Bold.fnt). "
            + "Run `gumcli fonts \"C:\\p\\My Game.gumx\"` to generate them, then screenshot again.");
    }

    [Fact]
    public void BuildHint_WithSetOverrides_SaysFontsOnlyAnOverrideNeedsAreMadeWithFontAndSize()
    {
        string hint = MissingFontHintWriter.BuildHint(new[] { "FontCache\\Font41Arial.fnt" }, "p.gumx", hasSetOverrides: true);

        hint.ShouldEndWith("A font or size that only a --set override uses is not in the project: "
            + "generate it with `gumcli fonts \"p.gumx\" --font <name> --size <n>`.");
    }

    [Fact]
    public void BuildHint_ManyFiles_NamesFirstFiveAndCountsTheRest()
    {
        string[] files = Enumerable.Range(1, 8).Select(i => $"FontCache\\F{i}.fnt").ToArray();

        string hint = MissingFontHintWriter.BuildHint(files, "p.gumx");

        hint.ShouldContain("FontCache\\F5.fnt and 3 more)");
        hint.ShouldNotContain("F6.fnt");
    }
}
