using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;

namespace MonoGameGum.Tests.V3;

/// <summary>
/// Pins that <see cref="FormsUtilities.InitializeDefaults"/> starts the keyboard it creates from the
/// operating system's key repeat settings, using a fake provider so the real OS is never read.
/// </summary>
public class FormsUtilitiesKeyRepeatTests : IDisposable
{
    private sealed class FakeProvider : IKeyRepeatSettingsProvider
    {
        public KeyRepeatSettings? Settings { get; set; }

        public KeyRepeatSettings? GetSettings() => Settings;
    }

    private readonly IKeyRepeatSettingsProvider? _originalProvider = KeyRepeatDefaults.Provider;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly ICursor? _originalCursor = FrameworkElement.MainCursor;
    private readonly InteractiveGue _originalPopupRoot = FrameworkElement.PopupRoot!;
    private readonly InteractiveGue _originalModalRoot = FrameworkElement.ModalRoot!;

    public void Dispose()
    {
        KeyRepeatDefaults.Provider = _originalProvider;
        FrameworkElement.PopupRoot = _originalPopupRoot;
        FrameworkElement.ModalRoot = _originalModalRoot;
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }

        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
    }

    [Fact]
    public void InitializeDefaults_WithProviderSettings_ShouldStartKeyboardFromThem()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };

        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);

        FormsUtilities.Keyboard.ShouldNotBeNull().RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(250));
        FormsUtilities.Keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public void InitializeDefaults_WhenServiceCreatesNoKeyboard_ShouldNotThrow()
    {
        // Render-only hosts (SkiaGum, SkiaGum.Wpf, ...) inherit IGumService.CreateKeyboard() => null.
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };
        IGumService? originalService = IGumService.Default;
        Mock<IGumService> service = new Mock<IGumService> { CallBase = true };
        service.Setup(x => x.CreateCursor()).Returns(((IGumService)Gum.GumService.Default).CreateCursor());
        service.Setup(x => x.CreateKeyboard()).Returns((IInputReceiverKeyboard?)null);

        try
        {
            IGumService.Default = service.Object;

            Should.NotThrow(() => FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3));
        }
        finally
        {
            IGumService.Default = originalService;
        }
    }

    [Fact]
    public void InitializeDefaults_WithNoSettings_ShouldKeepDefaults()
    {
        KeyRepeatDefaults.Provider = new FakeProvider { Settings = null };

        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);

        FormsUtilities.Keyboard.ShouldNotBeNull().RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        FormsUtilities.Keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }
}
