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
// appears - no crash, no message, nothing in the Output tab. These pin that the drop is reported.
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

        ComposablePartCatalog? catalog = factory.CreateCatalogForLoadableTypes("MyPlugin.dll", exception);

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

        ComposablePartCatalog? catalog = factory.CreateCatalogForLoadableTypes("MyPlugin.dll", exception);

        catalog.ShouldBeNull();
        outputManager.Verify(m => m.AddError(It.Is<string>(v => v.Contains("bad type"))), Times.Once);
    }
}
