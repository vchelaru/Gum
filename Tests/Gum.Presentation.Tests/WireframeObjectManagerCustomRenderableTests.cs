using Gum.Commands;
using Gum.Localization;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <c>HandleCreateGraphicalComponent</c>'s forwarding to <see cref="IPluginManager.CreateRenderableForType"/>.
/// This method used to be guarded by "#if GUM" from when WireframeObjectManager lived in Gum.csproj
/// (which always defines GUM). Gum.Presentation does not define GUM, so moving the class here without
/// removing the guard would have silently turned this into a permanent no-op (ElementSaveExtensions'
/// fallback renderable-creation path would always return null). This test proves the forwarding
/// actually happens now that the class lives here.
/// </summary>
public class WireframeObjectManagerCustomRenderableTests : IDisposable
{
    [Fact]
    public void Initialize_WiresCustomCreateGraphicalComponentFunc_ToPluginManager()
    {
        var mockPluginManager = new Mock<IPluginManager>();
        var expectedRenderable = Mock.Of<IRenderableIpso>();
        mockPluginManager.Setup(x => x.CreateRenderableForType("SomeType")).Returns(expectedRenderable);

        var wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            mockPluginManager.Object,
            Mock.Of<IProjectState>());

        wireframeObjectManager.Initialize();

        var result = ElementSaveExtensions.CustomCreateGraphicalComponentFunc!(
            "SomeType", Mock.Of<ISystemManagers>());

        result.ShouldBe(expectedRenderable);
        mockPluginManager.Verify(x => x.CreateRenderableForType("SomeType"), Times.Once);
    }

    public void Dispose()
    {
        // The delegate is a static field on ElementSaveExtensions; clear it so this test's mock
        // doesn't leak into other tests that run in the same process.
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = null;
    }
}
