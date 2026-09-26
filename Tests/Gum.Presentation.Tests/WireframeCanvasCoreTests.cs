using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using XnaAndWinforms;

namespace Gum.Presentation.Tests;

/// <summary>
/// <see cref="WireframeCanvasCore.DrawsOpaqueFrames"/> tells the Avalonia canvas whether to show a
/// frame fully opaque. Before the core initializes, <see cref="WireframeCanvasCore.Draw"/> leaves the
/// frame transparent, so forcing it opaque would show a black canvas instead of the window behind it.
/// </summary>
public class WireframeCanvasCoreTests
{
    [Fact]
    public void DrawsOpaqueFrames_BeforeInitialize_IsFalse()
    {
        WireframeCanvasCore core = new WireframeCanvasCore(
            Mock.Of<ICanvasHost>(), Mock.Of<IDialogService>(), Mock.Of<IOutputManager>(), Mock.Of<IPluginManager>());

        core.DrawsOpaqueFrames.ShouldBeFalse();
    }
}
