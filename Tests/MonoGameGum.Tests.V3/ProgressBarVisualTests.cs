using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3;

public class ProgressBarVisualTests : BaseTestClass
{
    [Fact]
    public void Constructor_ShouldCreateTrackAndIndicator()
    {
        ProgressBarVisual visual = new();

        visual.TrackInstance.Name.ShouldBe("TrackInstance");
        visual.IndicatorInstance.Name.ShouldBe("IndicatorInstance");
        visual.IndicatorInstance.Parent.ShouldBe(visual.TrackInstance);
        visual.FormsControl.ShouldBeOfType<ProgressBar>();
    }

    [Fact]
    public void Constructor_ShouldSetIndicatorWidthByPercentage()
    {
        ProgressBarVisual visual = new();

        visual.FormsControl.Value = 35;

        visual.IndicatorInstance.WidthUnits.ShouldBe(DimensionUnitType.PercentageOfParent);
        visual.IndicatorInstance.Width.ShouldBe(35);
    }

    [Fact]
    public void Constructor_ShouldColorIndicatorWithPrimary()
    {
        ProgressBarVisual visual = new();

        visual.IndicatorInstance.Color.ShouldBe(Styling.ActiveStyle.Colors.Primary);
    }

    [Fact]
    public void Visual_ShouldNotHaveEvents()
    {
        ProgressBarVisual visual = new();

        visual.HasEvents.ShouldBeFalse();
    }

    [Fact]
    public void DefaultFormsTemplates_ShouldCreateProgressBarVisual()
    {
        ProgressBar progressBar = new();

        progressBar.Visual.ShouldBeOfType<ProgressBarVisual>();
    }

    [Fact]
    public void IsIndeterminate_ShouldPlayAnimation_AndMoveIndicator()
    {
        ProgressBarVisual visual = new();
        visual.FormsControl.Value = 50;

        visual.FormsControl.IsIndeterminate = true;

        visual.AnimationController.IsPlaying.ShouldBeTrue();
        float indicatorWidth = visual.IndicatorInstance.Width;
        visual.AnimateSelf(0.1);
        float firstX = visual.IndicatorInstance.X;
        visual.AnimateSelf(0.5);
        visual.IndicatorInstance.X.ShouldBeGreaterThan(firstX);
        visual.IndicatorInstance.Width.ShouldBe(indicatorWidth);
    }

    [Fact]
    public void IsIndeterminate_ShouldStopAnimation_AndRestoreValueWidth_WhenSetBackToFalse()
    {
        ProgressBarVisual visual = new();
        visual.FormsControl.Value = 50;
        visual.FormsControl.IsIndeterminate = true;
        visual.AnimateSelf(0.4);

        visual.FormsControl.IsIndeterminate = false;

        visual.AnimationController.IsPlaying.ShouldBeFalse();
        visual.IndicatorInstance.X.ShouldBe(0);
        visual.IndicatorInstance.Width.ShouldBe(50);
    }
}
