#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>The generated host initializes exactly the recommended packages the csproj was given, even when later steps fail.</summary>
public class NewProjectRecommendedPackagesHostTests : NewProjectServiceTestBase
{
    [Theory]
    [MemberData(nameof(PlatformNames), MemberType = typeof(NewProjectServiceTestBase))]
    public async Task Execute_WithRecommendedPackages_InitializesExactlyThePackagesTheCsprojWasGiven(string platformName)
    {
        HostPlatform platform = NewProjectPlatforms.MainNames.First(entry => entry.Name == platformName).Platform;
        if (platform == HostPlatform.KniSkia)
        {
            return; // Always source-linked, which refuses packages (#7328), and it has none to add anyway.
        }

        NewProjectResult result = await Run(Request(platformName, packages: true));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string host = File.ReadAllText(Path.Combine(Game(), NewProjectLayout.WritesGame1(platform) ? "Game1.cs" : "Program.cs"));
        IReadOnlyList<string> added = RecommendedPackages.For(platform);
        _calls.Where(call => call.StartsWith("add ")).Select(call => call.Substring(4)).ShouldBe(added);

        host.Contains("GumExpressionService.Initialize()").ShouldBe(added.Contains("Gum.Expressions"));
        host.Contains("KernSmith.Gum.").ShouldBe(added.Any(package => package.StartsWith("KernSmith.")));
    }

    [Fact]
    public async Task Execute_WhenRestoreFailsAfterThePackagesWereAdded_TheHostStillInitializesThem()
    {
        NewProjectService service = CreateService(restore: new FailingRestore());

        NewProjectResult result = await service.ExecuteAsync(
            Request("monogame", packages: true, restore: true), null, CancellationToken.None);

        result.Restore!.Status.ShouldBe(RestoreStatus.Failed);
        File.ReadAllText(Path.Combine(Game(), "Game1.cs")).ShouldContain("GumExpressionService.Initialize()");
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_WhenCodegenFailsAfterThePackagesWereAdded_KeepsThePackageInitAndTheRuntimeLoadedScreen()
    {
        _codegen.Fail = true;

        NewProjectResult result = await Run(Request("monogame", packages: true, restore: true, generate: true));

        result.Codegen!.Success.ShouldBeFalse();
        string host = File.ReadAllText(Path.Combine(Game(), "Game1.cs"));
        host.ShouldContain("GumExpressionService.Initialize()");
        host.ShouldContain("ObjectFinder.Self.GetElementSave(\"DemoScreenGum\")");
        host.ShouldNotContain("new DemoScreenGum()");
    }
}
