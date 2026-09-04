using Gum.Input;
using Shouldly;

namespace StrideGum.Tests;

/// <summary>
/// Unit tests for <see cref="StrideGumClipboard"/>, which bridges Gum's <c>IGumClipboard</c> to the
/// cross-platform TextCopy package (Stride's runtime exposes no clipboard API of its own).
/// </summary>
public class StrideGumClipboardTests
{
    [Fact]
    public void SetText_ThenGetText_RoundTrips()
    {
        var clipboard = new StrideGumClipboard();

        clipboard.SetText("hello from StrideGum.Tests");

        clipboard.GetText(null).ShouldBe("hello from StrideGum.Tests");
    }

    [Fact]
    public void GetText_IgnoresCallback_WhenClipboardResolvesSynchronously()
    {
        var clipboard = new StrideGumClipboard();
        clipboard.SetText("synchronous");
        bool callbackInvoked = false;

        string? result = clipboard.GetText(() => callbackInvoked = true);

        result.ShouldBe("synchronous");
        callbackInvoked.ShouldBeFalse();
    }
}
