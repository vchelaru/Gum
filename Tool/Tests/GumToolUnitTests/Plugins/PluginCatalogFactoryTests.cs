using Gum.Managers;
using Gum.Plugins;
using Moq;
using Shouldly;
using System;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Reflection;
using Xunit;

namespace GumToolUnitTests.Plugins;

// A plugin whose types fail to reflection-load is dropped from the catalog and simply never
// appears - no crash, no message, nothing in the Output tab. These pin that the drop is reported,
// and that the two kinds of expected noise in a plugin folder stay out of the error log.
public class PluginCatalogFactoryTests
{
    [Fact]
    public void FindMismatchedDuplicates_FlagsOnlySameNamedFilesWhoseContentDiffers()
    {
        // A dependency each plugin ships in its own folder is the same bytes twice: expected. A stale
        // copy elsewhere with the same name but other bytes is the case that ends in a
        // TypeLoadException deep in an unrelated plugin (#4693).
        Mock<IOutputManager> outputManager = new();
        PluginCatalogFactory factory = new(outputManager.Object);
        (string Path, string ContentHash)[] files =
        {
            (@"C:\Plugins\Only.dll", "aaa"),
            (@"C:\Plugins\A\Shared.dll", "bbb"),
            (@"C:\Plugins\B\Shared.dll", "bbb"),
            (@"C:\Plugins\Stale.dll", "ccc"),
            (@"C:\Plugins\A\stale.DLL", "ddd"),
            // Per-platform native libraries share a name by design.
            (@"C:\Plugins\A\runtimes\win-x64\native\Native.dll", "eee"),
            (@"C:\Plugins\A\runtimes\win-x86\native\Native.dll", "fff"),
        };

        IReadOnlyList<PluginFileDuplicates> mismatched = factory.FindMismatchedDuplicates(files);

        PluginFileDuplicates single = mismatched.ShouldHaveSingleItem();
        single.FileName.ShouldBe("Stale.dll");
        single.Paths.ShouldBe(new[] { @"C:\Plugins\Stale.dll", @"C:\Plugins\A\stale.DLL" });
    }

