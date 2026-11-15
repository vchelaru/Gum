using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli check</c> given an element file instead of the project file: exit 2, and the message names the file given (#7059).
/// </summary>
public class CheckElementFilePassedAsProjectTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonElementFileGivenAsProject_ShouldExit2NamingTheFile()
    {
        string project = CreateJsonProject("JsonElementAsProject");
        AddJsonComponent(project, "Holder", "Container", JDefault());
        string elementPath = Path.Combine(Path.GetDirectoryName(project)!, "Components", "Holder.gucj");

        CheckRun run = Check(elementPath);

        run.ExitCode.ShouldBe(2);
        run.StandardError.ShouldContain("Holder.gucj");
    }

    [Fact]
    public void Check_XmlElementFileGivenAsProject_ShouldExit2NamingTheFile()
    {
        string project = CreateProject("XmlElementAsProject");
        AddComponent(project, "Holder", "Container", DefaultState());
        string elementPath = Path.Combine(Path.GetDirectoryName(project)!, "Components", "Holder.gucx");

        CheckRun run = Check(elementPath);

        run.ExitCode.ShouldBe(2);
        run.StandardError.ShouldContain("Holder.gucx");
    }
}
