using Shouldly;

namespace Gum.ProjectServices.Tests;

public class AssetTreeCopierTests : IDisposable
{
    private readonly string _tempDirectory;

    public AssetTreeCopierTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumAssetTreeCopierTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CopyStagedAssets_ShouldCopyNestedFilesFromEachAssetFolder()
    {
        string stagedDir = Path.Combine(_tempDirectory, "staged");
        string imagesSubdir = Path.Combine(stagedDir, "Images", "icons");
        Directory.CreateDirectory(imagesSubdir);
        File.WriteAllText(Path.Combine(imagesSubdir, "icon.png"), "fake-png");

        string fontsDir = Path.Combine(stagedDir, "Fonts");
        Directory.CreateDirectory(fontsDir);
        File.WriteAllText(Path.Combine(fontsDir, "Arial18.fnt"), "fake-fnt");

        string projectDir = Path.Combine(_tempDirectory, "project");
        Directory.CreateDirectory(projectDir);

        AssetTreeCopier.CopyStagedAssets(stagedDir, projectDir);

        File.Exists(Path.Combine(projectDir, "Images", "icons", "icon.png")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, "Fonts", "Arial18.fnt")).ShouldBeTrue();
        Directory.Exists(Path.Combine(projectDir, "FontCache")).ShouldBeFalse();
    }

    [Fact]
    public void CopyStagedAssets_ShouldOverwriteExistingFiles()
    {
        string stagedDir = Path.Combine(_tempDirectory, "staged");
        Directory.CreateDirectory(Path.Combine(stagedDir, "Images"));
        File.WriteAllText(Path.Combine(stagedDir, "Images", "icon.png"), "new-content");

        string projectDir = Path.Combine(_tempDirectory, "project");
        Directory.CreateDirectory(Path.Combine(projectDir, "Images"));
        File.WriteAllText(Path.Combine(projectDir, "Images", "icon.png"), "old-content");

        AssetTreeCopier.CopyStagedAssets(stagedDir, projectDir);

        File.ReadAllText(Path.Combine(projectDir, "Images", "icon.png")).ShouldBe("new-content");
    }
}
