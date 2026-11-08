using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// What every interactive Skia host's clipboard does for text: a set is read back by a get, and the
/// synchronous clipboards do not invoke the get callback. A host's test project derives from this
/// class and supplies a clipboard whose backing store works in the test process.
/// </summary>
public abstract class ClipboardTestsBase
{
    /// <summary>A fresh clipboard over a backing store the test can write and read.</summary>
    protected abstract IGumClipboard CreateClipboard();

    [Fact]
    public void SetText_ThenGetText_RoundTrips()
    {
        IGumClipboard clipboard = CreateClipboard();

        string text = "round trip " + Guid.NewGuid();
        clipboard.SetText(text);

        clipboard.GetText(null).ShouldBe(text);
    }

    [Fact]
    public void GetText_IgnoresCallback_WhenClipboardResolvesSynchronously()
    {
        IGumClipboard clipboard = CreateClipboard();
        clipboard.SetText("stale sentinel");
        string expected = "synchronous " + Guid.NewGuid();
        clipboard.SetText(expected);
        bool callbackInvoked = false;

        string? result = clipboard.GetText(() => callbackInvoked = true);

        result.ShouldBe(expected);
        callbackInvoked.ShouldBeFalse();
    }
}
