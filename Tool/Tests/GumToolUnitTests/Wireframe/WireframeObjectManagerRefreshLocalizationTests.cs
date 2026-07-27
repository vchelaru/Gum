using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
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
/// Reproduces a reported regression: opening a screen whose Text is bound to a valid,
/// database-backed string ID showed the literal string ID ("T_Cancel") instead of the
/// translated text, and switching the language dropdown (which forces a RefreshAll) did
/// nothing on screen.
///
/// Root cause: the tool's DI-registered LocalizationService singleton (injected into
/// WireframeObjectManager/FileCommands/etc., and the one FileCommands.LoadLocalizationFile
/// actually populates) was never wired into the static
/// CustomSetPropertyOnRenderable.LocalizationService hook that GraphicalUiElement.SetProperty
/// ("Text", ...) uses internally to translate - RenderingLibrary.SystemManagers only does
/// `??= new LocalizationService()`, leaving that hook pointing at a permanently-empty database
/// in the tool. Fixed by MainEditorTabPlugin.StartUp() assigning the same DI instance into that
/// hook - this test mirrors that wiring in its constructor.
/// </summary>
public class WireframeObjectManagerRefreshLocalizationTests : BaseTestClass
{
    private readonly LocalizationService _localizationService;
    private readonly WireframeObjectManager _wireframeObjectManager;
    private readonly ScreenSave _screen;
    private GraphicalUiElement? _textGue;

    public WireframeObjectManagerRefreshLocalizationTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.TryGetLocalizationKey = CustomSetPropertyOnRenderable.TryGetLocalizationKey;

        _localizationService = new LocalizationService();
        _localizationService.AddDatabase(
            new Dictionary<string, string[]> { { "T_Cancel", new[] { "T_Cancel", "Cancel", "Cancelar" } } },
            new List<string> { "Default", "English", "Spanish" });
        _localizationService.CurrentLanguage = 2; // Spanish
        // Mirrors MainEditorTabPlugin.StartUp()'s wiring of the DI-injected LocalizationService
        // singleton into the static hook GraphicalUiElement.SetProperty("Text", ...) uses to
        // translate - see that method's comment for why this must be the SAME instance.
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService;

        _screen = new ScreenSave { Name = "TextScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(defaultState);

        GumProjectSave project = new();
        project.Screens.Add(_screen);
        ObjectFinder.Self.GumProjectSave = project;

        Mock<ISelectedState> selectedState = new();
        selectedState.SetupGet(x => x.SelectedElements).Returns(new[] { _screen });
        selectedState.SetupGet(x => x.SelectedElement).Returns(_screen);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);

        Mock<IPluginManager> pluginManager = new();
        // Mirrors what real ToGraphicalUiElement/ApplyState does for a Text instance's default
        // state Text variable: GraphicalUiElement.SetProperty("Text", "T_Cancel").
        pluginManager.Setup(x => x.CreateGraphicalUiElement(_screen)).Returns(() =>
        {
            GraphicalUiElement root = new(new InvisibleRenderable()) { Name = "TextScreen" };
            _textGue = new GraphicalUiElement(new Text()) { Name = "CancelLabel", Parent = root };
            _textGue.SetProperty("Text", "T_Cancel");
            return root;
        });

        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave { ShowLocalizationInGum = true });

        _wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            _localizationService,
            pluginManager.Object,
            projectState.Object);
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.LocalizationService = null;
        GraphicalUiElement.TryGetLocalizationKey = null;
        base.Dispose();
    }

    [Fact]
    public void RefreshAll_ShouldShowTranslatedTextForCurrentLanguage_NotTheLiteralStringId()
    {
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _textGue.ShouldNotBeNull();
        Text containedText = (Text)_textGue!.RenderableComponent;
        containedText.RawText.ShouldBe("Cancelar");
    }

    [Fact]
    public void RefreshAll_AfterLanguageChanges_ShouldRetranslateToTheNewLanguage()
    {
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        // Mirrors ProjectPropertiesChangeLogic reacting to the language dropdown: it sets
        // CurrentLanguage then forces a RefreshAll.
        _localizationService.CurrentLanguage = 1; // English
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _textGue.ShouldNotBeNull();
        Text containedText = (Text)_textGue!.RenderableComponent;
        containedText.RawText.ShouldBe("Cancel");
    }
}
