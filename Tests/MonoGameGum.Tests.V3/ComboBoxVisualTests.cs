using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ComboBoxVisualTests
{
    [Fact]
    public void ComboBox_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ComboBox sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
