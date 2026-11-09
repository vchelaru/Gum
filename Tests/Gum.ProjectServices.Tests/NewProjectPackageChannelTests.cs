using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The "Gum packages" channel of New Project (#7356): Stable (the default), Preview (prerelease NuGet packages) and Local source.
/// The Preview host floats its Gum reference as <c>*-*</c> (the plain <c>*</c> does not resolve prereleases, NU1103), so no
/// network lookup is needed at creation, and each add runs with <c>--prerelease</c>.
/// </summary>
public class NewProjectPackageChannelTests : NewProjectServiceTestBase
{
    private NewProjectRequest ChannelRequest(
        string? platform, NewProjectPackageChannel channel, string name = "Game", bool packages = false, bool restore = false) =>
        new NewProjectRequest
        {
            Path = Path.Combine(_folder, name),
            Platform = platform,
            Template = "empty",
            PackageChannel = channel,
            Restore = restore,
            IncludeRecommendedPackages = packages,
        };

    private string Csproj(string name = "Game") => File.ReadAllText(Path.Combine(Game(name), name + ".csproj"));

    #region Request

    [Fact]
    public void Request_DefaultChannel_IsStable()
    {
        new NewProjectRequest().PackageChannel.ShouldBe(NewProjectPackageChannel.Stable);
    }

    [Fact]
    public void Request_SourceLinkedShorthand_IsTheLocalSourceChannel()
    {
        new NewProjectRequest { SourceLinked = true }.PackageChannel.ShouldBe(NewProjectPackageChannel.LocalSource);
        new NewProjectRequest { PackageChannel = NewProjectPackageChannel.LocalSource }.SourceLinked.ShouldBeTrue();
        new NewProjectRequest { PackageChannel = NewProjectPackageChannel.Preview }.SourceLinked.ShouldBeFalse();
    }

    #endregion

    #region Csproj

    [Theory]
    [MemberData(nameof(PlatformNames))]
    public async Task Preview_WritesAPrereleaseAwareGumReference(string platform)
    {
        NewProjectResult result = await Run(ChannelRequest(platform, NewProjectPackageChannel.Preview));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string csproj = Csproj();
        csproj.ShouldContain("Version=\"*-*\"");
        csproj.ShouldNotContain("Version=\"*\"");
    }

    [Fact]
    public async Task Preview_WithKniSkia_ReferencesTheGumKniSkiaPackage()
    {
        NewProjectResult result = await Run(ChannelRequest("kniskia", NewProjectPackageChannel.Preview));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        Csproj().ShouldContain("<PackageReference Include=\"Gum.KniSkia\" Version=\"*-*\"");
    }

    [Theory]
    [MemberData(nameof(PlatformNames))]
    public async Task Stable_KeepsTheFloatingGumReference(string platform)
    {
        if (platform == "kniskia")
        {
            return; // No stable package exists; see Stable_WithKniSkia_IsRefusedSayingThereIsNoStablePackage.
        }

        NewProjectResult result = await Run(ChannelRequest(platform, NewProjectPackageChannel.Stable));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string csproj = Csproj();
        csproj.ShouldContain("Version=\"*\"");
        csproj.ShouldNotContain("Version=\"*-*\"");
    }

    #endregion

    #region Package adds

    [Fact]
    public async Task Preview_AddsEveryRecommendedPackageWithPrerelease_FromThePreviewList()
    {
        PrereleaseRecordingAdd add = new PrereleaseRecordingAdd();

        NewProjectResult result = await CreateService(add: add).ExecuteAsync(
            ChannelRequest("monogame", NewProjectPackageChannel.Preview, packages: true), null, default);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        add.Added.Select(entry => entry.Package).ShouldBe(RecommendedPackages.For(HostPlatform.MonoGame, NewProjectPackageChannel.Preview));
        add.Added.ShouldAllBe(entry => entry.Prerelease);
    }

    [Fact]
    public async Task Stable_AddsPackagesWithoutPrerelease()
    {
        PrereleaseRecordingAdd add = new PrereleaseRecordingAdd();

        NewProjectResult result = await CreateService(add: add).ExecuteAsync(
            ChannelRequest("monogame", NewProjectPackageChannel.Stable, packages: true), null, default);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        add.Added.Select(entry => entry.Package).ShouldBe(RecommendedPackages.For(HostPlatform.MonoGame));
        add.Added.ShouldAllBe(entry => !entry.Prerelease);
    }

    [Fact]
    public void Preview_Validate_ResolvesThePreviewListAndNoLinkedAddOns()
    {
        NewProjectValidation validation = CreateService().Validate(ChannelRequest("monogame", NewProjectPackageChannel.Preview, packages: true));

        validation.Resolved!.PackageChannel.ShouldBe(NewProjectPackageChannel.Preview);
        validation.Resolved.UsesPrerelease.ShouldBeTrue();
        validation.Resolved.RecommendedPackages.ShouldBe(RecommendedPackages.For(HostPlatform.MonoGame, NewProjectPackageChannel.Preview));
        validation.Resolved.LinkedAddOns.ShouldBeEmpty();
        validation.Resolved.GumSourceDirectory.ShouldBeNull();
    }

    [Fact]
    public void LocalSource_Validate_StillLinksTheAddOnProjects()
    {
        NewProjectValidation validation = CreateService().Validate(ChannelRequest("monogame", NewProjectPackageChannel.LocalSource, packages: true));

        validation.Resolved!.UsesPrerelease.ShouldBeFalse();
        validation.Resolved.RecommendedPackages.ShouldBeEmpty();
        validation.Resolved.LinkedAddOns.ShouldBe(RecommendedPackages.ForSourceLinked(HostPlatform.MonoGame));
        validation.Resolved.GumSourceDirectory.ShouldNotBeNull();
    }

    #endregion

    #region Refusals

    [Fact]
    public void Preview_WithoutAPlatform_IsRefusedNamingThePlatformOption()
    {
        NewProjectValidation validation = CreateService().Validate(ChannelRequest(null, NewProjectPackageChannel.Preview));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.PreviewNeedsPlatform);
        validation.Error.Message.ShouldContain("--platform");
    }

    [Fact]
    public void Stable_WithKniSkia_IsRefusedSayingThereIsNoStablePackage()
    {
        NewProjectValidation validation = CreateService().Validate(ChannelRequest("kniskia", NewProjectPackageChannel.Stable));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.NoStablePackage);
        validation.Error.Message.ShouldContain("no stable");
        validation.Error.Message.ShouldContain("--preview");
        validation.Error.Message.ShouldContain("--source-linked");
    }

    [Fact]
    public void Preview_WithKniSkia_IsAccepted()
    {
        NewProjectValidation validation = CreateService().Validate(ChannelRequest("kniskia", NewProjectPackageChannel.Preview));

        validation.Error.ShouldBeNull();
        validation.Resolved!.UsesPrerelease.ShouldBeTrue();
    }

    #endregion

    #region Lists

    // A package that does not resolve fails the new project's restore, so none may be listed. There is no FNA platform, so its
    // ShadowDusk package is never listed.
    private static readonly string[] NotListed = { "Gum.FNA.ShadowDusk" };

    [Fact]
    public void For_Preview_CarriesEveryStablePackage_AndNeverAnUnlistedOne()
    {
        foreach ((string name, HostPlatform platform) in Platforms)
        {
            IReadOnlyList<string> preview = RecommendedPackages.For(platform, NewProjectPackageChannel.Preview);
            RecommendedPackages.For(platform).ShouldBeSubsetOf(preview, name);
            preview.Intersect(NotListed).ShouldBeEmpty(name);
        }
    }

    [Fact]
    public void For_Stable_IsUnchangedByTheChannelOverload()
    {
        foreach ((string name, HostPlatform platform) in Platforms)
        {
            RecommendedPackages.For(platform, NewProjectPackageChannel.Stable).ShouldBe(RecommendedPackages.For(platform), name);
        }
    }

    #endregion

    private class PrereleaseRecordingAdd : IPackageAddRunner
    {
        public List<(string Package, bool Prerelease)> Added { get; } = new();

        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            AddAsync(csprojPath, packageId, false, onOutputLine, cancellationToken);

        public Task<RestoreResult> AddAsync(
            string csprojPath, string packageId, bool prerelease, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            Added.Add((packageId, prerelease));
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }
}

