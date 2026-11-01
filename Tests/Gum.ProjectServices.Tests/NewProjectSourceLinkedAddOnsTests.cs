using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Source-linked New Project with the recommended add-ons (#7328): each add-on is a ProjectReference to its project in the Gum
/// checkout (what the NuGet package is for a normal project), not a package and not a refusal.
/// </summary>
public class NewProjectSourceLinkedAddOnsTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly List<string> _addCalls = new List<string>();

    public NewProjectSourceLinkedAddOnsTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumSourceLinkedAddOns_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    private NewProjectService CreateService()
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[]
        {
            Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes")
        });
        return new NewProjectService(locator, new FakeRestore(), new FakePackageAdd(_addCalls), new[] { _checkout });
    }

    // Every ProjectReference of the csproj as a full path, undoing MSBuild's %XX escapes.
    private static List<string> ReferencedProjects(string csproj) =>
        XDocument.Load(csproj).Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")!.Value)
            .Select(include => Regex.Replace(include, "%([0-9A-Fa-f]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()))
            .Select(include => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, include.Replace('\\', Path.DirectorySeparatorChar))))
            .ToList();

    [Theory]
    [InlineData(HostPlatform.MonoGame, "Gum.Shapes.MonoGame,KernSmith.MonoGameGum,Gum.Expressions")]
    [InlineData(HostPlatform.Kni, "Gum.Shapes.KNI,KernSmith.KniGum,Gum.Expressions")]
    [InlineData(HostPlatform.Raylib, "KernSmith.RaylibGum,Gum.Expressions")]
    [InlineData(HostPlatform.SilkNet, "Gum.Expressions")]
    [InlineData(HostPlatform.Stride, "Gum.Expressions")]
    [InlineData(HostPlatform.KniSkia, "")]
    public void ForSourceLinked_EachPlatform_ListsTheAddOnsTheCheckoutCanLink(HostPlatform platform, string expected)
    {
        string.Join(",", RecommendedPackages.ForSourceLinked(platform)).ShouldBe(expected);
    }

    [Fact]
    public void ForSourceLinked_EveryAddOn_HasAProjectInTheCheckout_AndNuGetAddOnsAreAllIncluded()
    {
        foreach ((string name, HostPlatform platform) in NewProjectPlatforms.MainNames)
        {
            foreach (string package in RecommendedPackages.ForSourceLinked(platform))
            {
                File.Exists(Path.Combine(_checkout, RecommendedPackages.GetSourceProject(package))).ShouldBeTrue($"{name}: {package}");
            }

            RecommendedPackages.For(platform).Except(RecommendedPackages.ForSourceLinked(platform)).ShouldBeEmpty(name);
        }
    }

    [Theory]
    [InlineData("monogame")]
    [InlineData("kni")]
    [InlineData("raylib")]
    [InlineData("silknet")]
    [InlineData("stride")]
    [InlineData("kniskia")]
    public async Task Execute_SourceLinkedWithAddOns_ReferencesEachAddOnProjectThatExists_AndAddsNoPackage(string platform)
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Linked_" + platform), Platform = platform, Template = "empty", Restore = false,
            SourceLinked = true, IncludeRecommendedPackages = true
        };

        NewProjectResult result = await service.ExecuteAsync(request, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        NewProjectPlatforms.TryParse(platform, out HostPlatform parsed).ShouldBeTrue();
        List<string> referenced = ReferencedProjects(result.Files.CsprojPath!);
        foreach (string package in RecommendedPackages.ForSourceLinked(parsed))
        {
            string expected = Path.GetFullPath(Path.Combine(_checkout, RecommendedPackages.GetSourceProject(package)));
            referenced.ShouldContain(expected, package);
            File.Exists(expected).ShouldBeTrue(package);
        }
        referenced.Count.ShouldBe(RecommendedPackages.ForSourceLinked(parsed).Count + 1, "the runtime project plus the add-ons");
        _addCalls.ShouldBeEmpty();
        result.PackageAdd.ShouldBeNull();
    }

    [Fact]
    public async Task Execute_SourceLinkedMonoGame_HostInitializesTheLinkedShapesAndAddOns()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "ShapesGame");

        NewProjectResult result = await service.ExecuteAsync(new NewProjectRequest
        {
            Path = game, Platform = "monogame", Template = "empty", Restore = false, SourceLinked = true, IncludeRecommendedPackages = true
        }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string host = File.ReadAllText(Path.Combine(game, "Game1.cs"));
        host.ShouldContain("Gum.Renderables.ShapeRenderer.Self.Initialize();");
        host.ShouldContain("KernSmithFontCreator");
        host.ShouldContain("GumExpressionService.Initialize();");
    }

    [Fact]
    public async Task Execute_SourceLinkedWithoutTheAddOnsOption_LinksOnlyTheRuntime()
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Plain"), Platform = "monogame", Template = "empty", Restore = false, SourceLinked = true
        }, null, CancellationToken.None);

        ReferencedProjects(result.Files.CsprojPath!).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Execute_SourceLinkedUnderAFolderWithSpaces_ReferencesTheAddOnsByARelativePathThatResolves()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "My Games", "Vic Personal", "SpaceGame");

        NewProjectResult result = await service.ExecuteAsync(new NewProjectRequest
        {
            Path = game, Platform = "kni", Template = "empty", Restore = false, SourceLinked = true, IncludeRecommendedPackages = true
        }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        ReferencedProjects(result.Files.CsprojPath!).ShouldAllBe(path => File.Exists(path));
        ReferencedProjects(result.Files.CsprojPath!).Count.ShouldBe(4);
    }

    [Fact]
    public void Create_AddOnsFromACheckoutWithSpacesAndSpecialCharacters_WritesWellFormedReferencesThatResolve()
    {
        string checkout = Path.Combine(_folder, "Gum Checkout 100%41 R&D");
        foreach (string package in new[] { "Gum.Shapes.MonoGame", "KernSmith.MonoGameGum", "Gum.Expressions" })
        {
            string project = Path.Combine(checkout, RecommendedPackages.GetSourceProject(package));
            Directory.CreateDirectory(Path.GetDirectoryName(project)!);
            File.WriteAllText(project, "<Project />");
        }
        string runtime = Path.Combine(checkout, "MonoGameGum", "MonoGameGum.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
        File.WriteAllText(runtime, "<Project />");
        string game = Path.Combine(_folder, "Game");

        // Code generation setup may stop on the stand-in projects; the host project is written first.
        try
        {
            new PlatformProjectScaffolder().Create(
                game, HostPlatform.MonoGame, false, checkout, linkedAddOns: RecommendedPackages.ForSourceLinked(HostPlatform.MonoGame));
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
        }

        List<string> referenced = ReferencedProjects(Path.Combine(game, "Game.csproj"));
        referenced.Count.ShouldBe(4);
        referenced.ShouldAllBe(path => File.Exists(path));
    }

    [Fact]
    public async Task Execute_SourceLinkedToACheckoutMissingAnAddOnProject_FailsAndLeavesNothing()
    {
        string partial = Path.Combine(_folder, "Partial");
        string runtime = Path.Combine(partial, "MonoGameGum", "MonoGameGum.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
        File.WriteAllText(runtime, "<Project />");
        Directory.CreateDirectory(Path.Combine(partial, "GumCommon"));
        File.WriteAllText(Path.Combine(partial, "GumCommon", "GumCommon.csproj"), "<Project />");
        NewProjectService service = new NewProjectService(
            new FormsThemeLocator(Array.Empty<string>()), new FakeRestore(), new FakePackageAdd(_addCalls), new[] { partial });
        string game = Path.Combine(_folder, "Out", "Game");

        NewProjectResult result = await service.ExecuteAsync(new NewProjectRequest
        {
            Path = game, Platform = "monogame", Template = "empty", Restore = false, SourceLinked = true, IncludeRecommendedPackages = true
        }, null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldContain("Gum source not found");
        Directory.Exists(Path.Combine(_folder, "Out")).ShouldBeFalse();
    }

    private class FakeRestore : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }

    private class FakePackageAdd : IPackageAddRunner
    {
        private readonly List<string> _calls;

        public FakePackageAdd(List<string> calls) => _calls = calls;

        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add(packageId);
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }
}
