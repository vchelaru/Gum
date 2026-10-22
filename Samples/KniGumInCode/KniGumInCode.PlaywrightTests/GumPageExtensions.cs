using Microsoft.Playwright;

namespace KniGumInCode.PlaywrightTests;

/// <summary>An element's box in CSS pixels in the page viewport, ready for page.Mouse.</summary>
public class GumBounds
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public bool Visible { get; set; }
}

/// <summary>Thin C# wrappers over window.gumAutomation. Each one is a single page.EvaluateAsync call.</summary>
public static class GumPageExtensions
{
    /// <summary>Throws if no element has that name. "Parent.Child" reaches a name inside a component.</summary>
    public static Task<bool> GumExistsAsync(this IPage page, string name) =>
        page.EvaluateAsync<bool>(
            "name => { try { window.gumAutomation.find(name); return true; } catch { return false; } }", name);

    /// <summary>False when the element is missing or it, or an ancestor, is hidden.</summary>
    public static Task<bool> GumVisibleAsync(this IPage page, string name) =>
        page.EvaluateAsync<bool>(
            "name => { try { return window.gumAutomation.find(name).visible; } catch { return false; } }", name);

    public static Task<GumBounds> GumBoundsAsync(this IPage page, string name) =>
        page.EvaluateAsync<GumBounds>("name => window.gumAutomation.bounds(name)", name);

    public static Task<string?> GumGetTextAsync(this IPage page, string name, string property) =>
        page.EvaluateAsync<string?>("([n, p]) => window.gumAutomation.get(n, p)", new object[] { name, property });

    public static Task GumSetAsync(this IPage page, string name, string property, object value) =>
        page.EvaluateAsync("([n, p, v]) => window.gumAutomation.set(n, p, v)", new object[] { name, property, value });

    /// <summary>Clicks the middle of an element with the real mouse, so Gum sees an ordinary browser click.</summary>
    public static async Task GumClickAsync(this IPage page, string name)
    {
        GumBounds bounds = await page.GumBoundsAsync(name);
        await page.Mouse.ClickAsync((float)bounds.CenterX, (float)bounds.CenterY);
    }
}
