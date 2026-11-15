using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7580: a variable the instance's type does not have is harmless leftover data, so <c>gumcli check</c> stays silent.
/// </summary>
public class CheckUnknownInstanceVariableTests : CheckHandWrittenFileHarness
{
    [Fact]
    public void Check_VariableTheInstanceTypeLacks_ShouldReportNothing()
    {
        string project = CreateProject("UnknownInstanceVariable");
        AddComponent(project, "Holder", "Container",
            DefaultState(Variable("float", "Label.NotAVariable", "float", "1")) + Instance("Label", "Text"));

        CheckRun run = Check(project);

        run.Entries.ShouldBeEmpty();
        run.ExitCode.ShouldBe(0);
    }
}
