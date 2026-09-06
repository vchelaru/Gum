using Shouldly;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        CultureInfo.CurrentCulture = _previousCulture;
    }

    // GetExtension read the final dot with the ordinal char overload but located the separators with
    // LastIndexOf(string), which compares by the current culture. Where that reported a separator
    // past the dot, every path returned no extension at all - which silently emptied every
    // extension-filtered file search, including the one that finds plugins and the one a game uses
    // to resolve a content file whose extension was not given.
    [Theory]
    // th-TH is the culture this was reported under: a Thai user saw no plugins at all, because
    // every path parsed as having no extension.
    [InlineData("th-TH")]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("")]
    public void GetExtension_ReadsTheExtension_WhateverTheCurrentCulture(string cultureName)
    {
        CultureInfo.CurrentCulture = new CultureInfo(cultureName);

        FileManager.GetExtension(@"C:\Gum Tool\Test\Plugins\CodeOutputPlugin\CodeOutputPlugin.dll")
            .ShouldBe("dll");
        FileManager.GetExtension("C:/Gum Tool/Test/Plugins/CodeOutputPlugin/CodeOutputPlugin.dll")
            .ShouldBe("dll");
        // A dot in a folder name, with no extension on the file itself, still has none.
        FileManager.GetExtension(@"C:\folder.with.dots\FileWithNoExtension").ShouldBe("");
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
    public void ClearDirectoryContents_ShouldDeleteFilesAndSubdirectories_ButKeepDirectoryItself()
    {
        string root = Path.Combine(Path.GetTempPath(), "GumClearDirectoryContentsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string subDirectory = Path.Combine(root, "SubDirectory");
        Directory.CreateDirectory(subDirectory);
        File.WriteAllText(Path.Combine(root, "file.txt"), "contents");
        File.WriteAllText(Path.Combine(subDirectory, "nested.txt"), "contents");

        try
        {
            FileManager.ClearDirectoryContents(root);

            Directory.Exists(root).ShouldBeTrue();
            Directory.GetFileSystemEntries(root).ShouldBeEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void ClearDirectoryContents_ShouldNotThrow_WhenDirectoryDoesNotExist()
    {
        string missingDirectory = Path.Combine(Path.GetTempPath(), "GumClearDirectoryContentsTests_Missing_" + Guid.NewGuid().ToString("N"));

        Should.NotThrow(() => FileManager.ClearDirectoryContents(missingDirectory));
    }

    [Fact]
    public void GetAllFilesInDirectory_ShouldPreserveCase_WhenNoFileTypeSpecified()
    {
        // Regression for #4481: the no-fileType branch standardized each result through
        // Standardize(files[i]) (default preserveCase: false), lowercasing every enumerated
        // filename even though Directory.GetFiles returns the real on-disk casing.
        string directory = Path.Combine(Path.GetTempPath(), "GumGetAllFilesInDirectoryTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "MyFile.txt"), "contents");

            List<string> files = FileManager.GetAllFilesInDirectory(directory, null);

            files.ShouldContain(f => Path.GetFileName(f) == "MyFile.txt");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FindAndAddExtension_ShouldPreserveCase_WhenMatchFound()
    {
        // Regression for #4481: FindAndAddExtension lowercased its input fileName via
        // Standardize(fileName) (default preserveCase: false) before comparing it against
        // case-preserved entries from GetAllFilesInDirectory, so a mixed-case lookup mismatched
        // the actual on-disk file case.
        string directory = Path.Combine(Path.GetTempPath(), "GumFindAndAddExtensionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string realFile = Path.Combine(directory, "MyFile.txt");
            File.WriteAllText(realFile, "contents");

            string result = FileManager.FindAndAddExtension(Path.Combine(directory, "MyFile"));

            Path.GetFileName(result).ShouldBe("MyFile.txt");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
