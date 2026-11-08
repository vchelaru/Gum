using System.Text.RegularExpressions;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Finds every Skia host by convention (a csproj under Runtimes/ importing SkiaHost.props or
/// SkiaInputHost.props, except SkiaGum itself) and reports the hand-made registrations it lacks.
/// </summary>
internal static class SkiaHostRegistrationChecker
{
    private static readonly Regex HostImport = new(@"Import\s+Project\s*=\s*""[^""]*Skia(Input)?Host\.props""", RegexOptions.IgnoreCase);

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
        string workflow = ReadOrEmpty(repoRoot, ".github/workflows/build-and-test.yaml");

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
                missing.Add($"Skia host {host} has no test step in .github/workflows/build-and-test.yaml. Add a step running: dotnet test \"Tests/{tests}\" --configuration Release --no-build --logger \"trx\" --results-directory \"TestResults\" (copy the SilkNetGum.Tests step).");
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

    private static string ReadOrEmpty(string repoRoot, string relative)
    {
        string path = Path.Combine(repoRoot, relative);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
