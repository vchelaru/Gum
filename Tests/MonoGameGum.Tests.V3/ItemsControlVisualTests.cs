using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ItemsControlVisualTests
{
    [Fact]
    public void ItemsControl_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ItemsControl sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
