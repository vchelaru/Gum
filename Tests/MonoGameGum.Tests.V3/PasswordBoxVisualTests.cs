using Gum.Forms.Controls;
using Gum.Wireframe;
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

    [Fact]
    public void Password_ShouldDisplayMaskedCharacters()
    {
        // This verifies both TextBoxBase's RefreshInternalVisualReferences (finds textComponent)
        // and PasswordBox's RefreshInternalVisualReferences (calls UpdateDisplayedCharacters)
        PasswordBox passwordBox = new();

        passwordBox.Password = "secret";

        // The visual text should show mask characters, not the actual password
        GraphicalUiElement? textInstance = passwordBox.Visual.GetGraphicalUiElementByName("TextInstance");
        textInstance.ShouldNotBeNull();
        string? displayedText = (textInstance.RenderableComponent as RenderingLibrary.Graphics.IText)?.RawText;
        displayedText.ShouldBe("******");
    }

    [Fact]
    public void Password_ShouldStoreActualValue()
    {
        PasswordBox passwordBox = new();

        passwordBox.Password = "secret";

        passwordBox.Password.ShouldBe("secret");
    }

    [Fact]
    public void RefreshInternalVisualReferences_ShouldFindCaretInstance()
    {
        PasswordBox passwordBox = new();

        GraphicalUiElement? caret = passwordBox.Visual.GetGraphicalUiElementByName("CaretInstance");

        caret.ShouldNotBeNull();
    }
}
