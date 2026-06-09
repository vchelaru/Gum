using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class ContentLoaderTests : BaseTestClass
{
    [Fact]
    public void LoadContent_MissingFileOnDesktop_ReturnsNullWithoutThrowing()
    {
        // Regression test for the missing-texture exception storm (issue #3075). On desktop the loader
        // used to fall into File.OpenRead for a missing file, throwing a (caught) exception per missing
        // file on every wireframe rebuild. With a debugger attached, those first-chance exceptions made
        // selecting a screen with many missing files take tens of seconds. The loader now detects the
        // missing file up front (File.Exists) and returns null instead of throwing.
        ContentLoader contentLoader = new ContentLoader();

        Texture2D result = null;
        Should.NotThrow(() =>
            result = contentLoader.LoadContent<Texture2D>("file_that_does_not_exist_3075.png"));

        result.ShouldBeNull();
    }

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
