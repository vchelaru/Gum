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
    // skipped types are not something the user can act on.
    [Fact]
    public void CreateCatalogForLoadableTypes_ReportsAsOutput_WhenAssemblyCannotContainPlugins()
    {
        Mock<IOutputManager> outputManager = new();
        ReflectionTypeLoadException exception = new(
            [typeof(string), null],
            [new TypeLoadException("Could not load type 'Union'.")]);
        PluginCatalogFactory factory = new(outputManager.Object);

        factory.CreateCatalogForLoadableTypes("Vortice.Direct3D12", exception, couldContainPlugins: false);

        outputManager.Verify(m => m.AddError(It.IsAny<string>()), Times.Never);
        outputManager.Verify(m => m.AddOutput(It.Is<string>(v => v.Contains("Vortice.Direct3D12"))), Times.Once);
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
