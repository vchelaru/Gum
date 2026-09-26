using CommunityToolkit.Mvvm.Messaging;
using Gum.Dialogs;
using Moq;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;

namespace Gum.Presentation.Tests.Plugins.TextureCoordinateSelectionPlugin;

public class BackgroundManagerTests
{
    [Fact]
    public void ThemeChanged_BeforeInitialize_DoesNotThrow()
    {
        // The manager subscribes to theme changes in its constructor, but its visuals only exist
        // once the canvas's graphics device is ready.
        StrongReferenceMessenger messenger = new StrongReferenceMessenger();
        BackgroundManager manager = new BackgroundManager(messenger, new Mock<IThemingService>().Object);

        Should.NotThrow(() => messenger.Send(new ThemeChangedMessage(new Mock<IEffectiveThemeSettings>().Object)));

        manager.Dispose();
    }
}
