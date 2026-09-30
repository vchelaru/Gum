using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>The Texture Coordinates tab for a PR's before/after table (see Harness/README.md).</summary>
[Trait("Category", PrScreenshot.Category)]
public class TextureCoordinateScreenshotTests
{
    [SkippableFact]
    public void SpriteWithNoTexture() => PrScreenshot.Run(() =>
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
        ComponentSave button = tab.Project.AddComponent("Button");
        InstanceSave plain = tab.Project.AddInstance(button, "Plain", "Sprite");

        tab.Select(plain);

        PrScreenshot.SaveWindow(tab.Input.Window, "texture-coordinates-no-texture");
    });
}
