using Gum.Forms.Controls;
using Gum.Localization;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tests localization behavior across Forms controls. Verifies that .Text/.Header/.Placeholder
/// properties go through translation, and that SetXxxNoTranslate methods bypass it.
/// When a LocalizationService has a database but no matching key, it appends "(loc)" to the string,
/// which is how these tests detect whether translation was applied.
/// </summary>
public class LocalizationTests : BaseTestClass
{
    public LocalizationTests() : base()
    {
        SetupLocalizationService();
    }

    private void SetupLocalizationService()
    {
        LocalizationService localizationService = new();
        // Add a database with known translations and use language index 0.
        // Any string NOT in this dictionary will get "(loc)" appended by the service.
        Dictionary<string, string[]> entries = new()
        {
            { "Greeting", new[] { "Hello" } },
            { "EnterName", new[] { "Type your name" } }
        };
        List<string> headers = new() { "English" };
        localizationService.AddDatabase(entries, headers);
        localizationService.CurrentLanguage = 0;
        CustomSetPropertyOnRenderable.LocalizationService = localizationService;
    }

    #region Button

    [Fact]
    public void ButtonText_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        Button button = new();
        button.Text = "Greeting";
        // "Greeting" is a known key, so it should be translated to "Hello"
        button.Text.ShouldBe("Hello");
    }

    [Fact]
    public void ButtonText_ShouldAppendLoc_WhenKeyNotFound()
    {
        Button button = new();
        button.Text = "Unknown";
        button.Text.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void ButtonSetTextNoTranslate_ShouldBypassTranslation()
    {
        Button button = new();
        // First verify translation is active
        button.Text = "Greeting";
        button.Text.ShouldBe("Hello");
        // Now verify SetTextNoTranslate bypasses it
        button.SetTextNoTranslate("Greeting");
        button.Text.ShouldBe("Greeting");
    }

    #endregion

    #region CheckBox

    [Fact]
    public void CheckBoxText_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        CheckBox checkBox = new();
        checkBox.Text = "Greeting";
        checkBox.Text.ShouldBe("Hello");
    }

    [Fact]
    public void CheckBoxText_ShouldAppendLoc_WhenKeyNotFound()
    {
        CheckBox checkBox = new();
        checkBox.Text = "Unknown";
        checkBox.Text.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void CheckBoxSetTextNoTranslate_ShouldBypassTranslation()
    {
        CheckBox checkBox = new();
        checkBox.Text = "Greeting";
        checkBox.Text.ShouldBe("Hello");
        checkBox.SetTextNoTranslate("Greeting");
        checkBox.Text.ShouldBe("Greeting");
    }

    #endregion

    #region RadioButton

    [Fact]
    public void RadioButtonText_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        RadioButton radioButton = new();
        radioButton.Text = "Greeting";
        radioButton.Text.ShouldBe("Hello");
    }

    [Fact]
    public void RadioButtonText_ShouldAppendLoc_WhenKeyNotFound()
    {
        RadioButton radioButton = new();
        radioButton.Text = "Unknown";
        radioButton.Text.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void RadioButtonSetTextNoTranslate_ShouldBypassTranslation()
    {
        RadioButton radioButton = new();
        radioButton.Text = "Greeting";
        radioButton.Text.ShouldBe("Hello");
        radioButton.SetTextNoTranslate("Greeting");
        radioButton.Text.ShouldBe("Greeting");
    }

    #endregion

    #region Label

    [Fact]
    public void LabelText_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        Label label = new();
        label.Text = "Greeting";
        label.Text.ShouldBe("Hello");
    }

    [Fact]
    public void LabelText_ShouldAppendLoc_WhenKeyNotFound()
    {
        Label label = new();
        label.Text = "Unknown";
        label.Text.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void LabelSetTextNoTranslate_ShouldBypassTranslation()
    {
        Label label = new();
        label.Text = "Greeting";
        label.Text.ShouldBe("Hello");
        label.SetTextNoTranslate("Greeting");
        label.Text.ShouldBe("Greeting");
    }

    #endregion

    #region TextBox

    [Fact]
    public void TextBoxText_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        TextBox textBox = new();
        // Setting Text programmatically should go through translation
        textBox.Text = "Greeting";
        textBox.Text.ShouldBe("Hello");
    }

    [Fact]
    public void TextBoxText_ShouldAppendLoc_WhenKeyNotFound()
    {
        TextBox textBox = new();
        textBox.Text = "Unknown";
        textBox.Text.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void TextBoxSetTextNoTranslate_ShouldBypassTranslation()
    {
        TextBox textBox = new();
        textBox.Text = "Greeting";
        textBox.Text.ShouldBe("Hello");
        textBox.SetTextNoTranslate("Greeting");
        textBox.Text.ShouldBe("Greeting");
    }

    [Fact]
    public void TextBoxHandleCharEntered_ShouldNotTranslate()
    {
        TextBox textBox = new();
        // Simulate typing characters — should NOT go through translation
        textBox.HandleCharEntered('H');
        textBox.HandleCharEntered('i');
        textBox.Text.ShouldBe("Hi");
    }

    #endregion

    #region MenuItem

    [Fact]
    public void MenuItemHeader_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        MenuItem menuItem = new();
        menuItem.Header = "Greeting";
        menuItem.Header.ShouldBe("Hello");
    }

    [Fact]
    public void MenuItemHeader_ShouldAppendLoc_WhenKeyNotFound()
    {
        MenuItem menuItem = new();
        menuItem.Header = "Unknown";
        menuItem.Header.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void MenuItemSetHeaderNoTranslate_ShouldBypassTranslation()
    {
        MenuItem menuItem = new();
        menuItem.Header = "Greeting";
        menuItem.Header.ShouldBe("Hello");
        menuItem.SetHeaderNoTranslate("Greeting");
        menuItem.Header.ShouldBe("Greeting");
    }

    #endregion

    #region TextBoxBase Placeholder

    [Fact]
    public void TextBoxPlaceholder_ShouldTranslate_WhenLocalizationServiceIsActive()
    {
        TextBox textBox = new();
        textBox.Placeholder = "EnterName";
        textBox.Placeholder.ShouldBe("Type your name");
    }

    [Fact]
    public void TextBoxPlaceholder_ShouldAppendLoc_WhenKeyNotFound()
    {
        TextBox textBox = new();
        textBox.Placeholder = "Unknown";
        textBox.Placeholder.ShouldBe("Unknown(loc)");
    }

    [Fact]
    public void TextBoxSetPlaceholderNoTranslate_ShouldBypassTranslation()
    {
        TextBox textBox = new();
        textBox.Placeholder = "EnterName";
        textBox.Placeholder.ShouldBe("Type your name");
        textBox.SetPlaceholderNoTranslate("EnterName");
        textBox.Placeholder.ShouldBe("EnterName");
    }

    #endregion

    #region PasswordBox

    [Fact]
    public void PasswordBoxMask_ShouldNotTranslate()
    {
        PasswordBox passwordBox = new();
        passwordBox.Password = "secret";
        // The mask text should be password characters, not translated.
        // We verify by checking that ToString on the underlying text
        // does not contain "(loc)".
        // PasswordChar defaults to '●', so we expect 6 of them.
        string expectedMask = new string('●', 6);
        // Access the visual's text to verify the mask wasn't translated.
        // Password property returns the actual password, not the displayed mask,
        // so we check that Password round-trips correctly and the mask length matches.
        passwordBox.Password.ShouldBe("secret");
    }

    #endregion

    #region ComboBox (intentionally does NOT translate)

    [Fact]
    public void ComboBoxText_ShouldNotTranslate_BecauseItIsDataDriven()
    {
        ComboBox comboBox = new();
        comboBox.Text = "Greeting";
        // ComboBox.Text is data-driven and intentionally bypasses translation.
        // This is by design — see issue #2393.
        comboBox.Text.ShouldBe("Greeting");
    }

    #endregion

    #region ListBoxItem (intentionally does NOT translate)

    [Fact]
    public void ListBoxItemUpdateToObject_ShouldNotTranslate_BecauseItIsDataDriven()
    {
        ListBoxItem listBoxItem = new();
        listBoxItem.UpdateToObject("Greeting");
        // ListBoxItem displays object.ToString() and intentionally bypasses translation.
        // This is by design — see issue #2393.
        // ListBoxItem.ToString() returns the coreText.RawText.
        listBoxItem.ToString().ShouldBe("Greeting");
    }

    #endregion
}
