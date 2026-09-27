using System.IO;
using Gum.Plugins.ImportPlugin.Services;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Covers GumxSourceService's pure URL/path helpers (NormalizeGitHubUrl, GetSourceBase).
/// The file-load path is covered by GumxSourceServiceEnumCoercionTests and
/// GumxImportServiceTests; the HTTP path isn't covered here since it needs a real network
/// call (HttpClient isn't seamed for mocking).
/// </summary>
public class GumxSourceServiceTests
{
    private readonly GumxSourceService _sut = new();

    [Fact]
    public void NormalizeGitHubUrl_GithubBlobUrl_ConvertsToRawContentUrl()
    {
        string result = _sut.NormalizeGitHubUrl("https://github.com/someuser/somerepo/blob/main/Project.gumx");

        result.ShouldBe("https://raw.githubusercontent.com/someuser/somerepo/main/Project.gumx");
    }

    [Fact]
    public void NormalizeGitHubUrl_NonGithubUrl_ReturnsUnchanged()
    {
        const string url = "https://example.com/blob/main/Project.gumx";

        _sut.NormalizeGitHubUrl(url).ShouldBe(url);
    }

    [Fact]
    public void NormalizeGitHubUrl_GithubUrlWithoutBlob_ReturnsUnchanged()
    {
        const string url = "https://github.com/someuser/somerepo/raw/main/Project.gumx";

        _sut.NormalizeGitHubUrl(url).ShouldBe(url);
    }

    [Fact]
    public void GetSourceBase_Url_ReturnsDirectoryPortionOfUrl()
    {
        _sut.GetSourceBase("https://raw.githubusercontent.com/user/repo/main/Project.gumx")
            .ShouldBe("https://raw.githubusercontent.com/user/repo/main/");
    }

    [Fact]
    public async Task FetchFromLocalFolder_BackslashRelativePath_ReadsTheFileOnEveryOS()
    {
        // A source project saved on Windows can name files with backslashes. On macOS/Linux a
        // backslash is a file-name character, so the local read must convert it, as the URL
        // branch already does.
        string sourceBase = Path.Combine(Path.GetTempPath(), "GumxSourceServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(sourceBase, "Components"));
        try
        {
            File.WriteAllText(Path.Combine(sourceBase, "Components", "Button.gucx"), "element text");
            File.WriteAllBytes(Path.Combine(sourceBase, "Components", "Icon.png"), new byte[] { 1, 2, 3 });

            string? text = await _sut.FetchElementTextAsync("Components\\Button.gucx", sourceBase);
            byte[]? bytes = await _sut.FetchBinaryAsync("Components\\Icon.png", sourceBase);

            text.ShouldBe("element text");
            bytes.ShouldBe(new byte[] { 1, 2, 3 });
        }
        finally
        {
            Directory.Delete(sourceBase, recursive: true);
        }
    }

    [Fact]
    public void GetSourceBase_LocalPath_ReturnsContainingDirectory()
    {
        string path = Path.Combine("C:", "SomeFolder", "SubFolder", "Project.gumx");

        _sut.GetSourceBase(path).ShouldBe(Path.Combine("C:", "SomeFolder", "SubFolder") + Path.DirectorySeparatorChar);
    }
}
