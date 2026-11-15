using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7059: <c>gumcli check</c> names an unknown BaseType with a stable code (GUM0030), and a derived component
/// whose file lacks a base instance reports that real cause instead of a nonexistent parent.
/// </summary>
public class CheckUnknownBaseTypeTests : CheckHandWrittenFileHarness
{
    #region Unknown BaseType (GUM0030)

    [Fact]
    public void Check_ComponentWithUnknownBaseType_ShouldReportGum0030AsError()
    {
        string project = CreateProject("UnknownBase");
        AddComponent(project, "Lost", "DoesNotExist", DefaultState());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        CheckEntry entry = run.Entries.ShouldHaveSingleItem();
        entry.Code.ShouldBe("GUM0030");
        entry.Severity.ShouldBe("Error");
        entry.Element.ShouldBe("Lost");
        entry.Message.ShouldContain("DoesNotExist");
    }

    [Fact]
    public void Check_ScreenWithUnknownBaseType_ShouldReportGum0030()
    {
        string project = CreateProject("UnknownScreenBase");
        AddScreen(project, "Lost", "DoesNotExist", DefaultState());

        Check(project).Entries.ShouldHaveSingleItem().Code.ShouldBe("GUM0030");
    }

    [Theory]
    [InlineData("Container")]
    [InlineData("Text")]
    public void Check_ComponentWithStandardBaseType_ShouldStaySilent(string baseType)
    {
        string project = CreateProject("StandardBase" + baseType);
        AddComponent(project, "Fine", baseType, DefaultState());

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_ComponentDerivedFromAnotherComponent_ShouldStaySilent()
    {
        string project = CreateProject("ComponentBase");
        AddComponent(project, "Parent", "Container", DefaultState());
        AddComponent(project, "Child", "Parent", DefaultState());

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_ComponentWithoutBaseType_ShouldReportGum0015NotGum0030()
    {
        string project = CreateProject("NoBaseNotUnknown");
        AddComponent(project, "Orphan", baseType: null, DefaultState());

        Check(project).Entries.ShouldHaveSingleItem().Code.ShouldBe("GUM0015");
    }

    #endregion

    #region Missing base instance named as the cause

    [Fact]
    public void Check_DerivedInstanceParentedToMissingBaseInstance_ShouldReportOnlyGum0016()
    {
        string project = CreateProject("ParentIsBaseInstance");
        AddComponent(project, "ButtonBase", "Container", DefaultState() + Instance("Holder", "Container"));
        AddComponent(project, "MyButton", "ButtonBase",
            DefaultState(Variable("string", "Extra.Parent", "string", "Holder")) + Instance("Extra", "Text"));

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Code == "GUM0016" && e.Message.Contains("Holder"));
        run.Entries.ShouldNotContain(e => e.Code != "GUM0016");
    }

    #endregion
}
