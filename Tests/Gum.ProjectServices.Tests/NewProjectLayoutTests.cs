using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Pins <see cref="NewProjectLayout"/> against what the real creation writes, for every platform, template and
/// file format, so the folders the New Project dialog shows cannot drift from what the creators do.
/// </summary>
public class NewProjectLayoutTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _checkout;

    public NewProjectLayoutTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumNewProjectLayout_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    public static IEnumerable<object?[]> AllCombinations()
    {
        foreach (string? platform in new string?[] { null, "monogame", "kni", "kniskia", "raylib", "stride", "silknet" })
        {
            foreach (bool xml in new[] { false, true })
            {
                yield return new object?[] { platform, "empty", null, xml };
                yield return new object?[] { platform, "forms", null, xml };
                yield return new object?[] { platform, "forms", "Meadow", xml };
            }
        }
    }

    private NewProjectService CreateService()
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[]
        {
            Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes")
        });
        return new NewProjectService(locator, new NoRestoreRunner(), new[] { _checkout });
    }

    private async Task<(string Root, NewProjectLayout Layout)> CreateAsync(string? platform, string template, string? theme, bool xml)
    {
        string root = Path.Combine(_folder, "Game");
        NewProjectResult result = await CreateService().ExecuteAsync(
            new NewProjectRequest
            {
                Path = root, Platform = platform, Template = template, Theme = theme, Restore = false, UseXmlFormat = xml
            },
            null, CancellationToken.None);
        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);

        NewProjectLayout layout = platform == null
            ? NewProjectLayout.ForGumOnly(root, "Game", xml)
            : NewProjectLayout.ForCodeProject(root, "Game", ParsePlatform(platform), xml);
        return (root, layout);
    }

    private static HostPlatform ParsePlatform(string name)
    {
        NewProjectPlatforms.TryParse(name, out HostPlatform platform).ShouldBeTrue();
        return platform;
    }

    private static List<string> Names(IEnumerable<string> paths) =>
        paths.Select(path => Path.GetFileName(path)).OrderBy(name => name, StringComparer.Ordinal).ToList();

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public async Task Layout_ForEveryPlatformTemplateAndTheme_NamesFoldersAndFilesTheCreationWrites(
        string? platform, string template, string? theme, bool xml)
    {
        (string root, NewProjectLayout layout) = await CreateAsync(platform, template, theme, xml);

        foreach (string folder in layout.Folders)
        {
            Directory.Exists(Path.Combine(root, folder)).ShouldBeTrue($"{folder} is in the layout but was not created");
        }
        File.Exists(Path.Combine(root, layout.GumProjectFile)).ShouldBeTrue(layout.GumProjectFile);
        Names(Directory.GetFiles(root).Where(file => platform != null)).ShouldBe(
            layout.HostFiles.OrderBy(name => name, StringComparer.Ordinal).ToList());
    }

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public async Task Layout_ForEveryPlatformTemplateAndTheme_CoversEveryFolderTheCreationWrites(
        string? platform, string template, string? theme, bool xml)
    {
        (string root, NewProjectLayout layout) = await CreateAsync(platform, template, theme, xml);

        string gumFolder = Path.Combine(root, layout.GumFolder);
        List<string> expectedRootFolders = platform == null
            ? NewProjectLayout.GumFolders.Concat(NewProjectLayout.OptionalGumFolders).OrderBy(name => name, StringComparer.Ordinal).ToList()
            : new List<string> { NewProjectLayout.ContentFolderName };
        Names(Directory.GetDirectories(root)).ShouldBeSubsetOf(expectedRootFolders);
        if (platform != null)
        {
            Names(Directory.GetDirectories(Path.Combine(root, NewProjectLayout.ContentFolderName)))
                .ShouldBe(new List<string> { NewProjectLayout.GumProjectFolderName });
        }

        List<string> written = Names(Directory.GetDirectories(gumFolder));
        written.ShouldBeSubsetOf(NewProjectLayout.GumFolders.Concat(NewProjectLayout.OptionalGumFolders).ToList());
        NewProjectLayout.GumFolders.ShouldBeSubsetOf(written);
    }

    [Theory]
    [InlineData("monogame", true)]
    [InlineData("kni", true)]
    [InlineData("kniskia", true)]
    [InlineData("raylib", false)]
    [InlineData("stride", false)]
    [InlineData("silknet", false)]
    public async Task WritesGame1_MatchesWhetherTheCreationWritesGame1(string platform, bool expected)
    {
        (string root, _) = await CreateAsync(platform, "empty", null, xml: false);

        NewProjectLayout.WritesGame1(ParsePlatform(platform)).ShouldBe(expected);
        File.Exists(Path.Combine(root, NewProjectLayout.Game1FileName)).ShouldBe(expected);
    }

    private class NoRestoreRunner : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded });
    }
}
