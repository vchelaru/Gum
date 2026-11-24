using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// The ProgressBar component of every packaged theme, loaded from the theme files, wraps as a ProgressBar
/// and sizes its indicator to the share of the track that Value covers.
/// </summary>
public class ThemedProgressBarTests : FileThemeInputTestBase
{
    [Theory]
    [MemberData(nameof(AllThemes))]
    public void ProgressBar_Component_HasProgressBarBehaviorAndWrapsAsProgressBar(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ComponentSave? component = Find(project, "ProgressBar");
        component.ShouldNotBeNull($"{Label(theme)} has no ProgressBar component");
        component.Behaviors.Select(item => item.BehaviorName)
            .ShouldContain(StandardFormsBehaviorNames.ProgressBarBehaviorName);

        InteractiveGue visual = PlaceComponent(project, component);

        visual.FormsControlAsObject.ShouldBeOfType<ProgressBar>();
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void ProgressBar_Value_SizesIndicatorToShareOfTrack(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        InteractiveGue visual = PlaceComponent(project, Find(project, "ProgressBar")!);
        ProgressBar bar = (ProgressBar)visual.FormsControlAsObject!;
        visual.WidthUnits = DimensionUnitType.Absolute;
        visual.Width = 200;
        bar.Minimum = 0;
        bar.Maximum = 100;
        bar.Value = 25;
        Driver.Step(2);

        GraphicalUiElement track = visual.FindByName("TrackInstance")!;
        GraphicalUiElement indicator = visual.FindByName("IndicatorInstance")!;
        indicator.AbsoluteWidth.ShouldBe(track.AbsoluteWidth * 0.25f, 0.5f, Label(theme));
        indicator.AbsoluteLeft.ShouldBe(track.AbsoluteLeft, 0.5f, Label(theme));
        indicator.AbsoluteHeight.ShouldBeGreaterThan(0, Label(theme));
        track.ClipsChildren.ShouldBeTrue(Label(theme));
    }
}
