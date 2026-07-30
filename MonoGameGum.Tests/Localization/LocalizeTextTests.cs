using Gum.Localization;
using Gum.Wireframe;
using Gum.GueDeriving;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Tests <see cref="TextRuntime.LocalizeText"/>, the per-instance opt-out from translation -
/// distinct from <see cref="TextRuntime.SetTextNoTranslate"/>, which bypasses translation for a
/// single assignment rather than persisting on the instance.
/// </summary>
public class LocalizeTextTests : BaseTestClass
{
    private LocalizationService _localizationService = null!;

    public LocalizeTextTests() : base()
    {
        _localizationService = new LocalizationService();
        Dictionary<string, string[]> entries = new()
        {
            { "Greeting", new[] { "Hello", "Hola" } },
        };
        List<string> headers = new() { "English", "Spanish" };
        _localizationService.AddDatabase(entries, headers);
        _localizationService.CurrentLanguage = 0;
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;
    }

    [Fact]
    public void LocalizeText_ShouldDefaultToTrue()
    {
        TextRuntime text = new();

        text.LocalizeText.ShouldBeTrue();
    }

    [Fact]
    public void SettingLocalizeTextToFalse_ShouldUntranslateAlreadyAssignedText()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.Text = "Greeting";
        text.Text.ShouldBe("Hello");

        text.LocalizeText = false;

        text.Text.ShouldBe("Greeting");
    }

    [Fact]
    public void SettingLocalizeTextToTrue_ShouldRetranslateText_AfterHavingBeenFalse()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.LocalizeText = false;
        text.Text = "Greeting";
        text.Text.ShouldBe("Greeting");

        text.LocalizeText = true;

        text.Text.ShouldBe("Hello");
    }

    [Fact]
    public void Text_ShouldNotBeTranslated_WhenLocalizeTextIsFalse()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.LocalizeText = false;

        text.Text = "Greeting";

        text.Text.ShouldBe("Greeting");
    }

    [Fact]
    public void SetPropertyLocalizeText_ShouldSetLocalizeTextProperty()
    {
        TextRuntime text = new();
        text.AddToRoot();

        text.SetProperty("LocalizeText", false);

        text.LocalizeText.ShouldBeFalse();
    }

    [Fact]
    public void RefreshLocalization_ShouldNotTranslate_WhenLocalizeTextIsFalse()
    {
        TextRuntime text = new();
        text.AddToRoot();
        text.LocalizeText = false;
        text.Text = "Greeting";

        _localizationService.CurrentLanguage = 1;
        GumService.Default.RefreshLocalization();

        text.Text.ShouldBe("Greeting");
    }
}
