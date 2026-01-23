using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class RadioButtonVisualTests
{
    [Fact]
    public void RadioButton_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        RadioButton sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
