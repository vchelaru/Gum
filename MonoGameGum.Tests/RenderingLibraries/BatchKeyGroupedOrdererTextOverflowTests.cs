using System.Collections.Generic;
using Gum.DataTypes;
using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

// SPIKE #8116 (sort3) repro: a Text whose wrapped lines spill past its layout Height draws over the next sibling's
// background in DFS order. BatchKeyGroupedOrderer tests overlap with GetAbsoluteBounds (layout bounds), sees no
// overlap, keeps the same-texture backgrounds together, and the spilled lines land on top of the second background.
public class BatchKeyGroupedOrdererTextOverflowTests : BaseTestClass
{
    [Fact]
    public void BuildDrawList_TextSpillingIntoNextCell_StaysUnderTheNextBackground()
    {
        object bgTexture = new object();
        OrdererFake first = new("bg0") { X = 0, Y = 0, Width = 100, Height = 30, BatchSortKey = bgTexture };
        TextRuntime text = new()
        {
            X = 4, Y = 4, Width = 90, Height = 8,
            WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute,
            Text = "line one\nline two\nline three\nline four",
        };
        OrdererFake second = new("bg1") { X = 0, Y = 30, Width = 100, Height = 30, BatchSortKey = bgTexture };

        // Precondition: the layout box ends at y=12, the drawn lines reach past y=30 into the second background.
        (text.GetAbsoluteTop() + text.GetAbsoluteHeight()).ShouldBeLessThan(30f);
        (text.GetAbsoluteTop() + ((Text)text.RenderableComponent).WrappedTextHeight).ShouldBeGreaterThan(30f);

        List<DrawCommand> commands = new();
        new BatchKeyGroupedOrderer().BuildDrawList(BuildLayer(new IRenderableIpso[] { first, text, second }), commands);

        int textIndex = commands.FindIndex(c => ReferenceEquals(c.Target, text));
        int secondIndex = commands.FindIndex(c => ReferenceEquals(c.Target, second));
        textIndex.ShouldBeLessThan(secondIndex, "the spilled lines must draw under the next background, as in DFS order");
    }
}
