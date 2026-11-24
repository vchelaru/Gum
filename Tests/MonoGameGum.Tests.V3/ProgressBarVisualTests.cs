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
    public void TrackInstance_ShouldClipChildren()
    {
        // A NineSlice cannot clip children, so the track is a container.
        ProgressBarVisual visual = new();

        visual.TrackInstance.ClipsChildren.ShouldBeTrue();
        visual.TrackBackground.Parent.ShouldBe(visual.TrackInstance);
        visual.TrackBackground.Color.ShouldBe(Styling.ActiveStyle.Colors.InputBackground);
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
}
