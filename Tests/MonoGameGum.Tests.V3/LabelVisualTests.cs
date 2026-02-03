using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class LabelVisualTests
{
    [Fact]
    public void Label_Visual_HasEvents_IsFalse()
    {
        // Arrange & Act
        Label sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeFalse();
    }
}
