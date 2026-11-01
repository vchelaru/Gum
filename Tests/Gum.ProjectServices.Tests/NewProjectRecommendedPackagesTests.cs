using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The "Include recommended packages" step of New Project (#7265): the per-platform package table, and the service adding
/// those packages to the new .csproj between writing the files and restoring.
/// </summary>
public class NewProjectRecommendedPackagesTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly List<string> _calls = new List<string>();
    private readonly FakeRestore _restore;
    private readonly FakePackageAdd _packageAdd;

    public NewProjectRecommendedPackagesTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumNewProjectPackages_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
        _restore = new FakeRestore(_calls);
        _packageAdd = new FakePackageAdd(_calls);
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    private NewProjectService CreateService(IPackageAddRunner? packageAdd = null)
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[]
        {
            Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes")
        });
        return new NewProjectService(locator, _restore, packageAdd ?? _packageAdd, new[] { _checkout });
    }

    private NewProjectRequest Request(string platform, bool packages = true, bool restore = true, bool sourceLinked = false) =>
        new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Game_" + platform),
            Platform = platform,
            Template = "empty",
            Restore = restore,
            SourceLinked = sourceLinked,
            IncludeRecommendedPackages = packages
        };

    #region Table

    [Theory]
    [InlineData(HostPlatform.MonoGame, "KernSmith.MonoGameGum,Gum.Expressions")]
    [InlineData(HostPlatform.Kni, "KernSmith.KniGum,Gum.Expressions")]
    [InlineData(HostPlatform.Raylib, "KernSmith.RaylibGum,Gum.Expressions")]
    [InlineData(HostPlatform.SilkNet, "Gum.Expressions")]
    [InlineData(HostPlatform.Stride, "Gum.Expressions")]
    [InlineData(HostPlatform.KniSkia, "")]
    public void For_EachPlatform_ListsThePackagesTheSetupDocsRecommend(HostPlatform platform, string expected)
    {
        string.Join(",", RecommendedPackages.For(platform)).ShouldBe(expected);
    }

    [Fact]
    public void For_EveryScaffoldedPlatform_IsCovered_AndNeverAddsWebOnlyOrSkiaShapes()
    {
        foreach ((string name, HostPlatform platform) in NewProjectPlatforms.MainNames)
        {
            IReadOnlyList<string> packages = RecommendedPackages.For(platform);
            packages.ShouldNotContain("KernSmith.Rasterizers.StbTrueType", name);
            if (platform is HostPlatform.KniSkia or HostPlatform.Stride or HostPlatform.SilkNet)
            {
                packages.Any(package => package.StartsWith("Gum.Shapes.")).ShouldBeFalse(name);
            }
        }
    }

    // nuget.org was checked on 2026-10-31: these ids exist only as previews. `dotnet add package X --no-restore` writes the
    // floating Version="*", which does not resolve to a prerelease, so restore fails with NU1103. Remove an id from this
    // list (and add it to the table) only after it has a stable version.
    private static readonly string[] PrereleaseOnlyOnNuGet =
    {
        "Gum.SkiaSharp.ShadowDusk", "Gum.MonoGame.ShadowDusk", "Gum.KNI.ShadowDusk", "Gum.raylib.ShadowDusk", "Gum.FNA.ShadowDusk",
    };

    [Fact]
    public void For_NoPlatform_ListsAPackageThatIsPrereleaseOnly()
    {
        foreach ((string name, HostPlatform platform) in NewProjectPlatforms.MainNames)
        {
            RecommendedPackages.For(platform).Intersect(PrereleaseOnlyOnNuGet).ShouldBeEmpty(name);
        }
    }

    #endregion

    #region Validate

    [Fact]
    public void Validate_WithThePackagesOption_ResolvesThePlatformsPackages()
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame"));

        validation.Resolved!.RecommendedPackages.ShouldBe(new[] { "KernSmith.MonoGameGum", "Gum.Expressions" });
    }

    [Fact]
    public void Validate_WithoutThePackagesOption_ResolvesNoPackages()
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame", packages: false));

        validation.Resolved!.RecommendedPackages.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_PackagesWithoutAPlatform_IsRefused()
    {
        NewProjectValidation validation = CreateService().Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "GumOnly"),
            IncludeRecommendedPackages = true
        });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.RecommendedPackagesNeedPlatform);
    }

    [Fact]
    public void Validate_PackagesWithSourceLinked_LinksTheAddOnsInsteadOfAddingPackages()
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame", sourceLinked: true));

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
        validation.Resolved!.RecommendedPackages.ShouldBeEmpty();
        validation.Resolved.LinkedAddOns.ShouldBe(new[] { "Gum.Shapes.MonoGame", "KernSmith.MonoGameGum", "Gum.Expressions" });
    }

    #endregion

    #region Adding

    [Fact]
    public async Task Execute_WithThePackagesOption_AddsEachPackageToTheNewCsproj_BeforeTheRestore()
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request("kni"), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string csproj = result.Files.CsprojPath!;
        _calls.ShouldBe(new[]
        {
            $"add {csproj} KernSmith.KniGum", $"add {csproj} Gum.Expressions", $"restore {csproj}"
        });
        result.PackageAdd!.Status.ShouldBe(RestoreStatus.Succeeded);
        result.Restore!.Status.ShouldBe(RestoreStatus.Succeeded);
    }

    [Fact]
    public async Task Execute_WithoutThePackagesOption_AddsNothing()
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request("kni", packages: false), null, CancellationToken.None);

        _calls.ShouldBe(new[] { $"restore {result.Files.CsprojPath}" });
        result.PackageAdd.ShouldBeNull();
    }

    [Fact]
    public async Task Execute_WhenAnAddFails_StopsThere_SkipsTheRestore_AndKeepsTheFiles()
    {
        _packageAdd.FailOn = "KernSmith.KniGum";
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request("kni"), null, CancellationToken.None);

        result.PackageAdd!.Status.ShouldBe(RestoreStatus.Failed);
        result.PackageAdd.FailedPackage.ShouldBe("KernSmith.KniGum");
        result.Restore.ShouldBeNull();
        _calls.Count.ShouldBe(1);
        File.Exists(result.Files.CsprojPath).ShouldBeTrue();
    }

    [Fact]
    public async Task AddPackages_ForAPlatformWithoutPackages_RunsNothingAndSucceeds()
    {
        NewProjectService service = CreateService();

        AddPackagesResult result = await service.AddPackagesAsync(
            Path.Combine(_folder, "x.csproj"), Array.Empty<string>(), null, CancellationToken.None);

        result.Status.ShouldBe(RestoreStatus.Succeeded);
        _calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Execute_WithTheRealDotnet_WritesAPackageReferencePerPackage_Offline()
    {
        NewProjectService service = CreateService(new DotnetPackageAddRunner());

        NewProjectResult result = await service.ExecuteAsync(Request("monogame", restore: false), null, CancellationToken.None);

        result.PackageAdd!.Status.ShouldBe(RestoreStatus.Succeeded, result.PackageAdd.OutputTail);
        string csproj = File.ReadAllText(result.Files.CsprojPath!);
        foreach (string package in RecommendedPackages.For(HostPlatform.MonoGame))
        {
            csproj.ShouldContain($"<PackageReference Include=\"{package}\" Version=\"*\"");
        }
        csproj.ShouldContain("Include=\"Gum.MonoGame\"");
    }

    #endregion

    private class FakeRestore : IRestoreRunner
    {
        private readonly List<string> _calls;

        public FakeRestore(List<string> calls) => _calls = calls;

        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add($"restore {csprojPath}");
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class FakePackageAdd : IPackageAddRunner
    {
        private readonly List<string> _calls;

        public FakePackageAdd(List<string> calls) => _calls = calls;

        public string? FailOn { get; set; }

        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add($"add {csprojPath} {packageId}");
            return Task.FromResult(packageId == FailOn
                ? new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "error: no network" }
                : new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }
}
