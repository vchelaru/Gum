using Gum.Localization;
using Gum.Wireframe;
using Shouldly;
using SkiaGum;
using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;

namespace SkiaGum.Tests.Localization;

/// <summary>
/// Tests <see cref="TextRuntime.LocalizeText"/> on SkiaGum's own <see cref="CustomSetPropertyOnRenderable"/>
/// copy - the per-instance opt-out from translation, distinct from <see cref="TextRuntime.SetTextNoTranslate"/>,
/// which bypasses translation for a single assignment rather than persisting on the instance.
/// </summary>
public class LocalizeTextTests : IDisposable
{
    private readonly LocalizationService _localizationService;

    public LocalizeTextTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;

        _localizationService = new LocalizationService();
        Dictionary<string, string[]> entries = new()
        {
            { "Greeting", new[] { "Hello", "Hola" } },
        };
        List<string> headers = new() { "English", "Spanish" };
        _localizationService.AddDatabase(entries, headers);
        _localizationService.CurrentLanguage = 1;
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.LocalizationService = null;
    }

    [Fact]
    public void Text_ShouldNotBeTranslated_WhenLocalizeTextIsFalse()
    {
        TextRuntime sut = new();
        sut.LocalizeText = false;

        sut.Text = "Greeting";

        Text containedText = (Text)sut.RenderableComponent;
        containedText.RawText.ShouldBe("Greeting");
    }

    [Fact]
    public void SetPropertyLocalizeText_ShouldSetLocalizeTextProperty()
    {
        TextRuntime sut = new();

        sut.SetProperty("LocalizeText", false);

        sut.LocalizeText.ShouldBeFalse();
    }

    [Fact]
    public void SettingLocalizeTextToFalse_ShouldUntranslateAlreadyAssignedText()
    {
        TextRuntime sut = new();
        sut.Text = "Greeting";
        ((Text)sut.RenderableComponent).RawText.ShouldBe("Hola");

        sut.LocalizeText = false;

        ((Text)sut.RenderableComponent).RawText.ShouldBe("Greeting");
    }
}
