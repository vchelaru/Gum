using Gum.Input;
using Shouldly;

namespace BrutalGum.Tests;

public class BrutalGumClipboardTests : SkiaHostTests.ClipboardTestsBase
{
    protected override Gum.Forms.Controls.IGumClipboard CreateClipboard() =>
        new BrutalGumClipboard(new FakeBrutalInput());

    [Fact]
    public void SetText_WritesToInputClipboard()
    {
        FakeBrutalInput input = new();

        new BrutalGumClipboard(input).SetText("copied");

        input.ClipboardText.ShouldBe("copied");
    }

    [Fact]
    public void GetText_ReadsInputClipboard()
    {
        FakeBrutalInput input = new() { ClipboardText = "hello" };

        new BrutalGumClipboard(input).GetText(null).ShouldBe("hello");
    }
}
