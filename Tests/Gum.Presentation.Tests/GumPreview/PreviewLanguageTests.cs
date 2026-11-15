using Gum.DataTypes;
using Gum.Localization;
using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class PreviewLanguageTests
{
    private static LocalizationService LoadedService()
    {
        LocalizationService service = new LocalizationService();
        service.AddDatabase(
            new Dictionary<string, string[]> { ["Hello"] = new[] { "Hello", "Hola", "Bonjour" } },
            new List<string> { "Spanish", "French" });
        return service;
    }

    [Fact]
    public void Apply_WithLocalization_AppliesTheProjectsLanguageAndNamesIt()
    {
        LocalizationService service = LoadedService();
        GumProjectSave project = new GumProjectSave { CurrentLanguageIndex = 2 };

        string? name = PreviewLanguage.Apply(project, service);

        name.ShouldBe("French");
        service.CurrentLanguage.ShouldBe(2);
        service.Translate("Hello").ShouldBe("Bonjour");
    }

    [Fact]
    public void Apply_WhenTheToolChangesTheLanguage_FollowsIt()
    {
        LocalizationService service = LoadedService();
        GumProjectSave project = new GumProjectSave { CurrentLanguageIndex = 1 };
        PreviewLanguage.Apply(project, service).ShouldBe("Spanish");

        project.CurrentLanguageIndex = 2;

        PreviewLanguage.Apply(project, service).ShouldBe("French");
        service.Translate("Hello").ShouldBe("Bonjour");
    }

    [Fact]
    public void Apply_WithTheSourceLanguage_ShowsNoName()
    {
        PreviewLanguage.Apply(new GumProjectSave { CurrentLanguageIndex = 0 }, LoadedService()).ShouldBeNull();
    }

    [Fact]
    public void Apply_WithAnIndexPastTheLanguages_ShowsNoName()
    {
        PreviewLanguage.Apply(new GumProjectSave { CurrentLanguageIndex = 9 }, LoadedService()).ShouldBeNull();
    }

    [Fact]
    public void Apply_WithoutLocalizationFiles_ShowsNoNameAndLeavesTheService()
    {
        LocalizationService service = new LocalizationService();

        PreviewLanguage.Apply(new GumProjectSave { CurrentLanguageIndex = 1 }, service).ShouldBeNull();

        service.CurrentLanguage.ShouldBe(0);
    }

    [Fact]
    public void Apply_WithNoServiceOrProject_ShowsNoName()
    {
        PreviewLanguage.Apply(new GumProjectSave(), null).ShouldBeNull();
        PreviewLanguage.Apply(null, LoadedService()).ShouldBeNull();
    }
}
