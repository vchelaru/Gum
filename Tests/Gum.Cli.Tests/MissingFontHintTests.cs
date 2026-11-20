using Gum.Cli.Commands;
using Shouldly;

namespace Gum.Cli.Tests;

public class MissingFontHintTests
{
    private const string Warning =
        "No usable font could be resolved for 'FontCache\\{0}' (in-memory creator, disk cache, and font service all failed) - falling back to the default font.";

    [Fact]
    public void Files_RepeatedWarnings_ListsEachFileOnce()
    {
        MissingFontHint hint = new MissingFontHint();

        hint.Add(string.Format(Warning, "Font14Arial.fnt"));
        hint.Add(string.Format(Warning, "Font14Arial.fnt"));
        hint.Add(string.Format(Warning, "Font28Arial_Bold.fnt"));

        hint.Files.ShouldBe(new[] { "FontCache\\Font14Arial.fnt", "FontCache\\Font28Arial_Bold.fnt" });
    }

    [Fact]
    public void Files_UnrelatedMessages_IsEmpty()
    {
        MissingFontHint hint = new MissingFontHint();

        hint.Add("something else failed for 'x.fnt'");

        hint.Files.ShouldBeEmpty();
    }

    [Fact]
    public void BuildHint_NamesFilesAndTheFontsCommand()
    {
        string text = MissingFontHint.BuildHint(
            new[] { "FontCache\\Font14Arial.fnt", "FontCache\\Font28Arial_Bold.fnt" }, "C:\\p\\My Game.gumx");

        text.ShouldBe(
            "hint: 2 font files are missing from FontCache (FontCache\\Font14Arial.fnt, FontCache\\Font28Arial_Bold.fnt), "
            + "so text drew in the default font. Run `gumcli fonts \"C:\\p\\My Game.gumx\"` to generate them, then screenshot again.");
    }

    [Fact]
    public void BuildHint_WithSetOverrides_SaysASetOnlySizeNeedsFontAndSize()
    {
        string text = MissingFontHint.BuildHint(new[] { "FontCache\\Font41Arial.fnt" }, "p.gumx", hasSetOverrides: true);

        text.ShouldEndWith("A size that only --set uses also needs `--font <name> --size <n>` on that command.");
        text.Split("p.gumx").Length.ShouldBe(2, "the project path appears once");
    }

    [Fact]
    public void BuildHint_ManyFiles_NamesFirstFiveAndCountsTheRest()
    {
        string[] files = Enumerable.Range(1, 8).Select(i => $"FontCache\\F{i}.fnt").ToArray();

        string text = MissingFontHint.BuildHint(files, "p.gumx");

        text.ShouldContain("FontCache\\F5.fnt and 3 more)");
        text.ShouldNotContain("F6.fnt");
    }
}
