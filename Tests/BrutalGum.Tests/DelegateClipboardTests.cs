using Gum.Input;
using Shouldly;

namespace BrutalGum.Tests;

/// <summary>
/// <see cref="DelegateClipboard"/> is compiled into every interactive Skia host; these run it once,
/// against a fake getter and setter, through the shared clipboard contract.
/// </summary>
public class DelegateClipboardTests : SkiaHostTests.ClipboardTestsBase
{
    private string? _store = "";

    protected override Gum.Forms.Controls.IGumClipboard CreateClipboard() =>
        new DelegateClipboard(() => _store, text => _store = text);

    [Fact]
    public void GetText_ReturnsNull_WhenGetterReturnsNull()
    {
        _store = null;

        CreateClipboard().GetText(null).ShouldBeNull();
    }

    [Fact]
    public void SetText_PassesTextToSetter()
    {
        string? received = null;

        new DelegateClipboard(() => null, text => received = text).SetText("copied");

        received.ShouldBe("copied");
    }
}
