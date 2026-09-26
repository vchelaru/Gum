using System.IO;
using System.Reflection;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using PluginHostFixture;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins;

/// <summary>
/// A plugin file the head refuses (a third-party plugin built against the WPF tool) is skipped with
/// an Output-tab error naming the file and the reason, and is listed in the Manage Plugins report.
/// </summary>
public class PluginCatalogFactoryHostTests
{
    [Fact]
    public void CreateCatalogForFile_AssemblyTheHeadRefuses_IsSkippedWithAClearError()
    {
        string dllPath = typeof(PluginCatalogFactoryHostTests).Assembly.Location;
        string reason = "references PresentationFramework, which only exists on Windows; this plugin needs an Avalonia build";
        Mock<IPluginHostConfiguration> host = new Mock<IPluginHostConfiguration>();
        host.Setup(x => x.CanHostExternalAssembly(It.IsAny<Assembly>(), out reason)).Returns(false);
        Mock<IOutputManager> output = new Mock<IOutputManager>();
        PluginCatalogFactory factory = new PluginCatalogFactory(output.Object);

        factory.CreateCatalogForFile(dllPath, host.Object).ShouldBeNull();

        output.Verify(x => x.AddError(It.Is<string>(message =>
            message.Contains(Path.GetFileName(dllPath)) && message.Contains(reason))), Times.Once);
        PluginFileScan scan = factory.Scans.ShouldHaveSingleItem();
        scan.Outcome.ShouldBe(PluginFileOutcome.NotHostable);
    }

    [Fact]
    public void CreateCatalogForFile_TypeMissingFromTheHostsOwnAssembly_ReportsThePluginNeedsARebuild()
    {
        // Backstop for a plugin the head's up-front check let through: its base type is missing
        // from one of the tool's own assemblies, so it was built against another build of the tool.
        string directory = Path.Combine(Path.GetTempPath(), "GumPluginHostTests", Guid.NewGuid().ToString("N"));
        string toolAssemblyName = "FakeTool" + Guid.NewGuid().ToString("N");
        Assembly toolAssembly = Assembly.LoadFrom(PluginAssemblyWriter.WriteEmpty(directory, toolAssemblyName));
        string pluginPath = PluginAssemblyWriter.WriteDerivingFrom(directory, "StalePlugin" + Guid.NewGuid().ToString("N"),
            toolAssemblyName, "Gum.Plugins.BaseClasses", "PriorityPlugin", typeof(PluginBase).Assembly.GetName().Name!);
        string? reason = null;
        Mock<IPluginHostConfiguration> host = new Mock<IPluginHostConfiguration>();
        host.Setup(x => x.CanHostExternalAssembly(It.IsAny<Assembly>(), out reason)).Returns(true);
        host.Setup(x => x.InternalPluginAssemblies).Returns(new[] { toolAssembly });
        Mock<IOutputManager> output = new Mock<IOutputManager>();
        PluginCatalogFactory factory = new PluginCatalogFactory(output.Object);

        factory.CreateCatalogForFile(pluginPath, host.Object);

        output.Verify(x => x.AddError(It.Is<string>(message =>
            message.Contains("Gum.Plugins.BaseClasses.PriorityPlugin") &&
            message.Contains("built against a different build of the Gum tool"))), Times.Once);
    }
}
