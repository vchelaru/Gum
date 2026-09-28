using GumTestSupport;
using Shouldly;

namespace Gum.Cli.Tests;

public class ThirdPartyNoticesCoverageTests
{
    [Fact]
    public void EveryPackageTheCliShips_IsListedInThirdPartyNotices()
    {
        string depsJson = Path.Combine(AppContext.BaseDirectory, "gumcli.deps.json");
        string notices = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt"));

        string.Join(", ", ThirdPartyNoticesCoverage.FindUnlistedPackages(depsJson, notices)).ShouldBeEmpty(
            "add each package to THIRD-PARTY-NOTICES.txt (name, license, copyright, and a NuGet: line)");
    }

    [Fact]
    public void EveryBinaryTheCliEmbeds_IsListedInThirdPartyNotices()
    {
        string depsJson = Path.Combine(AppContext.BaseDirectory, "gumcli.deps.json");
        string notices = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt"));

        string.Join(", ", ThirdPartyNoticesCoverage.FindUnlistedEmbeddedBinaries(depsJson, notices)).ShouldBeEmpty(
            "add each embedded binary to THIRD-PARTY-NOTICES.txt (name, license, copyright, and an Embedded: line)");
    }
}
