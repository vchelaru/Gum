using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// The three traps from #7059 that a hand-written project falls into and <c>gumcli check</c> used to
/// pass over: a component with no BaseType, a derived component missing its base's instances, and a
/// base instance's variable set without the instance prefix.
/// </summary>
public class CheckHandWrittenTrapTests : CheckHandWrittenFileHarness
{
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
        run.ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_ComponentWithoutBaseType_MessageShouldSayHowToFix()
    {
        string project = CreateProject("NoBaseMessage");
        AddComponent(project, "Orphan", baseType: null, DefaultState());

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Message.ShouldContain("Orphan");
        entry.Message.ShouldContain("BaseType");
        entry.Message.ShouldContain("Container");
    }

    [Fact]
    public void Check_ComponentWithEmptyBaseType_ShouldReportGum0015()
    {
        string project = CreateProject("EmptyBase");
        AddComponent(project, "Orphan", baseType: "", DefaultState());

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Code.ShouldBe("GUM0015");
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

    #endregion

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

    #endregion

    #region Base-instance variable without the instance prefix (GUM0017)

    private string CreateDerivedButtonProject(string name, string derivedVariables, string baseBody = "")
    {
        string project = CreateProject(name);
        AddComponent(project, "ButtonBase", "Container",
            DefaultState() + Instance("TextInstance", "Text") + baseBody);
        AddComponent(project, "MyButton", "ButtonBase",
            DefaultState(derivedVariables) + Instance("TextInstance", "Text", definedByBase: true));
        return project;
    }

    [Fact]
    public void Check_DerivedComponentSetsBaseInstanceVariableWithoutPrefix_ShouldReportGum0017AsError()
    {
        string project = CreateDerivedButtonProject("NoPrefix", Variable("string", "Text", "string", "Click me"));

        CheckRun run = Check(project);

        CheckEntry entry = run.Entries.ShouldHaveSingleItem();
        entry.Code.ShouldBe("GUM0017");
        entry.Severity.ShouldBe("Error");
        entry.Element.ShouldBe("MyButton");
        run.ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_DerivedComponentSetsBaseInstanceVariableWithoutPrefix_MessageShouldSuggestThePrefixedName()
    {
        string project = CreateDerivedButtonProject("NoPrefixMessage", Variable("string", "Text", "string", "Click me"));

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Message.ShouldContain("'Text'");
        entry.Message.ShouldContain("TextInstance.Text");
    }

    [Fact]
    public void Check_DerivedComponentSetsBaseInstanceVariableWithPrefix_ShouldNotReport()
    {
        string project = CreateDerivedButtonProject("WithPrefix", Variable("string", "TextInstance.Text", "string", "Click me"));

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_ComponentSetsOwnInstanceVariableWithoutPrefix_ShouldReportGum0017()
    {
        string project = CreateProject("OwnInstanceNoPrefix");
        AddComponent(project, "Label", "Container",
            DefaultState(Variable("string", "Text", "string", "Hi")) + Instance("TextInstance", "Text"));

        CheckEntry entry = Check(project).Entries.ShouldHaveSingleItem();

        entry.Code.ShouldBe("GUM0017");
        entry.Message.ShouldContain("TextInstance.Text");
    }

    [Fact]
    public void Check_RootStandardVariableOnDerivedComponent_ShouldNotReportGum0017()
    {
        // Width exists on the root Container and on the Text instance; the root one wins.
        string project = CreateDerivedButtonProject("RootVariable", Variable("float", "Width", "float", "100"));

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_ExposedVariableSetOnDerivedComponent_ShouldNotReportGum0017()
    {
        string project = CreateProject("Exposed");
        AddComponent(project, "ButtonBase", "Container",
            DefaultState(Variable("string", "TextInstance.Text", "string", "", exposedAs: "Text")) + Instance("TextInstance", "Text"));
        AddComponent(project, "MyButton", "ButtonBase",
            DefaultState(Variable("string", "Text", "string", "Click me")) + Instance("TextInstance", "Text", definedByBase: true));

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_CustomVariableDeclaredOnComponent_ShouldNotReportGum0017()
    {
        string project = CreateProject("Custom");
        AddComponent(project, "Label", "Container",
            DefaultState(Variable("string", "Text", "string", "Hi", isCustom: true)) + Instance("TextInstance", "Text"));

        Check(project).Entries.ShouldBeEmpty();
    }

    #endregion
}
