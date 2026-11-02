using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// GUM0016 (#7059): a derived screen or component whose file lacks its base's instances with
/// <c>DefinedByBase</c> true. The load repairs it in memory, so <c>gumcli check</c> only warns.
/// </summary>
public class CheckMissingBaseInstanceTests : CheckHandWrittenFileHarness
{
    #region Derived component missing DefinedByBase instances (GUM0016)

    private string CreateButtonProject(string name, string derivedBody)
    {
        string project = CreateProject(name);
        AddComponent(project, "ButtonBase", "Container",
            DefaultState() + Instance("TextInstance", "Text") + Instance("Background", "NineSlice"));
        AddComponent(project, "MyButton", "ButtonBase", derivedBody);
        return project;
    }

    [Fact]
    public void Check_DerivedComponentMissingBaseInstances_ShouldReportGum0016PerInstanceAsWarning()
    {
        string project = CreateButtonProject("MissingBaseInstances", DefaultState());

        CheckRun run = Check(project);

        List<CheckEntry> entries = run.Entries.Where(e => e.Code == "GUM0016").ToList();
        entries.Count.ShouldBe(2);
        entries.ShouldAllBe(e => e.Element == "MyButton" && e.Severity == "Warning");
        entries.ShouldContain(e => e.Message.Contains("TextInstance"));
        entries.ShouldContain(e => e.Message.Contains("Background"));
        run.ExitCode.ShouldBe(0);
    }

    [Fact]
    public void Check_DerivedComponentMissingBaseInstances_MessageShouldNameBaseAndSayHowToFix()
    {
        string project = CreateButtonProject("MissingBaseInstancesMessage", DefaultState());

        CheckEntry entry = Check(project).Entries.First(e => e.Code == "GUM0016");

        entry.Message.ShouldContain("ButtonBase");
        entry.Message.ShouldContain("DefinedByBase");
        entry.Message.ShouldContain("resave");
    }

    [Fact]
    public void Check_DerivedComponentListingAllBaseInstances_ShouldNotReportGum0016()
    {
        string project = CreateButtonProject("ListsBaseInstances",
            DefaultState() +
            Instance("TextInstance", "Text", definedByBase: true) +
            Instance("Background", "NineSlice", definedByBase: true));

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_DerivedComponentListingSomeBaseInstances_ShouldReportOnlyTheMissingOne()
    {
        string project = CreateButtonProject("ListsSomeBaseInstances",
            DefaultState() + Instance("TextInstance", "Text", definedByBase: true));

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Code.ShouldBe("GUM0016");
        entry.Message.ShouldContain("Background");
    }

    [Fact]
    public void Check_DerivedFromGrandparent_ShouldReportInstancesFromEveryAncestor()
    {
        string project = CreateButtonProject("Grandparent",
            DefaultState() +
            Instance("TextInstance", "Text", definedByBase: true) +
            Instance("Background", "NineSlice", definedByBase: true));
        AddComponent(project, "SpecialButton", "MyButton", DefaultState());

        List<CheckEntry> entries = Check(project).Entries.Where(e => e.Element == "SpecialButton").ToList();

        entries.Count.ShouldBe(2);
        entries.ShouldAllBe(e => e.Code == "GUM0016");
    }

    [Fact]
    public void Check_ScreenDerivedFromScreenMissingBaseInstances_ShouldReportGum0016()
    {
        string project = CreateProject("ScreenBase");
        AddScreen(project, "BaseScreen", null, DefaultState() + Instance("Logo", "Sprite"));
        AddScreen(project, "DerivedScreen", "BaseScreen", DefaultState());

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Code.ShouldBe("GUM0016");
        entry.Element.ShouldBe("DerivedScreen");
    }

    [Fact]
    public void Check_DerivedInstanceDiffersOnlyByCase_ShouldStillReportGum0016()
    {
        // Instance names are case-sensitive everywhere in Gum (GetInstance, FindByName, GUM0020), so
        // "textinstance" is a different instance and the base's "TextInstance" is still missing.
        string project = CreateButtonProject("CaseDiffers",
            DefaultState() + Instance("textinstance", "Text", definedByBase: true) + Instance("Background", "NineSlice", definedByBase: true));

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Code.ShouldBe("GUM0016");
        entry.Message.ShouldContain("'TextInstance'");
    }

    [Fact]
    public void Check_CircularBaseTypes_ShouldNotReportGum0016()
    {
        string project = CreateProject("CircularBases");
        AddComponent(project, "A", "B", DefaultState() + Instance("Child", "Text"));
        AddComponent(project, "B", "A", DefaultState());

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0016");
    }

    [Fact]
    public void Check_BaseWithoutBaseType_ShouldReportGum0015ButNotGum0016()
    {
        string project = CreateProject("BaseHasNoBase");
        AddComponent(project, "Orphan", null, DefaultState() + Instance("Child", "Text"));
        AddComponent(project, "Kid", "Orphan", DefaultState());

        List<CheckEntry> entries = Check(project).Entries.ToList();

        entries.ShouldContain(e => e.Code == "GUM0015");
        entries.ShouldNotContain(e => e.Code == "GUM0016");
    }

    [Fact]
    public void Check_BaseWithUnknownBaseType_ShouldNotReportGum0016()
    {
        string project = CreateProject("BaseHasUnknownBase");
        AddComponent(project, "Mid", "DoesNotExist", DefaultState() + Instance("Child", "Text"));
        AddComponent(project, "Kid", "Mid", DefaultState());

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0016");
    }

    [Fact]
    public void Resave_StaleDerivedFile_ShouldWriteTheBaseInstanceSoCheckNoLongerWarns()
    {
        string project = CreateButtonProject("ResaveStale", DefaultState());
        string derivedFile = Path.Combine(Path.GetDirectoryName(project)!, "Components", "MyButton.gucx");
        File.ReadAllText(derivedFile).ShouldNotContain("TextInstance");

        CliTestHelper.Run("resave", project).ExitCode.ShouldBe(0);

        File.ReadAllText(derivedFile).ShouldContain("TextInstance");
        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0016");
    }

    #endregion
}
