using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class MenuItemVisualTests
{
    [Fact]
    public void MenuItem_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        MenuItem sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindTextInstance()
    {
        // Header get/set depends on coreText being found by RefreshInternalVisualReferences
        MenuItem menuItem = new();

        menuItem.Header = "File";

        menuItem.Header.ShouldBe("File");
    }
}
