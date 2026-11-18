using Gum.Forms.Controls;
using MonoGameGum.Tests.Forms;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// A file-loaded control changes state through ApplyState's variable path (its states have no Apply delegate),
/// which runs on every hover, press and enable change in a game, so it must not allocate once warm (#8218).
/// </summary>
public class FileStateApplyAllocationTests : FileThemeInputTestBase
{
    [Fact]
    public void ButtonIsEnabledToggle_FileLoadedButton_AllocatesNothing()
    {
        Button button = PlaceAlone<Button>("", "/ButtonStandard");
        bool enabled = true;

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                enabled = !enabled;
                button.IsEnabled = enabled;
            },
            warmupIterations: 50,
            measuredIterations: 300);

        result.BytesPerIteration.ShouldBe(0);
    }
}
