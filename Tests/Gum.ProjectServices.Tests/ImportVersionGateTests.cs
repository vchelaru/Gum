using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ImportVersionGateTests
{
    private readonly ImportVersionGate _sut = new ImportVersionGate();

    [Fact]
    public void GetRejection_ReturnsMessage_WhenSourceIsNewerThanDestination()
    {
        string? rejection = _sut.GetRejection(
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
        string? rejection = _sut.GetRejection(sourceVersion, destinationVersion, "The source project");

        rejection.ShouldBeNull();
    }
}