/// <summary>The real runner's <c>dotnet add</c> arguments for each channel, and the pinned package lists (#7356).</summary>
public class NewProjectPackageListsTests
{
    [Fact]
    public void RealRunner_PreviewAdd_PassesPrerelease()
    {
        DotnetPackageAddRunner.GetArguments("G.csproj", "Gum.Expressions", prerelease: true)
            .ShouldBe(new[] { "add", "G.csproj", "package", "Gum.Expressions", "--no-restore", "--prerelease" });
    }

    [Fact]
    public void RealRunner_StableAdd_DoesNotPassPrerelease()
    {
        DotnetPackageAddRunner.GetArguments("G.csproj", "Gum.Expressions", prerelease: false)
            .ShouldBe(new[] { "add", "G.csproj", "package", "Gum.Expressions", "--no-restore" });
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame, NewProjectPackageChannel.Stable, "Gum.Shapes.MonoGame,KernSmith.MonoGameGum,Gum.MonoGame.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Kni, NewProjectPackageChannel.Stable, "Gum.Shapes.KNI,KernSmith.KniGum,Gum.KNI.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Raylib, NewProjectPackageChannel.Stable, "KernSmith.RaylibGum,Gum.raylib.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.SilkNet, NewProjectPackageChannel.Stable, "Gum.SkiaSharp.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Stride, NewProjectPackageChannel.Stable, "Gum.SkiaSharp.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.KniSkia, NewProjectPackageChannel.Stable, "")]
    [InlineData(HostPlatform.MonoGame, NewProjectPackageChannel.Preview, "Gum.Shapes.MonoGame,KernSmith.MonoGameGum,Gum.MonoGame.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Kni, NewProjectPackageChannel.Preview, "Gum.Shapes.KNI,KernSmith.KniGum,Gum.KNI.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Raylib, NewProjectPackageChannel.Preview, "KernSmith.RaylibGum,Gum.raylib.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.SilkNet, NewProjectPackageChannel.Preview, "Gum.SkiaSharp.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Stride, NewProjectPackageChannel.Preview, "Gum.SkiaSharp.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.KniSkia, NewProjectPackageChannel.Preview, "")]
    public void For_PinsEveryPlatformAndChannelList(HostPlatform platform, NewProjectPackageChannel channel, string expected)
    {
        string.Join(",", RecommendedPackages.For(platform, channel)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame, "Gum.Shapes.MonoGame,KernSmith.MonoGameGum,Gum.MonoGame.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Kni, "Gum.Shapes.KNI,KernSmith.KniGum,Gum.KNI.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Raylib, "KernSmith.RaylibGum,Gum.raylib.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.SilkNet, "Gum.SkiaSharp.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.Stride, "Gum.SkiaSharp.ShadowDusk,Gum.Expressions")]
    [InlineData(HostPlatform.KniSkia, "")]
    public void ForSourceLinked_PinsEveryPlatformsAddOns(HostPlatform platform, string expected)
    {
        string.Join(",", RecommendedPackages.ForSourceLinked(platform)).ShouldBe(expected);
    }
}
