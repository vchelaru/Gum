using Xunit;

namespace KniGumInCode.PlaywrightTests;

public class KniGumInCodeBrowserTests : IClassFixture<GumBrowserFixture>
{
    readonly GumBrowserFixture _browser;

    public KniGumInCodeBrowserTests(GumBrowserFixture browser) => _browser = browser;

    [Fact]
    public async Task Find_ReachesNamesInsideAComponent()
    {
        Assert.True(await _browser.Page.GumExistsAsync("OptionsComboBox"));
        // TextInstance exists in many components, so the path goes through the named parent.
        Assert.True(await _browser.Page.GumExistsAsync("OptionsComboBox.TextInstance"));
        Assert.False(await _browser.Page.GumExistsAsync("NoSuchElement"));
    }

    [Fact]
    public async Task Set_ChangesTheVariableAndTheBounds()
    {
        await _browser.Page.GumSetAsync("TitleText", "Text", "Set from Playwright");
        Assert.Equal("Set from Playwright", await _browser.Page.GumGetTextAsync("TitleText", "Text"));

        GumBounds before = await _browser.Page.GumBoundsAsync("ColoredRectangle");
        await _browser.Page.GumSetAsync("ColoredRectangle", "Width", 240);
        GumBounds after = await WaitForBoundsAsync("ColoredRectangle", b => b.Width > before.Width + 1);

        // Width is in Gum units; the bounds are CSS pixels, so only the ratio is stable.
        Assert.Equal(240.0 / 120.0, after.Width / before.Width, 2);
    }

    [Fact]
    public async Task ClickAndType_GoThroughTheRealInputPath()
    {
        await _browser.Page.GumClickAsync("TextBox1");
        await _browser.Page.Keyboard.TypeAsync("hello");

        await WaitForAsync(async () =>
            await _browser.Page.GumGetTextAsync("TextBox1.TextInstance", "Text") == "hello");
    }

    [Fact]
    public async Task Click_OpensTheComboBoxDropDown()
    {
        Assert.False(await _browser.Page.GumVisibleAsync("ListBoxInstance"));

        await _browser.Page.GumClickAsync("OptionsComboBox");

        await WaitForAsync(() => _browser.Page.GumVisibleAsync("ListBoxInstance"));
    }

    // Gum updates on the game's next frame, so poll instead of reading straight after a click or set.
    static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        for (int i = 0; i < 50; i++)
        {
            if (await condition()) return;
            await Task.Delay(100);
        }
        Assert.Fail("Condition was not met within 5 seconds");
    }

    async Task<GumBounds> WaitForBoundsAsync(string name, Func<GumBounds, bool> condition)
    {
        GumBounds bounds = await _browser.Page.GumBoundsAsync(name);
        for (int i = 0; i < 50 && !condition(bounds); i++)
        {
            await Task.Delay(100);
            bounds = await _browser.Page.GumBoundsAsync(name);
        }
        Assert.True(condition(bounds), "Bounds did not change within 5 seconds");
        return bounds;
    }
}
