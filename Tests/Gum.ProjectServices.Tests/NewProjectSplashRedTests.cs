#pragma warning disable xUnit1031
using System.Xml.Linq;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Red tests found by the New Project splash sweep (#7328). Each is a defect; each is listed in its own issue.
/// </summary>
public class NewProjectSplashRedTests : IDisposable
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly List<string> _calls = new();

    public NewProjectSplashRedTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "Gum Splash Red 7328 " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory) ?? throw new InvalidOperationException("Run from a Gum checkout.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private NewProjectService CreateService() => new NewProjectService(
        new FormsThemeLocator(new[] { Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes") }),
        new Restore(_calls), new Add(_calls), new[] { _checkout }, new Codegen(_calls));

    private NewProjectRequest Request(string? platform, string name = "Game", bool sourceLinked = false, bool packages = false,
        bool restore = false, bool generate = false) =>
        new NewProjectRequest
        {
            Path = Path.Combine(_folder, name), Platform = platform, Template = "forms", SourceLinked = sourceLinked,
            IncludeRecommendedPackages = packages, Restore = restore, GenerateCode = generate
        };

    [Theory]
    [InlineData("monogame")]
    [InlineData("kni")]
    [InlineData("raylib")]
    [InlineData("silknet")]
    [InlineData("stride")]
    public void Validate_RecommendedPackagesWithSourceLinked_IsAccepted(string platform)
    {
        NewProjectValidation validation = CreateService().Validate(Request(platform, sourceLinked: true, packages: true));

        validation.Error?.Message.ShouldBeNull();
        validation.Resolved.ShouldNotBeNull();
    }

    [Fact]
    public async Task Execute_SourceLinkedWithRecommendedPackages_LinksAProjectForEachAddOn()
    {
        NewProjectResult result = await CreateService().ExecuteAsync(
            Request("monogame", sourceLinked: true, packages: true), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string[] projectReferences = XDocument.Load(result.Files.CsprojPath!).Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")!.Value).ToArray();
        projectReferences.ShouldContain(include => include.Contains("KernSmith.MonoGameGum"), string.Join(" | ", projectReferences));
        projectReferences.ShouldContain(include => include.Contains("GumExpressions"), string.Join(" | ", projectReferences));
    }

    [Fact]
    public void Validate_KniSkiaWithoutSourceLink_IsRefused()
    {
        NewProjectValidation validation = CreateService().Validate(Request("kniskia", sourceLinked: false));

        validation.Resolved.ShouldBeNull("Gum.KniSkia is not on NuGet, so the project it writes can never restore.");
    }

    [Fact]
    public async Task Execute_GenerateCodeWithoutRestore_RunsNoCodegen()
    {
        NewProjectResult result = await CreateService().ExecuteAsync(
            Request("monogame", restore: false, generate: true), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        _calls.ShouldNotContain(call => call.StartsWith("codegen"), "The dialog says code generation needs the restore, so it is skipped.");
    }

    [Theory]
    [InlineData("a?b")]
    [InlineData("a*b")]
    [InlineData("a<b")]
    [InlineData("a|b")]
    public async Task Execute_NameWithACharacterWindowsForbids_FailsWithAMessageInsteadOfThrowing(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (string? platform in new string?[] { null, "monogame" })
        {
            NewProjectResult? result = null;
            Exception? thrown = null;
            try
            {
                result = await CreateService().ExecuteAsync(Request(platform, name), null, CancellationToken.None);
            }
            catch (Exception exception)
            {
                thrown = exception;
            }

            thrown.ShouldBeNull($"platform={platform}");
            result!.Files.Success.ShouldBeFalse($"platform={platform}");
            result.Files.ErrorMessage.ShouldNotBeEmpty($"platform={platform}");
        }
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    public async Task Execute_ReservedDeviceName_IsRefusedAndWritesNothing(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (string? platform in new string?[] { null, "monogame" })
        {
            NewProjectResult? result = null;
            Exception? thrown = null;
            try
            {
                result = await CreateService().ExecuteAsync(Request(platform, name), null, CancellationToken.None);
            }
            catch (Exception exception)
            {
                thrown = exception;
            }

            thrown.ShouldBeNull($"platform={platform}");
            result!.Files.Success.ShouldBeFalse($"platform={platform}");
            Directory.Exists(Path.Combine(_folder, name)).ShouldBeFalse($"platform={platform}");
        }
    }

    private class Restore : IRestoreRunner
    {
        private readonly List<string> _calls;
        public Restore(List<string> calls) => _calls = calls;
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add("restore");
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class Add : IPackageAddRunner
    {
        private readonly List<string> _calls;
        public Add(List<string> calls) => _calls = calls;
        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add("add " + packageId);
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class Codegen : IHeadlessCodegenRunner
    {
        private readonly List<string> _calls;
        public Codegen(List<string> calls) => _calls = calls;
        public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error)
        {
            _calls.Add("codegen");
            return 0;
        }
    }
}
