using Gum.Localization;
using Gum.Wireframe;
using Shouldly;
using SkiaGum;
using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;

namespace SkiaGum.Tests.Localization;

/// <summary>
/// Regression coverage for #3621: SkiaGum's CustomSetPropertyOnRenderable copy never ran the
/// localization path, so text assigned through the localized "Text" property was passed through
/// untranslated in SkiaGum-rendered hosts (SkiaGum.Standalone / WPF / MAUI, Gum.SilkNet).
/// </summary>
public class TextLocalizationTests : IDisposable
{
    private readonly LocalizationService _localizationService;

    public TextLocalizationTests()
    {
        // Route SetProperty through SkiaGum's dispatcher (normally done by SystemManagers.Initialize;
        // the full rendering pipeline isn't needed for these assignment-level assertions).
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;

        _localizationService = new LocalizationService();
        Dictionary<string, string[]> entries = new()
        {
            { "Greeting", new[] { "Hello", "Hola", "Bonjour" } },
            { "BBStr",    new[] { "plain English", "[Color=Red]Spanish red[/Color]", "French" } },
        };
        List<string> headers = new() { "English", "Spanish", "French" };
        _localizationService.AddDatabase(entries, headers);
        _localizationService.CurrentLanguage = 1;
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;
    }

    public void Dispose()
    {
        // LocalizationService is a process-wide static; reset it so it can't leak into other
        // test classes (mirrors MonoGameGum.Tests' BaseTestClass.Dispose).
        CustomSetPropertyOnRenderable.LocalizationService = null;
    }

    [Fact]
    public void SetTextNoTranslate_WhenLocalizationServiceSet_ShouldBypassTranslation()
    {
        TextRuntime sut = new();
        sut.SetTextNoTranslate("Greeting");

        Text containedText = (Text)sut.RenderableComponent;
        containedText.RawText.ShouldBe("Greeting");
    }

    [Fact]
    public void Text_WhenLocalizationServiceSet_ShouldTranslateThroughToRawText()
    {
        TextRuntime sut = new();
        sut.Text = "Greeting";

        Text containedText = (Text)sut.RenderableComponent;
        containedText.RawText.ShouldBe("Hola");
    }

    [Fact]
    public void Text_WhenTranslationContainsBBCode_ShouldReachRawText()
    {
        // SkiaGum's Text renders RawText literally through RichTextKit's TextBlock.AddText; there is
        // no separate markup path (StoredMarkupText is a stub), so a translation containing [...] must
        // land in RawText exactly as any other string.
        TextRuntime sut = new();
        sut.Text = "BBStr";

        Text containedText = (Text)sut.RenderableComponent;
        containedText.RawText.ShouldBe("[Color=Red]Spanish red[/Color]");
    }
}
