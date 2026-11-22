using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;

namespace Gum.Presentation.Tests.Plugins.TextureCoordinateSelectionPlugin;

// Where the Texture Coordinates tab draws a nine-slice's cut guides inside the selected region (#8354).
public class NineSliceGuidePositionsTests
{
    [Fact]
    public void Compute_EachSideUsesItsOwnCut_ThenTheSharedWidth_ThenAThird()
    {
        NineSliceGuidePositions positions = new NineSliceGuidePositions();

        // Region 10..40 wide, 20..80 tall.
        NineSliceGuideLines thirds = positions.Compute(10, 20, 40, 80, frameWidth: null, left: null, top: null, right: null, bottom: null);
        thirds.ShouldBe(new NineSliceGuideLines(Left: 20, Right: 30, Top: 40, Bottom: 60));

        NineSliceGuideLines shared = positions.Compute(10, 20, 40, 80, frameWidth: 4, left: null, top: null, right: null, bottom: null);
        shared.ShouldBe(new NineSliceGuideLines(Left: 14, Right: 36, Top: 24, Bottom: 76));

        NineSliceGuideLines perSide = positions.Compute(10, 20, 40, 80, frameWidth: 4, left: 6, top: 0, right: null, bottom: 8);
        perSide.ShouldBe(new NineSliceGuideLines(Left: 16, Right: 36, Top: 20, Bottom: 72));
    }
}
