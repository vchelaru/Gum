using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// A Skia host (a Runtimes/ csproj importing SkiaHost.props or SkiaInputHost.props) is registered by
/// hand in four places. A miss otherwise surfaces late in CI, so this fails fast with instructions.
/// </summary>
public class SkiaHostRegistrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gum-skiahost-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void RealRepo_EveryHostIsFullyRegistered()
    {
        IReadOnlyList<string> problems = SkiaHostRegistrationChecker.Check(RepoPaths.RepoRoot);

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void RealRepo_FindsTheExistingHosts()
    {
        IReadOnlyList<string> hosts = SkiaHostRegistrationChecker.DiscoverHosts(RepoPaths.RepoRoot);

        hosts.ShouldBe(new[] { "KniSkiaGum", "SilkNetGum", "StrideGum", "UnityGum" }, ignoreOrder: true);
    }

    [Fact]
    public void FullyRegisteredHost_HasNoProblems()
    {
        BuildFakeRepo("FooGum");

        SkiaHostRegistrationChecker.Check(_root).ShouldBeEmpty();
    }

    [Fact]
    public void SkiaGumItself_IsNotTreatedAsAHost()
    {
        BuildFakeRepo("FooGum");
        WriteHostCsproj("SkiaGum", "SkiaHost.props");

        SkiaHostRegistrationChecker.DiscoverHosts(_root).ShouldBe(new[] { "FooGum" });
    }

    [Fact]
    public void ProjectImportingSkiaInputHostProps_IsDiscovered()
    {
        BuildFakeRepo("FooGum");
        WriteHostCsproj("BarGum", "SkiaInputHost.props");

        SkiaHostRegistrationChecker.DiscoverHosts(_root).ShouldBe(new[] { "BarGum", "FooGum" }, ignoreOrder: true);
    }

    [Fact]
    public void MissingSolutionEntry_NamesHostAndFile()
    {
        BuildFakeRepo("FooGum", inAllLibraries: false);

        string message = Single();

        message.ShouldContain("FooGum");
        message.ShouldContain("AllLibraries.sln");
        message.ShouldContain("1.");
    }

    [Fact]
    public void MissingSlnfEntry_NamesHostAndFile()
    {
        BuildFakeRepo("FooGum", inSlnf: false);

        string message = Single();

        message.ShouldContain("Tests/CiTestProjects.slnf");
        message.ShouldContain("Tests\\\\FooGum.Tests\\\\FooGum.Tests.csproj");
    }

    [Fact]
    public void MissingWorkflowStep_NamesHostAndFile()
    {
        BuildFakeRepo("FooGum", inWorkflow: false);

        string message = Single();

        message.ShouldContain(".github/workflows/build-and-test.yaml");
        message.ShouldContain("dotnet test \"Tests/FooGum.Tests\"");
    }

    [Fact]
    public void MissingTestProject_NamesHostAndBase()
    {
        BuildFakeRepo("FooGum", testProject: false);

        string message = Single();

        message.ShouldContain("Tests/FooGum.Tests");
        message.ShouldContain("SkiaHost.TestsCommon");
    }

    [Fact]
    public void TestProjectNotUsingTestsCommon_IsReported()
    {
        BuildFakeRepo("FooGum", testsCommonReference: false);

        Single().ShouldContain("SkiaHost.TestsCommon");
    }

    [Fact]
    public void SeveralMisses_AreNumberedInOneMessage()
    {
        BuildFakeRepo("FooGum", inAllLibraries: false, inSlnf: false, inWorkflow: false);

        IReadOnlyList<string> problems = SkiaHostRegistrationChecker.Check(_root);

        problems.Count.ShouldBe(3);
        problems[0].ShouldStartWith("1.");
        problems[2].ShouldStartWith("3.");
    }

    private string Single()
    {
        IReadOnlyList<string> problems = SkiaHostRegistrationChecker.Check(_root);
        problems.Count.ShouldBe(1, string.Join(Environment.NewLine, problems));
        return problems[0];
    }

    private void WriteHostCsproj(string host, string props)
    {
        Write($"Runtimes/{host}/{host}.csproj",
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><Import Project=\"..\\{props}\" /></Project>");
    }

    private void BuildFakeRepo(
        string host,
        bool inAllLibraries = true,
        bool inSlnf = true,
        bool inWorkflow = true,
        bool testProject = true,
        bool testsCommonReference = true)
    {
        WriteHostCsproj(host, "SkiaHost.props");
        Write("AllLibraries.sln", inAllLibraries
            ? $"Project(\"{{FAE04EC0}}\") = \"{host}\", \"Runtimes\\{host}\\{host}.csproj\", \"{{1}}\"\r\n"
              + $"Project(\"{{FAE04EC0}}\") = \"{host}.Tests\", \"Tests\\{host}.Tests\\{host}.Tests.csproj\", \"{{2}}\"\r\n"
            : "");
        Write("Tests/CiTestProjects.slnf", inSlnf
            ? $"\"Tests\\\\{host}.Tests\\\\{host}.Tests.csproj\","
            : "{}");
        Write(".github/workflows/build-and-test.yaml", inWorkflow
            ? $"        run: dotnet test \"Tests/{host}.Tests\" --configuration Release --no-build\n"
            : "steps:\n");
        if (testProject)
        {
            Write($"Tests/{host}.Tests/{host}.Tests.csproj", testsCommonReference
                ? "<Project><ProjectReference Include=\"..\\SkiaHost.TestsCommon\\SkiaHost.TestsCommon.csproj\" /></Project>"
                : "<Project />");
        }
    }

    private void Write(string relative, string content)
    {
        string full = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }
}
