using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli check</c> on hand-written JSON element files (<c>.gucj</c>, <c>.gusj</c>) in a <c>.gumj</c> project.
/// The XML path has its own tests; these pin that the same mistakes are reported through the JSON loader (#7059).
/// </summary>
public class CheckJsonElementFileTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_ValidJsonComponent_ShouldReportNothing()
    {
        string project = CreateJsonProject("Valid");
        AddJsonComponent(project, "Holder", "Container",
            JDefault(JVar("float", "Width", "\"ValueAsFloat\":4")),
            JInstances(JInstance("Label", "Text")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(0);
        run.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_JsonComponentWithUnknownBaseType_ShouldReportErrorNamingTheBase()
    {
        string project = CreateJsonProject("UnknownBase");
        AddJsonComponent(project, "Holder", "Nope", JDefault());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Severity == "Error" && e.Message.Contains("Nope"));
    }

    [Fact]
    public void Check_JsonScreenWithUnknownBaseType_ShouldReportErrorNamingTheBase()
    {
        string project = CreateJsonProject("ScreenUnknownBase");
        AddJsonScreen(project, "Menu", "Nope", JDefault());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Menu" && e.Message.Contains("Nope"));
    }

    [Fact]
    public void Check_JsonInstanceWithUnknownBaseType_ShouldReportErrorNamingInstanceAndType()
    {
        string project = CreateJsonProject("InstanceUnknownBase");
        AddJsonComponent(project, "Holder", "Container", JDefault(), JInstances(JInstance("Child", "Nope")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("Child") && e.Message.Contains("Nope"));
    }

    [Fact]
    public void Check_JsonScreenUsedAsInstanceType_ShouldReportGum0025()
    {
        string project = CreateJsonProject("ScreenAsInstance");
        AddJsonScreen(project, "Menu", null, JDefault());
        AddJsonComponent(project, "Holder", "Container", JDefault(), JInstances(JInstance("Embedded", "Menu")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0025" && e.Element == "Holder");
    }

    [Fact]
    public void Check_JsonComponentInstantiatingItself_ShouldReportGum0018()
    {
        string project = CreateJsonProject("SelfInstance");
        AddJsonComponent(project, "Matryoshka", "Container", JDefault(), JInstances(JInstance("Inner", "Matryoshka")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0018" && e.Element == "Matryoshka");
    }

    [Fact]
    public void Check_JsonComponentsWithCircularBaseTypes_ShouldReportGum0014ForBoth()
    {
        string project = CreateJsonProject("CircularBase");
        AddJsonComponent(project, "A", "B", JDefault());
        AddJsonComponent(project, "B", "A", JDefault());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.Where(e => e.Code == "GUM0014").Select(e => e.Element).OrderBy(n => n).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public void Check_JsonComponentWithDuplicateStates_ShouldReportGum0021()
    {
        string project = CreateJsonProject("DuplicateStates");
        AddJsonComponent(project, "Holder", "Container", JStates(JState("Default"), JState("Default")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0021" && e.Element == "Holder");
    }

    [Fact]
    public void Check_JsonComponentWithDuplicateStateInACategory_ShouldReportGum0021NamingTheCategory()
    {
        string project = CreateJsonProject("DuplicateCategoryStates");
        AddJsonComponent(project, "Holder", "Container", JDefault(),
            "\"Categories\":[{\"Name\":\"Mode\",\"States\":[" + JState("On") + "," + JState("On") + "]}]");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0021" && e.Message.Contains("Mode"));
    }

    [Fact]
    public void Check_JsonComponentsWhoseInstancesParentEachOther_ShouldReportGum0024()
    {
        string project = CreateJsonProject("ParentCycle");
        AddJsonComponent(project, "Holder", "Container",
            JDefault(
                JVar("string", "A.Parent", "\"ValueAsString\":\"B\""),
                JVar("string", "B.Parent", "\"ValueAsString\":\"A\"")),
            JInstances(JInstance("A", "Container"), JInstance("B", "Container")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0024" && e.Element == "Holder");
    }

    [Fact]
    public void Check_JsonInstanceSetsAStateItsCategoryDoesNotHave_ShouldReportGum0026()
    {
        string project = CreateJsonProject("UnknownState");
        AddJsonComponent(project, "Toggle", "Container", JDefault(),
            "\"Categories\":[{\"Name\":\"Mode\",\"States\":[" + JState("On") + "]}]");
        AddJsonComponent(project, "Holder", "Container",
            JDefault(JVar("Mode", "Switch.ModeState", "\"ValueAsString\":\"Nope\"")),
            JInstances(JInstance("Switch", "Toggle")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0026" && e.Element == "Holder");
    }

    [Fact]
    public void Check_JsonVariableOnAMissingInstance_ShouldReportGum0027AsWarning()
    {
        string project = CreateJsonProject("MissingInstanceVariable");
        AddJsonComponent(project, "Holder", "Container", JDefault(JVar("float", "Ghost.X", "\"ValueAsFloat\":4")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(0);
        run.Entries.ShouldContain(e => e.Code == "GUM0027" && e.Severity == "Warning" && e.Message.Contains("Ghost"));
    }

    [Fact]
    public void Check_JsonDerivedComponentMissingItsBaseInstance_ShouldReportGum0016AsWarning()
    {
        string project = CreateJsonProject("MissingBaseInstance");
        AddJsonComponent(project, "Base", "Container", JDefault(), JInstances(JInstance("Label", "Text")));
        AddJsonComponent(project, "Derived", "Base", JDefault());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(0);
        run.Entries.ShouldContain(e => e.Code == "GUM0016" && e.Element == "Derived" && e.Message.Contains("Label"));
    }

    [Fact]
    public void Check_JsonComponentUsingAnUnknownBehavior_ShouldReportErrorNamingTheBehavior()
    {
        string project = CreateJsonProject("UnknownBehavior");
        AddJsonComponent(project, "Holder", "Container", JDefault(), "\"Behaviors\":[{\"BehaviorName\":\"Nope\"}]");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("Nope"));
    }

    [Fact]
    public void Check_JsonComponentMissingAnInstanceItsBehaviorRequires_ShouldReportGum0001()
    {
        string project = CreateJsonProject("BehaviorRequiredInstance");
        WriteFile(project, "Behaviors", "Needy.behj",
            "{\"Name\":\"Needy\",\"RequiredInstances\":[{\"Name\":\"Caption\",\"BaseType\":\"Text\",\"Behaviors\":[]}]}");
        AddJsonReference(project, "BehaviorReferences", "Needy");
        AddJsonComponent(project, "Holder", "Container", JDefault(), "\"Behaviors\":[{\"BehaviorName\":\"Needy\"}]");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0001" && e.Element == "Holder" && e.Message.Contains("Caption"));
    }

    [Fact]
    public void Check_JsonComponentFileThatIsMissing_ShouldReportGum0004NamingTheFile()
    {
        string project = CreateJsonProject("MissingComponentFile");
        AddJsonReference(project, "ComponentReferences", "Gone");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0004" && e.Element == "Gone" && e.Message.Contains("Gone.gucj"));
    }
}
