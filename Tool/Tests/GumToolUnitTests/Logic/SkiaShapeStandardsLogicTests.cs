using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Logic;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Logic;

public class SkiaShapeStandardsLogicTests
{
    private const int OlderThanV3 = (int)GumProjectSave.GumxVersions.AttributeVersion;
    private const int V3 = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

    [Fact]
    public void GetStandardNamesToAdd_IncludesLegacyShapes_OnPreV3Project()
    {
        IReadOnlyList<string> names = SkiaShapeStandardsLogic.GetStandardNamesToAdd(OlderThanV3);

        names.ShouldBe(new[]
        {
            "Arc", "Canvas", "ColoredCircle", "Line", "LottieAnimation", "RoundedRectangle", "Svg",
        });
    }

    [Fact]
    public void GetStandardNamesToAdd_OmitsLegacyShapes_OnV3Project()
    {
        IReadOnlyList<string> names = SkiaShapeStandardsLogic.GetStandardNamesToAdd(V3);

        names.ShouldNotContain("ColoredCircle");
        names.ShouldNotContain("RoundedRectangle");
    }

    [Fact]
    public void GetStandardNamesToAdd_AlwaysIncludesNonLegacyShapes()
    {
        IReadOnlyList<string> preV3 = SkiaShapeStandardsLogic.GetStandardNamesToAdd(OlderThanV3);
        IReadOnlyList<string> v3 = SkiaShapeStandardsLogic.GetStandardNamesToAdd(V3);

        foreach (string name in new[] { "Arc", "Canvas", "Line", "LottieAnimation", "Svg" })
        {
            preV3.ShouldContain(name);
            v3.ShouldContain(name);
        }
    }
}
