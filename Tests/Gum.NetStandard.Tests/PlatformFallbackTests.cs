using Gum.Forms.Controls;
using Gum.Input;
using Shouldly;
using SkiaSharp;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.NetStandard.Tests;

/// <summary>
/// netstandard2.1 has no OperatingSystem.IsX checks, so its build detects the platform through
/// RuntimeInformation instead. These compare that fallback with the .NET 10 answer for the test host.
/// </summary>
public class PlatformFallbackTests : IDisposable
{
    private sealed class HeldKeyboard : PolledKeyboard
    {
        public HashSet<GumKeys> Held { get; } = new HashSet<GumKeys>();

        protected override IEnumerable<GumKeys> SupportedKeys => new[] { GumKeys.LeftWindows };

        protected override bool IsDeviceKeyDown(GumKeys key) => Held.Contains(key);
    }

    private readonly TestGumService _service = new TestGumService();

    public void Dispose() => _service.Uninitialize();

    [Fact]
    public void PolledKeyboard_IsCommandDown_MatchesHostIsMacOS()
    {
        HeldKeyboard keyboard = new HeldKeyboard();
        keyboard.Held.Add(GumKeys.LeftWindows);

        keyboard.Activity(gameTime: 0);

        keyboard.IsCommandDown.ShouldBe(OperatingSystem.IsMacOS());
    }

    [Fact]
    public void TextBox_ShowNativeKeyboardOnFocus_MatchesHostIsMobile()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        _service.Initialize(surface.Canvas, 200, 100);

        TextBox textBox = new TextBox();

        textBox.ShowNativeKeyboardOnFocus.ShouldBe(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
    }
}
