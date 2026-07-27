using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Regression coverage: editing a Text variable with a localization database loaded produced
/// "Hello(loc)(loc)(loc)" instead of "Hello(loc)" for a missing string ID. Root cause was
/// WireframeObjectManager.ApplyLocalization translating the string itself and then passing the
/// already-translated result into GraphicalUiElement.SetProperty("Text", ...), which internally
/// translates again via CustomSetPropertyOnRenderable.TrySetPropertyOnText - a double translate.
/// A second contributor: when called with no forcedId, ApplyLocalization fell back to the live
/// (possibly already-translated) RawText instead of the originally-assigned string ID, so repeated
/// no-arg calls (e.g. RefreshAll) kept compounding the suffix further.
/// </summary>
public class WireframeObjectManagerApplyLocalizationTests : BaseTestClass
{
    private readonly WireframeObjectManager _wireframeObjectManager;
    private readonly LocalizationService _localizationService;

    public WireframeObjectManagerApplyLocalizationTests()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();

        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.TryGetLocalizationKey = CustomSetPropertyOnRenderable.TryGetLocalizationKey;

        _localizationService = new LocalizationService();
        // Loaded database that doesn't contain "Hello": any translate call on it appends
        // "(loc)" - this is what makes repeated/duplicate translation visible.
        _localizationService.AddDatabase(
            new Dictionary<string, string[]> { { "SomeOtherId", new[] { "SomeOtherId", "Translated" } } },
            new List<string> { "Default", "English" });
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;

        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave { ShowLocalizationInGum = true });

        _wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            _localizationService,
            Mock.Of<IPluginManager>(),
            projectState.Object);
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.LocalizationService = null;
        GraphicalUiElement.TryGetLocalizationKey = null;
        base.Dispose();
    }

    [Fact]
    public void ApplyLocalization_WithForcedId_ShouldNotDoubleTranslate()
    {
        GraphicalUiElement gue = new(new Text());
        // Mirrors MainEditorTabPlugin.HandleVariableSetLate's first push: the raw committed value
        // goes straight through SetProperty, which translates it once.
        gue.SetProperty("Text", "Hello");

        // Mirrors the second, redundant push at MainEditorTabPlugin.cs:783, passing the same raw
        // string ID as forcedId.
        _wireframeObjectManager.ApplyLocalization(gue, "Hello");

        Text containedText = (Text)gue.RenderableComponent;
        containedText.RawText.ShouldBe("Hello(loc)");
    }

    [Fact]
    public void ApplyLocalization_WithoutForcedId_ShouldNotReTranslateAlreadyTranslatedText()
    {
        GraphicalUiElement gue = new(new Text());
        gue.SetProperty("Text", "Hello");

        // A no-arg refresh pass (e.g. RefreshAll / a full ApplyLocalization() sweep) must
        // re-translate from the original string ID, not the already-translated live RawText.
        _wireframeObjectManager.ApplyLocalization(gue);

        Text containedText = (Text)gue.RenderableComponent;
        containedText.RawText.ShouldBe("Hello(loc)");
    }
}
