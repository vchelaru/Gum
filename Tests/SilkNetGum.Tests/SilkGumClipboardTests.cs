using Gum.Input;
using Moq;
using Shouldly;
using Silk.NET.Input;
using System.Collections.Generic;

namespace SilkNetGum.Tests;

/// <summary>
/// Unit tests for <see cref="SilkGumClipboard"/>, which bridges Gum's <c>IGumClipboard</c> to
/// Silk.NET.Input's <see cref="IKeyboard.ClipboardText"/>.
/// </summary>
public class SilkGumClipboardTests
{
    private static IInputContext CreateInputContext(IKeyboard? keyboard)
    {
        var context = new Mock<IInputContext>();
        var keyboards = keyboard == null
            ? new List<IKeyboard>()
            : new List<IKeyboard> { keyboard };
        context.Setup(c => c.Keyboards).Returns(keyboards);
        return context.Object;
    }

    [Fact]
    public void GetText_ReturnsNull_WhenNoKeyboardIsPresent()
    {
        var clipboard = new SilkGumClipboard(CreateInputContext(null));

        clipboard.GetText(null).ShouldBeNull();
    }

    [Fact]
    public void GetText_ReturnsKeyboardClipboardText()
    {
        var device = new Mock<IKeyboard>();
        device.SetupProperty(k => k.ClipboardText, "hello");
        var clipboard = new SilkGumClipboard(CreateInputContext(device.Object));

        clipboard.GetText(null).ShouldBe("hello");
    }

    [Fact]
    public void SetText_WritesToKeyboardClipboardText()
    {
        var device = new Mock<IKeyboard>();
        device.SetupProperty(k => k.ClipboardText, "");
        var clipboard = new SilkGumClipboard(CreateInputContext(device.Object));

        clipboard.SetText("copied");

        device.Object.ClipboardText.ShouldBe("copied");
    }

    [Fact]
    public void SetText_DoesNotThrow_WhenNoKeyboardIsPresent()
    {
        var clipboard = new SilkGumClipboard(CreateInputContext(null));

        Should.NotThrow(() => clipboard.SetText("copied"));
    }
}
