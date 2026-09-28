using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ProjectLoadFillsTests
{
    [Fact]
    public void Apply_FillsMissingCanvasSizes_AndLeavesAProjectsOwnSizesAlone()
    {
        ProjectLoadFills fills = new ProjectLoadFills();
        GumProjectSave withoutSizes = new GumProjectSave { CustomCanvasSizes = new List<CustomCanvasSize>() };
        CustomCanvasSize ownSize = new CustomCanvasSize { FriendlyName = "Phone", Width = 390, Height = 844 };
        GumProjectSave withSizes = new GumProjectSave { CustomCanvasSizes = new List<CustomCanvasSize> { ownSize } };

        fills.Apply(withoutSizes).ShouldBeTrue();
        fills.Apply(withSizes).ShouldBeFalse();

        withoutSizes.CustomCanvasSizes!.Select(size => size.FriendlyName)
            .ShouldBe(new[] { "Project Default", "480p", "720p", "Steam Deck", "1080p", "4k" });
        withSizes.CustomCanvasSizes!.ShouldBe(new[] { ownSize });
    }
}
