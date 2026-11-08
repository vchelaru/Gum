using System.Text.RegularExpressions;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Finds every Skia host by convention (a csproj under Runtimes/ importing SkiaHost.props or
/// SkiaInputHost.props, except SkiaGum itself) and reports the hand-made registrations it lacks.
/// </summary>
internal static class SkiaHostRegistrationChecker
{
    private static readonly Regex HostImport = new(@"Import\s+Project\s*=\s*""[^""]*Skia(Input)?Host\.props""", RegexOptions.IgnoreCase);

    private const string DocsFolder = "docs/code/getting-started/setup/adding-initializing-gum";

    // KniSkiaGum is deliberately kept out of the docs for now (no page, card, SUMMARY entry or README row), so it
    // is exempt; remove it from this set when its docs are written. Every other host needs all four.
    private static readonly HashSet<string> DocsExempt = new() { "KniSkiaGum" };

    public static IReadOnlyList<string> DiscoverHosts(string repoRoot)
    {
        string runtimes = Path.Combine(repoRoot, "Runtimes");
        List<string> hosts = new();
        if (!Directory.Exists(runtimes))
        {
            return hosts;
        }
        foreach (string csproj in Directory.EnumerateFiles(runtimes, "*.csproj", SearchOption.AllDirectories))
        {
            string name = Path.GetFileNameWithoutExtension(csproj);
            if (name == "SkiaGum" || !HostImport.IsMatch(File.ReadAllText(csproj)))
            {
                continue;
            }
            hosts.Add(name);
        }
        return hosts;
    }

    /// <summary>Returns one numbered, actionable line per missing registration; empty when all is registered.</summary>
    public static IReadOnlyList<string> Check(string repoRoot)
    {
        List<string> missing = new();
        string allLibraries = ReadOrEmpty(repoRoot, "AllLibraries.sln");
        string slnf = ReadOrEmpty(repoRoot, "Tests/CiTestProjects.slnf");
        string workflow = StripComments(ReadOrEmpty(repoRoot, ".github/workflows/build-and-test.yaml"));
        string readme = ReadOrEmpty(repoRoot, "README.md");
        string summary = ReadOrEmpty(repoRoot, "docs/SUMMARY.md");
        string docsReadme = ReadOrEmpty(repoRoot, DocsFolder + "/README.md");
        string windowsGroupsPath = Path.Combine(repoRoot, ".github/scripts/windows-test-groups.txt");
        string? windowsGroups = File.Exists(windowsGroupsPath) ? StripComments(File.ReadAllText(windowsGroupsPath)) : null;

        foreach (string host in DiscoverHosts(repoRoot))
        {
            string tests = host + ".Tests";
            string hostEntry = $"Runtimes\\{host}\\{host}.csproj";
            string testEntry = $"Tests\\{tests}\\{tests}.csproj";

            if (!allLibraries.Contains(hostEntry) || !allLibraries.Contains(testEntry))
            {
                missing.Add($"Skia host {host} is not fully in AllLibraries.sln. Add both {hostEntry} and {testEntry} to AllLibraries.sln.");
            }

            string slnfEntry = testEntry.Replace("\\", "\\\\");
            if (!slnf.Contains(slnfEntry))
            {
                missing.Add($"Skia host {host} is missing from Tests/CiTestProjects.slnf (the Windows --no-build test step fails without it). Add \"{slnfEntry}\" to its projects list.");
            }

            if (!workflow.Contains($"dotnet test \"Tests/{tests}\""))
            {
                string example = host == "SilkNetGum" ? "StrideGum" : "SilkNetGum";
                missing.Add($"Skia host {host} has no test step in .github/workflows/build-and-test.yaml. Add a step running: dotnet test \"Tests/{tests}\" --configuration Release --no-build --logger \"trx\" --results-directory \"TestResults\" (copy the {example}.Tests step).");
            }

            if (windowsGroups is not null && !Regex.IsMatch(windowsGroups, $@"^\s*\S+\s+Tests/{Regex.Escape(tests)}(\s|$)", RegexOptions.Multiline))
            {
                missing.Add($"Skia host {host} is missing from .github/scripts/windows-test-groups.txt (the Windows CI script fails fast without it). Add a line \"headless Tests/{tests}\" to it.");
            }

            if (!Directory.Exists(Path.Combine(repoRoot, "Samples", host)))
            {
                missing.Add($"Skia host {host} has no sample. Add Samples/{host} (copy Samples/SilkNetGum).");
            }

            if (!DocsExempt.Contains(host))
            {
                CheckDocs(repoRoot, host, readme, summary, docsReadme, missing);
            }

            string testCsproj = Path.Combine(repoRoot, "Tests", tests, tests + ".csproj");
            if (!File.Exists(testCsproj))
            {
                missing.Add($"Skia host {host} has no test project at Tests/{tests}. Create it with a ProjectReference to Tests/SkiaHost.TestsCommon and derive from SkiaHostTestBase (see Tests/SilkNetGum.Tests).");
            }
            else if (!File.ReadAllText(testCsproj).Contains("SkiaHost.TestsCommon"))
            {
                missing.Add($"Tests/{tests} does not reference Tests/SkiaHost.TestsCommon. Add a ProjectReference to it and derive from SkiaHostTestBase / use SkiaHostTestFramework.");
            }
        }

        return missing.Select((message, index) => $"{index + 1}. {message}").ToList();
    }

    private static void CheckDocs(string repoRoot, string host, string readme, string summary, string docsReadme, List<string> missing)
    {
        (string page, string display) = DocsNames(host);
        string docsPage = $"{DocsFolder}/{page}";

        if (!File.Exists(Path.Combine(repoRoot, docsPage)))
        {
            missing.Add($"Skia host {host} has no docs page. Add {docsPage} (copy another host's page in that folder).");
        }

        if (!docsReadme.Contains($"href=\"{page}\""))
        {
            missing.Add($"Skia host {host} has no card in {DocsFolder}/README.md. Add a table cell for {display} linking to {page}.");
        }

        if (!summary.Contains($"adding-initializing-gum/{page})"))
        {
            missing.Add($"Skia host {host} is missing from docs/SUMMARY.md. Add \"* [{display}](code/getting-started/setup/adding-initializing-gum/{page})\" under Adding and Initializing Gum.");
        }

        if (!Regex.IsMatch(readme, $@"^\|\s*{Regex.Escape(display)}\b", RegexOptions.Multiline))
        {
            missing.Add($"Skia host {host} has no row in the root README.md platform table. Add a row starting with \"| {display} |\".");
        }
    }

    // Convention: docs page is the host name minus "Gum", lowercased; display name is that stem. Hosts that differ are listed here.
    private static (string Page, string Display) DocsNames(string host)
    {
        if (host == "SilkNetGum")
        {
            // The product is "Silk.NET" (with a dot), so its page is silk.net.md, not silknet.md.
            return ("silk.net.md", "Silk.NET");
        }
        if (host == "BrutalGum")
        {
            // BRUTAL is an all-caps product name, not a word.
            return ("brutal.md", "BRUTAL");
        }
        string stem = host.EndsWith("Gum") ? host.Substring(0, host.Length - 3) : host;
        return (stem.ToLowerInvariant() + ".md", stem);
    }

    private static string StripComments(string yaml)
    {
        return string.Join("\n", yaml.Split('\n').Where(line => !line.TrimStart().StartsWith("#")));
    }

    private static string ReadOrEmpty(string repoRoot, string relative)
    {
        string path = Path.Combine(repoRoot, relative);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
