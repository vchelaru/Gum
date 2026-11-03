using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli check</c> on the typed value properties of a hand-written JSON variable (<c>ValueAsFloat</c>,
/// <c>ValueAsString</c>, ...): a value of the wrong kind for the variable's type is GUM0019 (#7059).
/// </summary>
public class CheckJsonValueTypeTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonFloatVariableWithTextValue_ShouldReportGum0019()
    {
        string project = CreateJsonProject("FloatText");
        AddJsonComponent(project, "Holder", "Container", JDefault(JVar("float", "Width", "\"ValueAsString\":\"wide\"")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0019" && e.Element == "Holder" && e.Message.Contains("Width"));
    }

    [Fact]
    public void Check_JsonBoolVariableWithIntegerValue_ShouldReportGum0019()
    {
        string project = CreateJsonProject("BoolInt");
        AddJsonComponent(project, "Holder", "Container", JDefault(JVar("bool", "Visible", "\"ValueAsInt\":1")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0019" && e.Message.Contains("Visible"));
    }

    [Fact]
    public void Check_JsonFloatVariableWithBoolValue_ShouldReportGum0019()
    {
        string project = CreateJsonProject("FloatBool");
        AddJsonComponent(project, "Holder", "Container", JDefault(JVar("float", "Width", "\"ValueAsBool\":true")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0019" && e.Message.Contains("Width"));
    }

    [Fact]
    public void Check_JsonBoolVariableWithTrueText_ShouldNotReportGum0019()
    {
        string project = CreateJsonProject("BoolText");
        AddJsonComponent(project, "Holder", "Container", JDefault(JVar("bool", "Visible", "\"ValueAsString\":\"true\"")));

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0019");
    }

    [Fact]
    public void Check_JsonFloatVariableWithIntegerValue_ShouldNotReportGum0019()
    {
        string project = CreateJsonProject("FloatInt");
        AddJsonComponent(project, "Holder", "Container", JDefault(JVar("float", "Width", "\"ValueAsInt\":4")));

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0019");
    }

    [Fact]
    public void Check_JsonSameVariableTwiceInOneState_ShouldReportGum0022()
    {
        string project = CreateJsonProject("SameVariableTwice");
        AddJsonComponent(project, "Holder", "Container",
            JDefault(JVar("float", "Width", "\"ValueAsFloat\":4"), JVar("float", "Width", "\"ValueAsFloat\":5")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Code == "GUM0022" && e.Element == "Holder");
    }
}
