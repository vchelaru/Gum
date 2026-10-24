using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
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
    public void LoadContent_WithContentManager_TextureCreationFails_ThrowsRealException()
    {
        // The file is served, so creating the texture fails (unreadable bytes, and no device in this
        // headless test). That failure must surface, not a ContentLoadException from the fallback.
        FileManager.CustomGetStreamFromFile = path => path.EndsWith("corrupt_6717.png")
            ? new MemoryStream(new byte[] { 1, 2, 3, 4 })
            : null!;
        ContentLoader contentLoader = new ContentLoader
        {
            XnaContentManager = new ContentManager(new GameServiceContainer(), "Content")
        };

        Exception exception = Should.Throw<Exception>(() =>
            contentLoader.LoadContent<Texture2D>("corrupt_6717.png"));

        exception.ShouldNotBeOfType<ContentLoadException>();
    }

    [Fact]
    public void LoadContent_WithContentManager_StreamHookFailsWithNonMissingError_ThrowsHookError()
    {
        FileManager.CustomGetStreamFromFile = path => path.EndsWith("hookfails_6717.png")
            ? throw new InvalidDataException("bundle is corrupt 6717")
            : null!;
        ContentLoader contentLoader = new ContentLoader
        {
            XnaContentManager = new ContentManager(new GameServiceContainer(), "Content")
        };

        Exception exception = Should.Throw<Exception>(() =>
            contentLoader.LoadContent<Texture2D>("hookfails_6717.png"));

        exception.ShouldNotBeOfType<ContentLoadException>();
        exception.ToString().ShouldContain("bundle is corrupt 6717");
    }

    [Fact]
    public void LoadContent_WithContentManager_FileMissing_FallsBackToContentManager()
    {
        // A file that is not there may be a content-pipeline asset, so the ContentManager is asked.
        ContentLoader contentLoader = new ContentLoader
        {
            XnaContentManager = new ContentManager(new GameServiceContainer(), "Content")
        };

        Should.Throw<ContentLoadException>(() =>
            contentLoader.LoadContent<Texture2D>("missing_6717.png"));
    }

    [Fact]
    public void LoadContent_WithContentManager_DirectoryMissing_FallsBackToContentManager()
    {
        ContentLoader contentLoader = new ContentLoader
        {
            XnaContentManager = new ContentManager(new GameServiceContainer(), "Content")
        };

        Should.Throw<ContentLoadException>(() =>
            contentLoader.LoadContent<Texture2D>("missing_dir_6717/missing_6717.png"));
    }

    [Fact]
    public void LoadContent_WithContentManager_StreamHookThrowsFileNotFound_FallsBackToContentManager()
    {
        FileManager.CustomGetStreamFromFile = path => path.EndsWith("hookmissing_6717.png")
            ? throw new FileNotFoundException("not in bundle 6717", path)
            : null!;
        ContentLoader contentLoader = new ContentLoader
        {
            XnaContentManager = new ContentManager(new GameServiceContainer(), "Content")
        };

        Should.Throw<ContentLoadException>(() =>
            contentLoader.LoadContent<Texture2D>("hookmissing_6717.png"));
    }

    [Fact]
    public void TryLoadContent_WithContentManager_TextureCreationFails_ReturnsNull()
    {
        FileManager.CustomGetStreamFromFile = path => path.EndsWith("trycorrupt_6717.png")
            ? new MemoryStream(new byte[] { 1, 2, 3, 4 })
            : null!;
        ContentLoader contentLoader = new ContentLoader
        {
            XnaContentManager = new ContentManager(new GameServiceContainer(), "Content")
        };

        Texture2D? result = contentLoader.TryLoadContent<Texture2D>("trycorrupt_6717.png");

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
