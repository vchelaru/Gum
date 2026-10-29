using System.Collections.Generic;
using System.Linq;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #7032: sibling component instances (a button is a free container holding a background and
/// a text on another texture) must merge into one run per texture, not alternate
/// background/text/background/text.
/// </summary>
public class BatchKeyGroupedOrdererSiblingMergeTests : BaseTestClass
{
    private static readonly object BgTexture = new object();
    private static readonly object FontTexture = new object();

    /// <summary>A button: free-container root covering a background and a text drawn over it.</summary>
    private static OrdererFake AddButton(OrdererFake parent, int index, float y)
    {
        OrdererFake button = AddChild(parent, $"btn{index}", "", 0, y, 100, 20);
        AddChild(button, $"bg{index}", Sprites, 0, 0, 100, 20, BgTexture);
        AddChild(button, $"text{index}", Sprites, 10, 4, 60, 12, FontTexture);
        return button;
    }

    private static OrdererFake BuildStack(int buttonCount, bool withPanelBackground)
    {
        OrdererFake stack = new OrdererFake("stack", "") { X = 0, Y = 0, Width = 100, Height = buttonCount * 24 };
        if (withPanelBackground)
        {
            AddChild(stack, "panelBg", Sprites, 0, 0, 100, buttonCount * 24, BgTexture);
        }
        for (int i = 0; i < buttonCount; i++)
        {
            AddButton(stack, i, i * 24);
        }
        return stack;
    }

    /// <summary>Number of times the (BatchKey, BatchSortKey) changes between consecutive real draws.</summary>
    private static int CountKeyRuns(List<DrawCommand> commands)
    {
        int runs = 0;
        string? key = null;
        object? sortKey = null;
        foreach (DrawCommand command in commands)
        {
            OrdererFake item = (OrdererFake)command.Target;
            if (command.Kind != DrawCommandKind.DrawRenderable || item.BatchKey.Length == 0)
            {
                continue;
            }
            if (key == null || key != item.BatchKey || !Equals(sortKey, item.BatchSortKey))
            {
                runs++;
            }
            key = item.BatchKey;
            sortKey = item.BatchSortKey;
        }
        return runs;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildDrawList_StackOfButtons_DrawsAllBackgroundsThenAllTexts(bool withPanelBackground)
    {
        Layer layer = BuildLayer(new IRenderableIpso[] { BuildStack(6, withPanelBackground) });
        List<DrawCommand> commands = new List<DrawCommand>();

        new BatchKeyGroupedOrderer().BuildDrawList(layer, commands);

        List<string> real = Describe(commands)
            .Select(d => d.Replace("DrawRenderable:", ""))
            .Where(n => n.StartsWith("bg") || n.StartsWith("text") || n == "panelBg")
            .ToList();
        List<string> expected = new List<string>();
        if (withPanelBackground) { expected.Add("panelBg"); }
        expected.AddRange(Enumerable.Range(0, 6).Select(i => $"bg{i}"));
        expected.AddRange(Enumerable.Range(0, 6).Select(i => $"text{i}"));
        real.ShouldBe(expected);
    }

    [Fact]
    public void BuildDrawList_StackOfButtons_SwitchesTextureFewerTimesThanHierarchical()
    {
        Layer hierarchicalLayer = BuildLayer(new IRenderableIpso[] { BuildStack(6, true) });
        Layer groupedLayer = BuildLayer(new IRenderableIpso[] { BuildStack(6, true) });
        List<DrawCommand> hierarchical = new List<DrawCommand>();
        List<DrawCommand> grouped = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(hierarchicalLayer, hierarchical);
        new BatchKeyGroupedOrderer().BuildDrawList(groupedLayer, grouped);

        CountKeyRuns(hierarchical).ShouldBe(12);
        CountKeyRuns(grouped).ShouldBe(2);
    }
}
