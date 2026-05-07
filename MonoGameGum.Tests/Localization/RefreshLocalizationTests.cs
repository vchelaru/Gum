using Gum.Forms.Controls;
using Gum.Localization;
using Gum.Wireframe;
using Gum.GueDeriving;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Tests runtime language switching: when CurrentLanguage changes, already-instantiated
/// text-bearing visuals should re-translate without the caller having to recreate them.
/// </summary>
public class RefreshLocalizationTests : BaseTestClass
{
    private LocalizationService _localizationService = null!;

    public RefreshLocalizationTests() : base()
    {
        _localizationService = new LocalizationService();
        Dictionary<string, string[]> entries = new()
        {
            { "Greeting", new[] { "Hello", "Hola", "Bonjour" } },
            { "Farewell", new[] { "Goodbye", "Adios", "Au revoir" } },
            { "BBStr",    new[] { "plain English", "[Color=Red]Spanish red[/Color]", "French" } },
        };
        List<string> headers = new() { "English", "Spanish", "French" };
        _localizationService.AddDatabase(entries, headers);
        _localizationService.CurrentLanguage = 0;
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;
    }

    [Fact]
    public void RefreshLocalization_ShouldRetranslateLiveText_WhenLanguageChanges()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.Text = "Greeting";
        text.Text.ShouldBe("Hello");

        _localizationService.CurrentLanguage = 1;
        GumService.Default.RefreshLocalization();

        text.Text.ShouldBe("Hola");
    }

    [Fact]
    public void RefreshLocalization_ShouldRetranslateButton()
    {
        Button button = new();
        button.Visual.AddToRoot();
        button.Text = "Greeting";
        button.Text.ShouldBe("Hello");

        _localizationService.CurrentLanguage = 2;
        GumService.Default.RefreshLocalization();

        button.Text.ShouldBe("Bonjour");
    }

    [Fact]
    public void RefreshLocalization_ShouldNotTouchTextNoTranslate_WhenLanguageChanges()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.SetTextNoTranslate("Greeting");
        text.Text.ShouldBe("Greeting");

        _localizationService.CurrentLanguage = 1;
        GumService.Default.RefreshLocalization();

        text.Text.ShouldBe("Greeting");
    }

    [Fact]
    public void RefreshLocalization_ShouldNotRetranslateUserTypedTextBoxInput()
    {
        TextBox textBox = new();
        textBox.Visual.AddToRoot();
        textBox.HandleCharEntered('H');
        textBox.HandleCharEntered('i');
        textBox.Text.ShouldBe("Hi");

        _localizationService.CurrentLanguage = 1;
        GumService.Default.RefreshLocalization();

        textBox.Text.ShouldBe("Hi");
    }

    [Fact]
    public void RefreshLocalization_ShouldHandleBBCodeProducedByTranslation()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.Text = "BBStr";
        text.Text.ShouldBe("plain English");

        _localizationService.CurrentLanguage = 1;
        GumService.Default.RefreshLocalization();

        // Spanish entry contains BBCode markup, which should be parsed on refresh.
        // We verify by checking that StoredMarkupText was populated.
        var renderable = (RenderingLibrary.Graphics.Text)text.RenderableComponent;
        renderable.StoredMarkupText.ShouldBe("[Color=Red]Spanish red[/Color]");
    }

    [Fact]
    public void CurrentLanguageChanged_ShouldFireRefresh_Automatically()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.Text = "Greeting";
        text.Text.ShouldBe("Hello");

        // No explicit RefreshLocalization() call — assignment alone should refresh.
        _localizationService.CurrentLanguage = 2;

        text.Text.ShouldBe("Bonjour");
    }

    [Fact]
    public void RefreshLocalization_ShouldBeNoOp_WhenLocalizationServiceIsNull()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.Text = "Greeting";

        CustomSetPropertyOnRenderable.LocalizationService = null;

        Should.NotThrow(() => GumService.Default.RefreshLocalization());
    }

    [Fact]
    public void RefreshLocalization_ShouldRefresh_PopupRootAndModalRoot()
    {
        TextRuntime popupText = new();
        TextRuntime modalText = new();
        GumService.Default.PopupRoot.Children.Add(popupText);
        GumService.Default.ModalRoot.Children.Add(modalText);
        popupText.Text = "Greeting";
        modalText.Text = "Farewell";

        _localizationService.CurrentLanguage = 1;
        GumService.Default.RefreshLocalization();

        popupText.Text.ShouldBe("Hola");
        modalText.Text.ShouldBe("Adios");
    }
}
