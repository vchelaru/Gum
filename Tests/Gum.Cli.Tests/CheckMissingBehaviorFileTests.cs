using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// A project that lists a behavior whose file is not on disk. A missing element file is GUM0004; a missing behavior
/// file loads as an empty behavior with no message, so the behavior's requirements are never checked (#7059).
/// </summary>
public class CheckMissingBehaviorFileTests : CheckHandWrittenJsonFileHarness
{
    [Fact]
    public void Check_JsonProjectListingABehaviorWithNoFile_ShouldReportTheBehavior()
    {
        string project = CreateJsonProject("MissingBehaviorJson");
        AddJsonReference(project, "BehaviorReferences", "Gone");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Message.Contains("Gone"));
    }

    [Fact]
    public void Check_XmlProjectListingABehaviorWithNoFile_ShouldReportTheBehavior()
    {
        string project = CreateProject("MissingBehaviorXml");
        AddReference(project, """  <BehaviorReference Name="Gone" />""");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Message.Contains("Gone"));
    }
}
