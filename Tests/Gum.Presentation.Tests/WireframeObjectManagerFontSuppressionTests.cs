using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
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
/// A scrub's intermediate ticks run with font regeneration suppressed. A full wireframe rebuild
/// during such a tick creates fresh Text objects that must still resolve their font, or they render
/// with the default font (#5714, FontScale).
/// </summary>
public class WireframeObjectManagerFontSuppressionTests
{
    [Fact]
    public void RefreshAll_ShouldResolveFonts_WhenCalledWhileFontRegenerationIsSuppressed()
    {
        ScreenSave screen = new ScreenSave { Name = "FontSuppressionScreen" };

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { screen });
        selectedState.Setup(x => x.SelectedElement).Returns(screen);

        bool? suppressedDuringCreate = null;
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.CreateGraphicalUiElement(screen))
            .Callback(() => suppressedDuringCreate = GraphicalUiElement.SuppressFontRegeneration)
            .Returns((GraphicalUiElement?)null);

        WireframeObjectManager sut = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            pluginManager.Object,
            Mock.Of<IProjectState>());

        GraphicalUiElement.SuppressFontRegeneration = true;
        try
        {
            sut.RefreshAll(forceLayout: true);

            suppressedDuringCreate.ShouldBe(false);
            GraphicalUiElement.SuppressFontRegeneration.ShouldBeTrue();
        }
        finally
        {
            GraphicalUiElement.SuppressFontRegeneration = false;
        }
    }
}
