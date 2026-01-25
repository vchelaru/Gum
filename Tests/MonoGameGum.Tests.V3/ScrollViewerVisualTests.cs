using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ScrollViewerVisualTests
{
    [Fact]
    public void ScrollViewer_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ScrollViewer sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
