using Gum;
using RenderingLibrary;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;

namespace BrutalGum.Tests;

public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Clipboard_IsAssigned_AfterInitialize()
    {
        GumService.Default.Clipboard.ShouldNotBeNull();
    }

    [Fact]
    public void Update_WithFakeWindow_DoesNotThrow()
    {
        Should.NotThrow(() => GumService.Default.Update(0));
    }

    [Fact]
    public void ApplyGamePadState_WithNoGamepad_LeavesItDisconnected()
    {
        Gum.Input.GamePad gamePad = new();

        ((IGumService)GumService.Default).ApplyGamePadState(gamePad, 0, 1);

        gamePad.IsConnected.ShouldBeFalse();
    }
}
