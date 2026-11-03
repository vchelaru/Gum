using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli check</c> on JSON files that cannot be read at all (malformed text, wrong value kind): the element
/// is named, the exit code is 1, and nothing crashes. Wording of the message is covered separately (#7059).
/// </summary>
public class CheckJsonLoadFailureTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonComponentWithTrailingComma_ShouldReportErrorForThatComponentAndExit1()
    {
        string project = CreateJsonProject("TrailingComma");
        WriteFile(project, "Components", "Holder.gucj", "{\"Name\":\"Holder\",\"BaseType\":\"Container\",}");
        AddJsonReference(project, "ComponentReferences", "Holder");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Severity == "Error");
    }

    [Fact]
    public void Check_JsonScreenThatIsNotAnObject_ShouldReportErrorForThatScreenAndExit1()
    {
        string project = CreateJsonProject("ScreenArray");
        WriteFile(project, "Screens", "Menu.gusj", "[1,2");
        AddJsonReference(project, "ScreenReferences", "Menu");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Menu" && e.Severity == "Error");
    }

    [Fact]
    public void Check_EmptyJsonComponentFile_ShouldReportErrorForThatComponentAndExit1()
    {
        string project = CreateJsonProject("EmptyFile");
        WriteFile(project, "Components", "Holder.gucj", "");
        AddJsonReference(project, "ComponentReferences", "Holder");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Severity == "Error");
    }

    [Fact]
    public void Check_MalformedJsonProjectFile_ShouldExit2AndExplainOnStandardError()
    {
        string project = CreateJsonProject("MalformedProject");
        File.WriteAllText(project, "{ \"Version\": ");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(2);
        run.StandardError.ShouldContain("Failed to load project");
    }

    [Fact]
    public void Check_JsonProjectWithTextWhereAnElementTypeNumberBelongs_ShouldExit2AndNameTheProperty()
    {
        string project = CreateJsonProject("ElementTypeText");
        WriteFile(project, "Components", "Holder.gucj", JElement("Holder", "Container", JDefault()));
        AddJsonReference(project, "ComponentReferences", "Holder");
        string content = File.ReadAllText(project);
        File.WriteAllText(project, content.Replace("\"ElementType\": 0", "\"ElementType\": \"Component\""));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(2);
        run.StandardError.ShouldContain("ElementType");
    }
}
