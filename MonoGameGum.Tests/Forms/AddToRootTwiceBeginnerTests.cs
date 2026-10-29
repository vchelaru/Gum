using System.Linq;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A beginner calls AddToRoot on the same control twice, e.g. a ShowScreen that runs again.
/// </summary>
public class AddToRootTwiceBeginnerTests : BeginnerDriverTestBase
{
    [Fact]
    public void AddToRoot_ShouldAddOnce_WhenCalledTwiceOnTheSameControl()
    {
        Button button = new();

        button.AddToRoot();
        button.AddToRoot();

        Gum.GumService.Default.Root.Children!.Count(c => c == button.Visual).ShouldBe(1);
    }
}
