using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gum.PropertyGridHelpers.Converters;
using Gum.Services.Fonts;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

public class FontTypeConverterTests
{
    [Fact]
    public void GetStandardValues_ListsTheProvidersFamilies_AsAnExclusiveDropDown()
    {
        Mock<IInstalledFontProvider> provider = new Mock<IInstalledFontProvider>();
        provider.Setup(p => p.GetInstalledFontFamilyNames()).Returns(new List<string> { "Arial", "Consolas" });
        FontTypeConverter converter = new FontTypeConverter(provider.Object);

        List<object?> values = converter.GetStandardValues()!.Cast<object?>().ToList();

        values.ShouldBe(new object?[] { "Arial", "Consolas" });
        converter.GetStandardValuesSupported().ShouldBeTrue();
        converter.GetStandardValuesExclusive().ShouldBeTrue();
    }

    [Fact]
    public void SkiaInstalledFontProvider_ReturnsSortedDistinctNames()
    {
        IReadOnlyList<string> names = new SkiaInstalledFontProvider().GetInstalledFontFamilyNames();

        names.ShouldBe(names.Distinct().OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase));
        names.ShouldAllBe(name => !string.IsNullOrWhiteSpace(name));
    }
}
