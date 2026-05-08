using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class ContentLoaderTests : BaseTestClass
{
    [Fact]
    public void ResolveContentManagerAssetName_Android_DotSlashRelativePath()
    {
        // On Android, FileManager.RelativeDirectory becomes "./Content/" (see FileManager.RelativeDirectory
        // setter), so the standardized file name is "./Content/images/atlas". Previously this fell through
        // MakeRelative unchanged and ContentManager.Load then prepended RootDirectory, producing
        // "Content/./Content/images/atlas.xnb" — the exact ContentLoadException reported.
        string result = ContentLoader.ResolveContentManagerAssetName(
            contentRootDirectory: "Content",
            fileNameStandardized: "./Content/images/atlas");

        result.ShouldBe("images/atlas");
    }

    [Fact]
    public void ResolveContentManagerAssetName_Android_EmptyExeLocation()
    {
        string result = ContentLoader.ResolveContentManagerAssetName(
            contentRootDirectory: "Content",
            fileNameStandardized: "./Content/images/atlas");

        result.ShouldBe("images/atlas");
    }

    [Fact]
    public void ResolveContentManagerAssetName_Android_RootExeLocation()
    {
        string result = ContentLoader.ResolveContentManagerAssetName(
            contentRootDirectory: "Content",
            fileNameStandardized: "./Content/images/atlas");

        result.ShouldBe("images/atlas");
    }

    [Fact]
    public void ResolveContentManagerAssetName_Desktop_StripsExeFolderAndRoot()
    {
        string result = ContentLoader.ResolveContentManagerAssetName(
            contentRootDirectory: "Content",
            fileNameStandardized: "C:/app/bin/Debug/Content/images/atlas");

        result.ShouldBe("images/atlas");
    }

    [Fact]
    public void ResolveContentManagerAssetName_NestedAssetPath()
    {
        string result = ContentLoader.ResolveContentManagerAssetName(
            contentRootDirectory: "Content",
            fileNameStandardized: "C:/app/bin/Debug/Content/ui/buttons/play");

        result.ShouldBe("ui/buttons/play");
    }

    [Fact]
    public void ResolveContentManagerAssetName_RootDirectoryWithTrailingSlash()
    {
        string result = ContentLoader.ResolveContentManagerAssetName(
            contentRootDirectory: "Content/",
            fileNameStandardized: "C:/app/bin/Debug/Content/images/atlas");

        result.ShouldBe("images/atlas");
    }
}
