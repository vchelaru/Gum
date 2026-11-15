using Gum;
using Shouldly;

namespace BrutalGum.Tests;

public class BrutalGumClipboardTests : SkiaHostTests.ClipboardTestsBase
{
    protected override Gum.Forms.Controls.IGumClipboard CreateClipboard() =>
        GumService.CreateClipboard(new FakeBrutalInput());

    [Fact]
    public void SetText_WritesToInputClipboard()
    {
        FakeBrutalInput input = new();

        GumService.CreateClipboard(input).SetText("copied");

        input.ClipboardText.ShouldBe("copied");
    }

    [Fact]
    public void GetText_ReadsInputClipboard()
    {
        FakeBrutalInput input = new() { ClipboardText = "hello" };

        GumService.CreateClipboard(input).GetText(null).ShouldBe("hello");
    }
}
