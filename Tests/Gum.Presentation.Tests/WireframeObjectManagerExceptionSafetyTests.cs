using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// RefreshAll turns on thread-wide state for the rebuild (layout suspension, ObjectFinder's
/// ref-counted cache, font-regeneration override). An exception anywhere in the rebuild, including the
/// error reporting in its own catch block, must put all of it back, or every later build on the thread
/// lays out nothing and ObjectFinder keeps answering from a stale snapshot.
/// </summary>
public class WireframeObjectManagerExceptionSafetyTests : BaseTestClass
{
    private readonly Action<bool>? _originalSetLocalizationEnabled = GraphicalUiElement.SetLocalizationEnabled;

    public override void Dispose()
    {
        GraphicalUiElement.IsAllLayoutSuspended = false;
        GraphicalUiElement.SuppressFontRegeneration = false;
        GraphicalUiElement.SetLocalizationEnabled = _originalSetLocalizationEnabled;
        // Leave the ref-counted cache disabled even when the code under test leaked an enable.
        ObjectFinder.Self.ForceResetCache();
        ObjectFinder.Self.DisableCache();
        base.Dispose();
    }

    private static (WireframeObjectManager sut, ScreenSave screen, GumProjectSave project) CreateSut(
        Mock<IDialogService> dialogService, Mock<IPluginManager> pluginManager)
    {
        ScreenSave screen = new ScreenSave { Name = "BrokenScreen" };
        GumProjectSave project = new GumProjectSave();
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { screen });
        selectedState.Setup(x => x.SelectedElement).Returns(screen);

        WireframeObjectManager sut = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            dialogService.Object,
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            pluginManager.Object,
            Mock.Of<IProjectState>());
        return (sut, screen, project);
    }

    [Fact]
    public void RefreshAll_ShouldRestoreThreadWideState_WhenReportingTheBuildErrorThrows()
    {
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.CreateGraphicalUiElement(It.IsAny<ElementSave>()))
            .Throws(new InvalidOperationException("build failed"));
        Mock<IDialogService> dialogService = new Mock<IDialogService>();
        dialogService.Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Throws(new InvalidOperationException("dialog failed"));
        (WireframeObjectManager sut, _, GumProjectSave project) = CreateSut(dialogService, pluginManager);
        GraphicalUiElement.SuppressFontRegeneration = true;

        Should.Throw<InvalidOperationException>(() => sut.RefreshAll(forceLayout: true));

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBeFalse();
        GraphicalUiElement.SuppressFontRegeneration.ShouldBeTrue();
        ComponentSave addedLater = new ComponentSave { Name = "AddedAfterTheFailedRefresh" };
        project.Components.Add(addedLater);
        ObjectFinder.Self.GetComponent("AddedAfterTheFailedRefresh").ShouldBeSameAs(addedLater);
    }

    [Fact]
    public void RefreshAll_ShouldNotLeaveLayoutSuspended_WhenSettingLocalizationThrows()
    {
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        (WireframeObjectManager sut, _, _) = CreateSut(new Mock<IDialogService>(), pluginManager);
        GraphicalUiElement.SetLocalizationEnabled = _ => throw new InvalidOperationException("localization failed");

        Should.Throw<InvalidOperationException>(() => sut.RefreshAll(forceLayout: true));

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBeFalse();
    }
}
