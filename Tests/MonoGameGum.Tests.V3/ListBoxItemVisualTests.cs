using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ListBoxItemVisualTests
{
    [Fact]
    public void ListBoxItem_Visual_HasEvents_IsTrue()
    {
        // Arrange & Act
        ListBoxItem sut = new();

        // Assert
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindTextInstance()
    {
        // UpdateToObject depends on coreText being found by RefreshInternalVisualReferences
        ListBoxItem listBoxItem = new();

        listBoxItem.UpdateToObject("test item");

        GraphicalUiElement? textInstance = listBoxItem.Visual.GetGraphicalUiElementByName("TextInstance");
        textInstance.ShouldNotBeNull();
        string? displayedText = (textInstance.RenderableComponent as RenderingLibrary.Graphics.IText)?.RawText;
        displayedText.ShouldBe("test item");
    }
}
