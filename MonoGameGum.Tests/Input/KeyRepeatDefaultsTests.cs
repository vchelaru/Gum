using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Tests how a keyboard's key repeat starts from the operating system's settings: the mapping from OS
/// values and applying them once, without reading the real OS.
/// </summary>
public class KeyRepeatDefaultsTests : IDisposable
{
    private sealed class FakeProvider : IKeyRepeatSettingsProvider
    {
        public KeyRepeatSettings? Settings { get; set; }
        public bool Throws { get; set; }

        public KeyRepeatSettings? GetSettings()
        {
            if (Throws)
            {
                throw new InvalidOperationException("OS unavailable");
            }

            return Settings;
        }
    }

    private sealed class FakePolledKeyboard : PolledKeyboard
    {
        protected override IEnumerable<GumKeys> SupportedKeys => new[] { GumKeys.Left };

        protected override bool IsDeviceKeyDown(GumKeys key) => false;
    }

    private readonly IKeyRepeatSettingsProvider? _originalProvider = KeyRepeatDefaults.Provider;

    public void Dispose()
    {
        KeyRepeatDefaults.Provider = _originalProvider;
    }

    [Theory]
    [InlineData(0, 250)]
    [InlineData(1, 500)]
    [InlineData(2, 750)]
    [InlineData(3, 1000)]
    [InlineData(-1, 250)]
    [InlineData(4, 1000)]
    public void FromWindowsLevels_DelayLevel_ShouldBeQuarterSecondsPlusOne(int delayLevel, double expectedMilliseconds)
    {
        KeyRepeatSettings settings = OsKeyRepeatSettingsProvider.FromWindowsLevels(delayLevel, speedLevel: 31);

        settings.Delay.TotalMilliseconds.ShouldBe(expectedMilliseconds);
    }

    [Theory]
    [InlineData(0, 400)]
    [InlineData(31, 33.33)]
    [InlineData(15, 63.27)]
    [InlineData(-1, 400)]
    [InlineData(32, 33.33)]
    public void FromWindowsLevels_SpeedLevel_ShouldSpanTwoPointFiveToThirtyRepeatsPerSecond(
        int speedLevel, double expectedMilliseconds)
    {
        KeyRepeatSettings settings = OsKeyRepeatSettingsProvider.FromWindowsLevels(delayLevel: 1, speedLevel);

        settings.Rate.TotalMilliseconds.ShouldBe(expectedMilliseconds, tolerance: 0.01);
    }

    [Fact]
    public void ApplyOsSettings_WithProviderSettings_ShouldSetMonoGameKeyboard()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };
        Keyboard keyboard = new Keyboard();

        KeyRepeatDefaults.ApplyOsSettings(keyboard);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(250));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public void ApplyOsSettings_WithProviderSettings_ShouldSetPolledKeyboard()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(750), TimeSpan.FromMilliseconds(100))
        };
        FakePolledKeyboard keyboard = new FakePolledKeyboard();

        KeyRepeatDefaults.ApplyOsSettings(keyboard);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(750));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void ApplyOsSettings_ProviderReturnsNull_ShouldKeepDefaults()
    {
        KeyRepeatDefaults.Provider = new FakeProvider { Settings = null };
        Keyboard keyboard = new Keyboard();

        KeyRepeatDefaults.ApplyOsSettings(keyboard);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void ApplyOsSettings_NoProvider_ShouldKeepDefaults()
    {
        KeyRepeatDefaults.Provider = null;
        Keyboard keyboard = new Keyboard();

        KeyRepeatDefaults.ApplyOsSettings(keyboard);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void ApplyOsSettings_ProviderThrows_ShouldKeepDefaultsAndNotThrow()
    {
        KeyRepeatDefaults.Provider = new FakeProvider { Throws = true };
        Keyboard keyboard = new Keyboard();

        Should.NotThrow(() => KeyRepeatDefaults.ApplyOsSettings(keyboard));

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void ApplyOsSettings_ValueSetBeforehand_ShouldKeepTheUsersValueAndSetTheOther()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };
        Keyboard keyboard = new Keyboard();
        keyboard.RepeatDelay = TimeSpan.FromMilliseconds(900);

        KeyRepeatDefaults.ApplyOsSettings(keyboard);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(900));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public void ApplyOsSettings_ValueSetAfterwards_ShouldWin()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };
        Keyboard keyboard = new Keyboard();
        KeyRepeatDefaults.ApplyOsSettings(keyboard);

        keyboard.RepeatDelay = TimeSpan.FromMilliseconds(900);
        keyboard.RepeatRate = TimeSpan.FromMilliseconds(15);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(900));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(15));
    }

    [Fact]
    public void ApplyOsSettings_NullKeyboard_ShouldNotThrow()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };

        Should.NotThrow(() => KeyRepeatDefaults.ApplyOsSettings(null));
    }

    [Fact]
    public void ApplyOsSettings_KeyboardWithoutRepeatMembers_ShouldNotThrow()
    {
        KeyRepeatDefaults.Provider = new FakeProvider
        {
            Settings = new KeyRepeatSettings(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(40))
        };
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard> { CallBase = true };

        Should.NotThrow(() => KeyRepeatDefaults.ApplyOsSettings(keyboard.Object));
    }

    [Fact]
    public void OsProvider_GetSettings_ShouldNotThrowAndBeWithinWindowsRangeWhenAvailable()
    {
        KeyRepeatSettings? settings = new OsKeyRepeatSettingsProvider().GetSettings();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            settings.ShouldNotBeNull();
        }

        if (settings != null)
        {
            settings.Value.Delay.TotalMilliseconds.ShouldBeInRange(250, 1000);
            settings.Value.Rate.TotalMilliseconds.ShouldBeInRange(33, 401);
        }
    }
}
