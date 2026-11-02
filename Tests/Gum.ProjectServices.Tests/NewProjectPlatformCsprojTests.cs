#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>The csproj written for each platform.</summary>
public class NewProjectPlatformCsprojTests : NewProjectServiceTestBase
{
    [Theory]
    [MemberData(nameof(PlatformNames), MemberType = typeof(NewProjectServiceTestBase))]
    public async Task Execute_EveryPlatform_ProducesAWellFormedCsprojWithOneGumReference(string platform)
    {
        NewProjectResult result = await Run(Request(platform, sourceLinked: platform == "kniskia"));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        XDocument document = XDocument.Load(result.Files.CsprojPath!);
        string[] gumReferences = document.Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? "")
            .Where(include => include.StartsWith("Gum.", StringComparison.OrdinalIgnoreCase) || include.Contains("Gum", StringComparison.Ordinal))
            .ToArray();
        gumReferences.Length.ShouldBe(1, string.Join(" | ", gumReferences));
        document.Descendants("TargetFramework").Single().Value.ShouldStartWith("net");
        result.Files.CsprojPath!.ShouldStartWith(Game());
    }
}
