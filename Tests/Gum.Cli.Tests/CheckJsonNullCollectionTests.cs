using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// A JSON collection written as <c>null</c> fails the load with a .NET argument message; the error names the property (#7059).
/// </summary>
public class CheckJsonNullCollectionTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonComponentWithNullStates_ShouldNameStatesInTheError()
    {
        string project = CreateJsonProject("NullStates");
        AddJsonComponent(project, "Holder", "Container", "\"States\":null");

        CheckEntry entry = Check(project).Entries.First(e => e.Element == "Holder");

        entry.Message.ShouldContain("States");
    }

    [Fact]
    public void Check_JsonComponentWithNullInstances_ShouldNameInstancesInTheError()
    {
        string project = CreateJsonProject("NullInstances");
        AddJsonComponent(project, "Holder", "Container", JDefault(), "\"Instances\":null");

        CheckEntry entry = Check(project).Entries.First(e => e.Element == "Holder");

        entry.Message.ShouldContain("Instances");
    }

    [Fact]
    public void Check_JsonProjectWithNullComponentReferences_ShouldNameThePropertyOnStandardError()
    {
        string project = CreateJsonProject("NullReferences");
        File.WriteAllText(project, "{\"ComponentReferences\":null}");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(2);
        run.StandardError.ShouldContain("ComponentReferences");
    }
}
