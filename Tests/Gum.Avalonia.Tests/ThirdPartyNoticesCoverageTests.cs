using System;
using System.IO;
using GumTestSupport;
using Shouldly;
using Xunit;

namespace Gum.Avalonia.Tests;

public class ThirdPartyNoticesCoverageTests
{
    [Fact]
    public void EveryPackageTheHeadShips_IsListedInThirdPartyNotices()
    {
        string depsJson = Path.Combine(AppContext.BaseDirectory, "Gum.deps.json");
        string notices = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt"));

        string.Join(", ", ThirdPartyNoticesCoverage.FindUnlistedPackages(depsJson, notices)).ShouldBeEmpty(
            "add each package to THIRD-PARTY-NOTICES.txt (name, license, copyright, and a NuGet: line)");
    }
}
