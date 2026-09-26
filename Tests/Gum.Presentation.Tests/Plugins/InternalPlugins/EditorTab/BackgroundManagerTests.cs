using CommunityToolkit.Mvvm.Messaging;
using EditorTabPlugin_XNA.Services;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Plugins;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.EditorTab;

public class BackgroundManagerTests
{
    [Fact]
    public void ThemeChangedMessage_BeforeInitialize_DoesNotThrow()
    {
        // A theme change can arrive before the canvas exists (e.g. the --theme startup override).
        IMessenger messenger = new StrongReferenceMessenger();
        WireframeCommands wireframeCommands = new WireframeCommands(
            Mock.Of<IWireframeObjectManager>(), Mock.Of<IPluginManager>());
        using BackgroundManager backgroundManager = new BackgroundManager(
            wireframeCommands, messenger, Mock.Of<IThemingService>());

        Should.NotThrow(() => messenger.Send(new ThemeChangedMessage(Mock.Of<IEffectiveThemeSettings>())));
    }
}
