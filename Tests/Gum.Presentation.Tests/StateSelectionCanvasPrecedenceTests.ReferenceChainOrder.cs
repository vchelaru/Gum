using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

public partial class StateSelectionCanvasPrecedenceTests
{
    /// <summary>Discriminator for the chain case: the default state's chain, written in reverse order, on the default selection.</summary>
    [Fact]
    public void RefreshAll_ChainOfRowsInDefaultStateWrittenInReverseOrder_EvaluatesInDependencyOrder()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        AddReference(panel.DefaultState!, "Background", "Width = Mid.Width + 1");
        AddReference(panel.DefaultState!, "Mid", "Width = Reference.Width * 2");

        ShowWidth(panel, panel.DefaultState!).ShouldBe(101f);
    }

    /// <summary>A chain inside the selected state: Background reads Mid, and Mid reads Reference.</summary>
    [Fact]
    public void RefreshAll_ChainOfRowsInSelectedState_EvaluatesInDependencyOrder()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        StateSave alt = AddUncategorizedState(panel, "Alt", s =>
        {
            AddReference(s, "Background", "Width = Mid.Width + 1");
            AddReference(s, "Mid", "Width = Reference.Width * 2");
        });

        ShowWidth(panel, alt).ShouldBe(101f);
    }

    /// <summary>A chain read through an unset middle row: setting the tail moves the middle and the head.</summary>
    [Fact]
    public void RefreshAll_SelectedStateSetsTailOfChain_MiddleAndHeadFollow()
    {
        ComponentSave chain = BuildChain(rowA: true, rowB: true);
        StateSave state = AddCategoryState(chain, "Cat", "S", s => SetNumber(s, "C.Width", 33f));

        GraphicalUiElement root = Show(chain, state);

        (WidthOfChild(root, "A"), WidthOfChild(root, "B"), WidthOfChild(root, "C")).ShouldBe((33f, 33f, 33f));
    }
}
