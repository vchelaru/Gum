using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class UnattendedWindowTests
{
    [Fact]
    public void StartPosition_WhenUnattended_IsFarOutsideEveryMonitor()
    {
        (int X, int Y)? position = UnattendedWindow.StartPosition(isUnattended: true);

        position.ShouldNotBeNull();
        position.Value.X.ShouldBeLessThanOrEqualTo(-32000);
        position.Value.Y.ShouldBeLessThanOrEqualTo(-32000);
    }

    [Fact]
    public void StartPosition_WhenAttended_LeavesPlacementToSdl()
    {
        UnattendedWindow.StartPosition(isUnattended: false).ShouldBeNull();
    }
}
