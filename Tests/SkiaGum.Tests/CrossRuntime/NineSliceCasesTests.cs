using System.Globalization;
using CrossRuntimeTests;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.CrossRuntime;

/// <summary>
/// The case names key <c>NineSliceCrossRuntimeTests.KnownDifferences</c> and name the baseline files'
/// cells, so they must read the same on every machine.
/// </summary>
public class NineSliceCasesTests
{
    [Fact]
    public void CaseNames_UseTheInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            // Guards the test itself: de-DE must really write a comma, or the check below proves nothing.
            1.5f.ToString().ShouldBe("1,5");

            IReadOnlyList<NineSliceSheet> sheets = NineSliceCases.BuildSheets();

            sheets.Single(sheet => sheet.Name == "fractional").Cases.Select(c => c.Name).ShouldContain("3x3-f1-1.5x2.5");
            sheets.Single(sheet => sheet.Name == "zoom").Cases.Select(c => c.Name).ShouldContain("6x6-f2-3x3-z1.5");
            sheets.SelectMany(sheet => sheet.Cases).Select(c => c.Name).ShouldAllBe(name => !name.Contains(','));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
