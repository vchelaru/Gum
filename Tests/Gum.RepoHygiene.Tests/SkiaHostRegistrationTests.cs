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
    public void CommentedOutWorkflowStep_IsReportedAsMissing()
    {
        BuildFakeRepo("FooGum", commentedWorkflowStepOnly: true);

        Single().ShouldContain("no test step");
    }

    [Fact]
    public void MissingWorkflowStep_ForSilkNetGum_DoesNotTellYouToCopyItself()
    {
        BuildFakeRepo("SilkNetGum", inWorkflow: false);

        string message = Single();

        message.ShouldNotContain("copy the SilkNetGum.Tests step");
        message.ShouldContain("copy the StrideGum.Tests step");
    }

    [Fact]
    public void MissingDocsPage_NamesFileAndFolder()
    {
        BuildFakeRepo("FooGum", docsPage: false);

        string message = Single();

        message.ShouldContain("docs/code/getting-started/setup/adding-initializing-gum/foo.md");
        message.ShouldContain("Add");
    }

    [Fact]
    public void MissingDocsCard_NamesReadme()
    {
        BuildFakeRepo("FooGum", docsCard: false);

        string message = Single();

        message.ShouldContain("adding-initializing-gum/README.md");
        message.ShouldContain("foo.md");
    }

    [Fact]
    public void MissingSummaryEntry_NamesSummary()
    {
        BuildFakeRepo("FooGum", summaryEntry: false);

        string message = Single();

        message.ShouldContain("docs/SUMMARY.md");
        message.ShouldContain("adding-initializing-gum/foo.md");
    }

    [Fact]
    public void MissingRootReadmeRow_NamesReadme()
    {
        BuildFakeRepo("FooGum", readmeRow: false);

        string message = Single();

        message.ShouldContain("README.md");
        message.ShouldContain("| Foo |");
    }

    [Fact]
    public void MissingSample_NamesDirectory()
    {
        BuildFakeRepo("FooGum", sample: false);

        Single().ShouldContain("Samples/FooGum");
    }

    [Fact]
    public void MissingWindowsTestGroup_NamesGroupsFile()
    {
        BuildFakeRepo("FooGum", testGroup: false);

        string message = Single();

        message.ShouldContain(".github/scripts/windows-test-groups.txt");
        message.ShouldContain("Tests/FooGum.Tests");
    }

    [Fact]
    public void KniSkiaGum_NeedsNoDocsPageCardSummaryOrReadmeRow()
    {
        // KniSkiaGum is documented under the KNI pages, so it is exempt from the four docs expectations.
        BuildFakeRepo("KniSkiaGum", docsPage: false, docsCard: false, summaryEntry: false, readmeRow: false);

        SkiaHostRegistrationChecker.Check(_root).ShouldBeEmpty();
    }

    [Fact]
    public void SilkNetGum_UsesTheDottedDocsPageAndDisplayName()
    {
        BuildFakeRepo("SilkNetGum", docsPage: false, docsCard: false, summaryEntry: false, readmeRow: false);

        IReadOnlyList<string> problems = SkiaHostRegistrationChecker.Check(_root);

        problems.Count.ShouldBe(4);
        problems[0].ShouldContain("silk.net.md");
        problems[3].ShouldContain("| Silk.NET |");
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
        bool testsCommonReference = true,
        bool docsPage = true,
        bool docsCard = true,
        bool summaryEntry = true,
        bool readmeRow = true,
        bool sample = true,
        bool testGroup = true,
        bool commentedWorkflowStepOnly = false)
    {
        // A fake host "FooGum" gets the conventional docs page "foo.md" and display name "Foo".
        string stem = host.EndsWith("Gum") ? host.Substring(0, host.Length - 3) : host;
        string page = stem.ToLowerInvariant() + ".md";
        if (host == "SilkNetGum")
        {
            stem = "Silk.NET";
            page = "silk.net.md";
        }
        string folder = "docs/code/getting-started/setup/adding-initializing-gum";
        if (docsPage)
        {
            Write($"{folder}/{page}", "# page");
        }
        Write($"{folder}/README.md", docsCard ? $"<tr><td>{stem}</td><td><a href=\"{page}\">{page}</a></td></tr>" : "cards");
        Write("docs/SUMMARY.md", summaryEntry ? $"      * [{stem}](code/getting-started/setup/adding-initializing-gum/{page})\n" : "summary\n");
        Write("README.md", readmeRow ? $"| {stem} | pkg | Built-in |\n" : "table\n");
        if (sample)
        {
            Write($"Samples/{host}/readme.txt", "sample");
        }
        Write(".github/scripts/windows-test-groups.txt", testGroup ? $"headless Tests/{host}.Tests\n" : "# groups\n");
        WriteHostCsproj(host, "SkiaHost.props");
        Write("AllLibraries.sln", inAllLibraries
            ? $"Project(\"{{FAE04EC0}}\") = \"{host}\", \"Runtimes\\{host}\\{host}.csproj\", \"{{1}}\"\r\n"
              + $"Project(\"{{FAE04EC0}}\") = \"{host}.Tests\", \"Tests\\{host}.Tests\\{host}.Tests.csproj\", \"{{2}}\"\r\n"
            : "");
        Write("Tests/CiTestProjects.slnf", inSlnf
            ? $"\"Tests\\\\{host}.Tests\\\\{host}.Tests.csproj\","
            : "{}");
        string step = $"dotnet test \"Tests/{host}.Tests\" --configuration Release --no-build\n";
        Write(".github/workflows/build-and-test.yaml", commentedWorkflowStepOnly
            ? $"        # run: {step}"
            : inWorkflow ? $"        run: {step}" : "steps:\n");
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
