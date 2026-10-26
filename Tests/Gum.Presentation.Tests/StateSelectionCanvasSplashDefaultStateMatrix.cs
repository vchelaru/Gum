using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

public partial class StateSelectionCanvasSplashTests
{
    /// <summary>The component's Default selection shows Disabled when the Forms variable or the authored category state says so.</summary>
    [Theory]
    [MemberData(nameof(DefaultStateGapCells))]
    public void RefreshAll_ComponentDefaultSelectedWhereLookShouldBeDisabled_ShowsDisabled(string view, string state, string isEnabled, string authored, bool defaultReference)
    {
        ShowPrecedenceCell(view, state, isEnabled, authored, defaultReference).ShouldBe(ButtonColors["Disabled"]);
    }
}
