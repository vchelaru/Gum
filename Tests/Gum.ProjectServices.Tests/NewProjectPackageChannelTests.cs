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
        if (platform == "kniskia")
        {
            return; // Always local source; see Preview_WithKniSkia_IsRefused.
        }

        NewProjectResult result = await Run(ChannelRequest(platform, NewProjectPackageChannel.Preview));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string csproj = Csproj();
        csproj.ShouldContain("Version=\"*-*\"");
        csproj.ShouldNotContain("Version=\"*\"");
    }

    [Theory]
    [MemberData(nameof(PlatformNames))]
    public async Task Stable_KeepsTheFloatingGumReference(string platform)
    {
        if (platform == "kniskia")
        {
            return;
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
    public void Preview_WithKniSkia_IsRefusedSayingSourceLinkedIsTheWay()
    {
        NewProjectValidation validation = CreateService().Validate(ChannelRequest("kniskia", NewProjectPackageChannel.Preview));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.KniSkiaNeedsSourceLinked);
        validation.Error.Message.ShouldContain("--source-linked");
    }

    #endregion

    #region Lists

    // nuget.org was checked on 2026-11-01: these ids are not published in any version. A package that does not resolve fails the
    // new project's restore, so none may be listed.
    private static readonly string[] NotPublished =
    {
        "Gum.MonoGame.ShadowDusk", "Gum.KNI.ShadowDusk", "Gum.raylib.ShadowDusk", "Gum.FNA.ShadowDusk",
    };

    [Theory]
    [InlineData(HostPlatform.MonoGame, "Gum.Shapes.MonoGame,KernSmith.MonoGameGum,Gum.Expressions")]
    [InlineData(HostPlatform.Kni, "Gum.Shapes.KNI,KernSmith.KniGum,Gum.Expressions")]
    [InlineData(HostPlatform.Raylib, "KernSmith.RaylibGum,Gum.Expressions")]
    [InlineData(HostPlatform.SilkNet, "Gum.Expressions,Gum.SkiaSharp.ShadowDusk")]
    [InlineData(HostPlatform.Stride, "Gum.Expressions,Gum.SkiaSharp.ShadowDusk")]
    [InlineData(HostPlatform.KniSkia, "")]
    public void For_Preview_ListsThePackagesPublishedAsPreviews(HostPlatform platform, string expected)
    {
        string.Join(",", RecommendedPackages.For(platform, NewProjectPackageChannel.Preview)).ShouldBe(expected);
    }

    [Fact]
    public void For_Preview_CarriesEveryStablePackage_AndNeverAnUnpublishedOne()
    {
        foreach ((string name, HostPlatform platform) in Platforms)
        {
            IReadOnlyList<string> preview = RecommendedPackages.For(platform, NewProjectPackageChannel.Preview);
            RecommendedPackages.For(platform).ShouldBeSubsetOf(preview, name);
            preview.Intersect(NotPublished).ShouldBeEmpty(name);
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
