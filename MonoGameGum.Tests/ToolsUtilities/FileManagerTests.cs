using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.ToolsUtilities;
public class FileManagerTests : IDisposable
{
    private readonly Func<string, Stream>? _previousHook = FileManager.CustomGetStreamFromFile;

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
    }

    [Fact]
    public void GetStreamForFile_ShouldThrowFileNotFoundException_WhenCustomHookReturnsNull()
    {
        FileManager.CustomGetStreamFromFile = _ => null!;

        var ex = Should.Throw<IOException>(() => FileManager.GetStreamForFile("anything.gumx"));

        ex.InnerException.ShouldBeOfType<FileNotFoundException>();
        // GetStreamForFile normalizes the path to absolute before invoking the hook so that
        // it agrees with FileExists on what file is being asked for. The FileNotFoundException
        // therefore carries the absolute path, not the original relative input.
        ((FileNotFoundException)ex.InnerException!).FileName.ShouldEndWith("anything.gumx");
        Path.IsPathRooted(((FileNotFoundException)ex.InnerException!).FileName).ShouldBeTrue();
    }

    [Fact]
    public void FromFileText_ShouldLoad_WhenPathHasDotDotSlash()
    {
        System.IO.Directory.CreateDirectory("DirectoryA1");
        System.IO.Directory.CreateDirectory("DirectoryB1");

        System.IO.File.WriteAllText("DirectoryA1/test.txt", "Test content A");

        var text = FileManager.FromFileText("DirectoryB1/../DirectoryA1/test.txt");

        text.ShouldBe("Test content A");
    }

    [Fact]
    public void FromFileText_ShouldLoad_WhenPathHasBackSlashes()
    {

        System.IO.Directory.CreateDirectory("DirectoryA2");
        System.IO.Directory.CreateDirectory("DirectoryB2");

        System.IO.File.WriteAllText("DirectoryA2/test.txt", "Test content A");
        System.IO.File.WriteAllText("DirectoryA2/test2.txt", "Test content A2");

        var text = FileManager.FromFileText("DirectoryA2\\test.txt");
        var text2 = FileManager.FromFileText("DirectoryB2\\..\\DirectoryA2\\test2.txt");

        text.ShouldBe("Test content A");
        text2.ShouldBe("Test content A2");
    }

    // --- macOS .app bundle content resolution (issue #731) ---------------------------------------
    // In a macOS .app bundle the executable lives in <Bundle>.app/Contents/MacOS/ but loose content
    // ships in <Bundle>.app/Contents/Resources/. GetMacOSBundleResourcesPath is the pure path-math
    // seam that rebases an exe-relative absolute path onto the sibling Resources directory. It is OS-
    // and filesystem-agnostic (it takes the exe directory as a parameter), so these tests exercise the
    // real fix logic on every CI OS, not just macos-15. The end-to-end "launch from a real .app" check
    // lives in the macOS-only CI step.

    private static string Bundle(params string[] segments) =>
        Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, segments);

    [Fact]
    public void GetMacOSBundleResourcesPath_ShouldRebaseOntoResources_WhenExeIsInMacOSBundle()
    {
        string exeDirectory = Bundle("Apps", "MyGame.app", "Contents", "MacOS") + Path.DirectorySeparatorChar;
        string absolutePath = exeDirectory + Path.Combine("Content", "fonts", "test.fnt");

        string? result = FileManager.GetMacOSBundleResourcesPath(absolutePath, exeDirectory);

        result.ShouldBe(Bundle("Apps", "MyGame.app", "Contents", "Resources", "Content", "fonts", "test.fnt"));
    }

    [Fact]
    public void GetMacOSBundleResourcesPath_ShouldReturnNull_WhenExeIsNotInBundle()
    {
        string exeDirectory = Bundle("Apps", "MyGame") + Path.DirectorySeparatorChar;
        string absolutePath = exeDirectory + Path.Combine("Content", "fonts", "test.fnt");

        FileManager.GetMacOSBundleResourcesPath(absolutePath, exeDirectory).ShouldBeNull();
    }

    [Fact]
    public void GetMacOSBundleResourcesPath_ShouldReturnNull_WhenPathIsNotUnderExeDirectory()
    {
        string exeDirectory = Bundle("Apps", "MyGame.app", "Contents", "MacOS") + Path.DirectorySeparatorChar;
        string absolutePath = Bundle("SomewhereElse", "Content", "fonts", "test.fnt");

        FileManager.GetMacOSBundleResourcesPath(absolutePath, exeDirectory).ShouldBeNull();
    }

    [Fact]
    public void GetMacOSBundleResourcesPath_ShouldResolveRealFile_WhenContentShippedInResources()
    {
        // Build a real .app directory layout in a temp dir: content physically in Resources, nothing
        // under MacOS. This proves the rebased path points at the actual on-disk file.
        string bundleRoot = Path.Combine(Path.GetTempPath(), "GumBundleTest_" + Guid.NewGuid().ToString("N"));
        string exeDirectory = Path.Combine(bundleRoot, "MyGame.app", "Contents", "MacOS") + Path.DirectorySeparatorChar;
        string resourcesContentDirectory = Path.Combine(bundleRoot, "MyGame.app", "Contents", "Resources", "Content", "fonts");
        try
        {
            Directory.CreateDirectory(exeDirectory);
            Directory.CreateDirectory(resourcesContentDirectory);
            string realFile = Path.Combine(resourcesContentDirectory, "test.fnt");
            File.WriteAllText(realFile, "font data");

            string exeRelativePath = exeDirectory + Path.Combine("Content", "fonts", "test.fnt");
            File.Exists(exeRelativePath).ShouldBeFalse();

            string? rebased = FileManager.GetMacOSBundleResourcesPath(exeRelativePath, exeDirectory);

            rebased.ShouldNotBeNull();
            File.Exists(rebased).ShouldBeTrue();
            File.ReadAllText(rebased!).ShouldBe("font data");
        }
        finally
        {
            if (Directory.Exists(bundleRoot))
            {
                Directory.Delete(bundleRoot, recursive: true);
            }
        }
    }
}
