using System.IO;
using System.Reflection;
using Gum.Managers;
using Gum.Plugins;
using Moq;
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
}
