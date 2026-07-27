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
    private readonly GumProjectSave _toolProject;
    private readonly VariableSave _textVariable;
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
        // Mirrors MainEditorTabPlugin.StartUp()'s wiring of the "Show Localization" toggle hook.
        GraphicalUiElement.SetLocalizationEnabled = enabled =>
            CustomSetPropertyOnRenderable.LocalizationService = enabled ? _localizationService : null;

        _screen = new ScreenSave { Name = "TextScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(defaultState);
        // Stored the same way the real state system holds an instance's Text value - a
        // VariableSave, read (never written) by ApplyState each time the tree is rebuilt.
        _textVariable = new VariableSave { Name = "CancelLabel.Text", Value = "T_Cancel", Type = "string" };
        defaultState.Variables.Add(_textVariable);

        GumProjectSave project = new();
        project.Screens.Add(_screen);
        ObjectFinder.Self.GumProjectSave = project;

        Mock<ISelectedState> selectedState = new();
        selectedState.SetupGet(x => x.SelectedElements).Returns(new[] { _screen });
        selectedState.SetupGet(x => x.SelectedElement).Returns(_screen);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);

        Mock<IPluginManager> pluginManager = new();
        // Mirrors what real ToGraphicalUiElement/ApplyState does for a Text instance's default
        // state Text variable: reads the stored VariableSave value and pushes it through
        // GraphicalUiElement.SetProperty("Text", ...) - it never writes back to the VariableSave.
        pluginManager.Setup(x => x.CreateGraphicalUiElement(_screen)).Returns(() =>
        {
            GraphicalUiElement root = new(new InvisibleRenderable()) { Name = "TextScreen" };
            _textGue = new GraphicalUiElement(new Text()) { Name = "CancelLabel", Parent = root };
            _textGue.SetProperty("Text", (string)_textVariable.Value);
            return root;
        });

        _toolProject = new GumProjectSave { ShowLocalizationInGum = true };
        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(_toolProject);

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
        GraphicalUiElement.SetLocalizationEnabled = null;
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

    [Fact]
    public void RefreshAll_WithShowLocalizationInGumFalse_ShouldShowTheLiteralStringId()
    {
        _toolProject.ShowLocalizationInGum = false;

        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _textGue.ShouldNotBeNull();
        Text containedText = (Text)_textGue!.RenderableComponent;
        containedText.RawText.ShouldBe("T_Cancel");
    }

    [Fact]
    public void RefreshAll_AfterShowLocalizationInGumIsToggledBackOn_ShouldTranslateAgain()
    {
        _toolProject.ShowLocalizationInGum = false;
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _toolProject.ShowLocalizationInGum = true;
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _textGue.ShouldNotBeNull();
        Text containedText = (Text)_textGue!.RenderableComponent;
        containedText.RawText.ShouldBe("Cancelar");
    }

    [Fact]
    public void RefreshAll_ThroughRepeatedLanguageAndToggleChanges_ShouldNeverMutateTheStoredVariable()
    {
        // Regression pin for the original reported chain: an earlier (now-fixed) bug baked the
        // translated text back into the persisted value, so turning localization off revealed an
        // already-corrupted "Hello(loc)" instead of the real raw "Hello". This drives RefreshAll
        // through several language switches and Show Localization toggles and asserts the
        // VariableSave itself - the actual saved/authored value - is untouched throughout.
        _wireframeObjectManager.RefreshAll(forceLayout: true);
        _localizationService.CurrentLanguage = 1;
        _wireframeObjectManager.RefreshAll(forceLayout: true);
        _toolProject.ShowLocalizationInGum = false;
        _wireframeObjectManager.RefreshAll(forceLayout: true);
        _toolProject.ShowLocalizationInGum = true;
        _localizationService.CurrentLanguage = 2;
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _textVariable.Value.ShouldBe("T_Cancel");
    }
}
