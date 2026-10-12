using Gum.Plugins.ImportPlugin.Services;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

public class ImportVersionGateTests
{
    [Fact]
    public void GetRejection_ReturnsMessage_WhenSourceIsNewerThanDestination()
    {
        string? rejection = ImportVersionGate.GetRejection(
            sourceVersion: 5, destinationVersion: 3, sourceDescription: "The source project");

        rejection.ShouldNotBeNull();
        rejection.ShouldContain("The source project");
        rejection.ShouldContain("5");
        rejection.ShouldContain("3");
        rejection.ShouldContain("docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(3, 5)]
    public void GetRejection_ReturnsNull_WhenSourceIsNotNewerThanDestination(int sourceVersion, int destinationVersion)
    {
        string? rejection = ImportVersionGate.GetRejection(sourceVersion, destinationVersion, "The source project");

        rejection.ShouldBeNull();
    }
}
