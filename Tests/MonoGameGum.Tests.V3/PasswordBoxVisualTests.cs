using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class PasswordBoxVisualTests
{
    [Fact]
    public void PasswordBox_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        PasswordBox sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }
}