    [Fact]
    public void ReportMismatchedDuplicates_NamesBothCopies_AndTheOneThatLoads()
    {
        string folder = CreateTempPluginFolder();
        try
        {
            string rootCopy = Path.Combine(folder, "Dependency.dll");
            string pluginCopy = Path.Combine(folder, "MyPlugin", "Dependency.dll");
            File.WriteAllBytes(rootCopy, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(pluginCopy, new byte[] { 4, 5, 6 });
            string reported = "";
            Mock<IOutputManager> outputManager = new();
            outputManager.Setup(m => m.AddError(It.IsAny<string>())).Callback<string>(v => reported = v);
            PluginCatalogFactory factory = new(outputManager.Object);

            factory.ReportMismatchedDuplicates(new[] { rootCopy, pluginCopy });

            outputManager.Verify(m => m.AddError(It.IsAny<string>()), Times.Once);
            reported.ShouldContain("Dependency.dll");
            reported.ShouldContain(rootCopy);
            reported.ShouldContain(pluginCopy);
            reported.IndexOf(rootCopy, StringComparison.Ordinal).ShouldBeLessThan(reported.IndexOf(pluginCopy, StringComparison.Ordinal), "the first copy found is the one that loads");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void ReportMismatchedDuplicates_StaysQuiet_ForIdenticalCopies()
    {
        string folder = CreateTempPluginFolder();
        try
        {
            string firstCopy = Path.Combine(folder, "MyPlugin", "Dependency.dll");
            string secondCopy = Path.Combine(folder, "OtherPlugin", "Dependency.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(secondCopy)!);
            File.WriteAllBytes(firstCopy, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(secondCopy, new byte[] { 1, 2, 3 });
            Mock<IOutputManager> outputManager = new();
            PluginCatalogFactory factory = new(outputManager.Object);

            factory.ReportMismatchedDuplicates(new[] { firstCopy, secondCopy });

            outputManager.Verify(m => m.AddError(It.IsAny<string>()), Times.Never);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static string CreateTempPluginFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumPluginScan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "MyPlugin"));
        return folder;
    }

    [Fact]
    public void CreateCatalogForLoadableTypes_ReportsSkippedTypesAndDistinctLoaderErrors()
    {
        string reported = "";
        Mock<IOutputManager> outputManager = new();
        outputManager.Setup(m => m.AddError(It.IsAny<string>())).Callback<string>(v => reported = v);
        ReflectionTypeLoadException exception = new(
            [typeof(string), null, null],
            [
                new FileNotFoundException("Could not load file or assembly 'Missing.Dependency'."),
                new FileNotFoundException("Could not load file or assembly 'Missing.Dependency'."),
            ]);
        PluginCatalogFactory factory = new(outputManager.Object);

        ComposablePartCatalog? catalog = factory.CreateCatalogForLoadableTypes(
            "MyPlugin.dll", exception, couldContainPlugins: true);

        catalog.ShouldNotBeNull();
        outputManager.Verify(m => m.AddError(It.IsAny<string>()), Times.Once);
        reported.ShouldContain("MyPlugin.dll");
        reported.ShouldContain("2 of its 3 types");
        // Deduplicated: one line, not one per failed type.
        reported.Split("Could not load file or assembly 'Missing.Dependency'.").Length.ShouldBe(2);
    }

    [Fact]
    public void CreateCatalogForLoadableTypes_ReturnsNull_WhenNoTypeLoaded()
    {
        Mock<IOutputManager> outputManager = new();
        ReflectionTypeLoadException exception = new(
            [null],
            [new TypeLoadException("bad type")]);
        PluginCatalogFactory factory = new(outputManager.Object);

        ComposablePartCatalog? catalog = factory.CreateCatalogForLoadableTypes(
            "MyPlugin.dll", exception, couldContainPlugins: true);

        catalog.ShouldBeNull();
        outputManager.Verify(m => m.AddError(It.Is<string>(v => v.Contains("bad type"))), Times.Once);
    }

    // A plugin's dependencies sit in the same folder and get scanned too. Vortice.Direct3D12 is the
    // standing example: two of its types can never reflection-load, and it holds no plugins, so the
    // skipped types are not something the user can act on and are not worth a line.
    [Fact]
    public void CreateCatalogForLoadableTypes_ReportsNothing_WhenAssemblyCannotContainPlugins()
    {
        Mock<IOutputManager> outputManager = new();
        ReflectionTypeLoadException exception = new(
            [typeof(string), null],
            [new TypeLoadException("Could not load type 'Union'.")]);
        PluginCatalogFactory factory = new(outputManager.Object);

        ComposablePartCatalog? catalog = factory.CreateCatalogForLoadableTypes(
            "Vortice.Direct3D12", exception, couldContainPlugins: false);

        // Still catalogued - staying quiet must not mean dropping the types that did load.
        catalog.ShouldNotBeNull();
        outputManager.Verify(m => m.AddError(It.IsAny<string>()), Times.Never);
        outputManager.Verify(m => m.AddOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CouldContainPlugins_IsFalse_ForAssemblyNotReferencingThePluginBase()
    {
        PluginCatalogFactory factory = new(Mock.Of<IOutputManager>());

        // The test assembly references Gum, so it passes the same check a real plugin dll does.
        factory.CouldContainPlugins(typeof(PluginCatalogFactoryTests).Assembly).ShouldBeTrue();
        factory.CouldContainPlugins(typeof(string).Assembly).ShouldBeFalse();
    }

    // The plugin folder is scanned recursively, so it also turns up the native DLLs a plugin ships
    // in runtimes/<rid>/native. Those aren't managed assemblies and never will be - not an error.
    [Fact]
    public void CreateCatalogForFile_ReturnsNullWithoutError_ForNonManagedDll()
    {
        string nativeDllPath = Path.Combine(Path.GetTempPath(), $"gum-test-native-{Guid.NewGuid():N}.dll");
        File.WriteAllBytes(nativeDllPath, [0x4D, 0x5A, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04]);
        Mock<IOutputManager> outputManager = new();
        PluginCatalogFactory factory = new(outputManager.Object);

        try
        {
            ComposablePartCatalog? catalog = factory.CreateCatalogForFile(nativeDllPath);

            catalog.ShouldBeNull();
            outputManager.Verify(m => m.AddError(It.IsAny<string>()), Times.Never);
            factory.Scans.ShouldHaveSingleItem().Outcome.ShouldBe(PluginFileOutcome.NotManagedAssembly);
        }
        finally
        {
            File.Delete(nativeDllPath);
        }
    }

    // A truncated or corrupted plugin dll also throws BadImageFormatException. Treating that as
    // "native, nothing to see" would silently drop a real plugin - the failure this all exists for.
    [Fact]
    public void IsManagedAssembly_TellsACorruptAssemblyApartFromANativeDll()
    {
        string nativeDllPath = Path.Combine(Path.GetTempPath(), $"gum-test-native-{Guid.NewGuid():N}.dll");
        File.WriteAllBytes(nativeDllPath, [0x4D, 0x5A, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04]);

        try
        {
            PluginCatalogFactory.IsManagedAssembly(nativeDllPath).ShouldBeFalse();
            PluginCatalogFactory.IsManagedAssembly(typeof(PluginCatalogFactoryTests).Assembly.Location)
                .ShouldBeTrue();
        }
        finally
        {
            File.Delete(nativeDllPath);
        }
    }

    [Fact]
    public void CreateCatalogForFile_ReportsError_WhenFileIsMissing()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"gum-test-missing-{Guid.NewGuid():N}.dll");
        Mock<IOutputManager> outputManager = new();
        PluginCatalogFactory factory = new(outputManager.Object);

        ComposablePartCatalog? catalog = factory.CreateCatalogForFile(missingPath);

        catalog.ShouldBeNull();
        outputManager.Verify(m => m.AddError(It.Is<string>(v => v.Contains(missingPath))), Times.Once);
    }
}
