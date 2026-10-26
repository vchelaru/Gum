using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

public partial class StateSelectionCanvasSplashTests
{
    /// <summary>The clicked state wins when it sets both the left and the right side of a default row.</summary>
    [Fact]
    public void RefreshAll_SelectedStateSetsBothSidesOfDefaultRow_LeftKeepsItsOwnValue()
    {
        ComponentSave chain = BuildChain(rowA: false, rowB: true);
        StateSave state = AddCategoryState(chain, "Cat", "S", s =>
        {
            SetNumber(s, "B.Width", 102f);
            SetNumber(s, "C.Width", 103f);
        });

        GraphicalUiElement root = Show(chain, state);

        (WidthOfChild(root, "B"), WidthOfChild(root, "C")).ShouldBe((102f, 103f));
    }

    /// <summary>Every default-row and set combination where the state sets a variable and what its row reads: the state's value wins.</summary>
    [Fact]
    public void RefreshAll_StateSetsBothEndsOfRowsInAChain_SetVariablesKeepStatesValues()
    {
        for (int bits = 0; bits < 32; bits++)
        {
            bool rowA = (bits & 1) != 0, rowB = (bits & 2) != 0, setA = (bits & 4) != 0, setB = (bits & 8) != 0, setC = (bits & 16) != 0;
            if (!SetsBothEndsOfARow(rowA, rowB, setA, setB, setC))
            {
                continue;
            }

            ComponentSave chain = BuildChain(rowA, rowB, a: rowA ? 30f : 10f, b: rowB ? 30f : 20f, c: 30f);
            StateSave state = AddCategoryState(chain, "Cat", "S", s =>
            {
                if (setA) SetNumber(s, "A.Width", 101f);
                if (setB) SetNumber(s, "B.Width", 102f);
                if (setC) SetNumber(s, "C.Width", 103f);
            });

            GraphicalUiElement root = Show(chain, state);

            string tag = $"rowA={rowA} rowB={rowB} setA={setA} setB={setB} setC={setC}";
            if (setA) WidthOfChild(root, "A").ShouldBe(101f, tag);
            if (setB) WidthOfChild(root, "B").ShouldBe(102f, tag);
            if (setC) WidthOfChild(root, "C").ShouldBe(103f, tag);
        }
    }
}
