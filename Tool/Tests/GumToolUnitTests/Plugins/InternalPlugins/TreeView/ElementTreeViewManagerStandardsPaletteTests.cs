using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class ElementTreeViewManagerStandardsPaletteTests : BaseTestClass
{
    [Fact]
    public void SortStandardTypeNamesForPalette_UnsortedNames_ReturnsAlphabeticalOrder()
    {
        List<string> typeNames = new List<string> { "Text", "Sprite", "Container", "Circle", "Rectangle", "Polygon", "NineSlice" };

        List<string> result = ElementTreeViewManager.SortStandardTypeNamesForPalette(typeNames);

        result.ShouldBe(new[] { "Circle", "Container", "NineSlice", "Polygon", "Rectangle", "Sprite", "Text" });
    }

    [Fact]
    public void SortStandardTypeNamesForPalette_EmptyInput_ReturnsEmpty()
    {
        List<string> result = ElementTreeViewManager.SortStandardTypeNamesForPalette(new List<string>());

        result.ShouldBeEmpty();
    }
}
