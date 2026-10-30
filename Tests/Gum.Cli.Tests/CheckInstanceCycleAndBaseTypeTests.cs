using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli check</c> on hand-written files: a component that instantiates itself (#7125) and a
/// component with no BaseType (#7161).
/// </summary>
public class CheckInstanceCycleAndBaseTypeTests : CheckHandWrittenFileHarness
{
    #region Instance cycles (GUM0018)

    [Fact]
    public void Check_ComponentInstantiatingItself_ShouldReportGum0018InsteadOfCrashing()
    {
        string project = CreateProject("Recursive");
        AddComponent(project, "Matryoshka", "Container", DefaultState() + Instance("Inner", "Matryoshka"));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        CheckEntry entry = run.Entries.ShouldHaveSingleItem();
        entry.Code.ShouldBe("GUM0018");
        entry.Severity.ShouldBe("Error");
        entry.Element.ShouldBe("Matryoshka");
    }

    [Fact]
    public void Check_TwoComponentsInstantiatingEachOther_ShouldReportBothAndNotCrash()
    {
        string project = CreateProject("MutualRecursion");
        AddComponent(project, "A", "Container", DefaultState() + Instance("ToB", "B"));
        AddComponent(project, "B", "Container", DefaultState() + Instance("ToA", "A"));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.Where(e => e.Code == "GUM0018").Select(e => e.Element).OrderBy(n => n).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public void Check_ComponentOnlyUsingACycle_ShouldNotBeReportedAsPartOfIt()
    {
        string project = CreateProject("UsesCycle");
        AddComponent(project, "A", "Container", DefaultState() + Instance("ToB", "B"));
        AddComponent(project, "B", "Container", DefaultState() + Instance("ToA", "A"));
        AddComponent(project, "Outside", "Container", DefaultState() + Instance("Cycle", "A"));

        Check(project).Entries.ShouldNotContain(e => e.Element == "Outside" && e.Code == "GUM0018");
    }

    #endregion

    #region Component without BaseType (GUM0015)

    [Fact]
    public void Check_ComponentWithoutBaseType_ShouldReportGum0015AsError()
    {
        string project = CreateProject("NoBase");
        AddComponent(project, "Orphan", baseType: null, DefaultState());

        CheckRun run = Check(project);

        CheckEntry entry = run.Entries.ShouldHaveSingleItem();
        entry.Code.ShouldBe("GUM0015");
        entry.Severity.ShouldBe("Error");
        entry.Element.ShouldBe("Orphan");
        entry.Message.ShouldContain("Container");
        run.ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_ComponentWithEmptyBaseType_ShouldReportGum0015()
    {
        string project = CreateProject("EmptyBase");
        AddComponent(project, "Orphan", baseType: "", DefaultState());

        Check(project).Entries.ShouldHaveSingleItem().Code.ShouldBe("GUM0015");
    }

    [Fact]
    public void Check_ComponentWithBaseType_ShouldNotReportGum0015()
    {
        string project = CreateProject("HasBase");
        AddComponent(project, "Fine", baseType: "Container", DefaultState());

        CheckRun run = Check(project);

        run.Entries.ShouldBeEmpty();
        run.ExitCode.ShouldBe(0);
    }

    [Fact]
    public void Check_ScreenWithoutBaseType_ShouldNotReportGum0015()
    {
        string project = CreateProject("ScreenNoBase");
        AddScreen(project, "Menu", baseType: null, DefaultState());

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_ComponentWithoutBaseType_ShouldBeSkippedWithExitCode1()
    {
        string project = CreateProject("NoBaseCodegen");
        AddComponent(project, "Orphan", baseType: null, DefaultState());
        string directory = Path.GetDirectoryName(project)!;
        File.WriteAllText(Path.Combine(directory, "Game.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        CliTestHelper.Run("codegen-init", project);

        CliTestHelper result = CliTestHelper.Run("codegen", project);

        result.ExitCode.ShouldBe(1);
        Directory.GetFiles(directory, "Orphan.Generated.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    #endregion
}
