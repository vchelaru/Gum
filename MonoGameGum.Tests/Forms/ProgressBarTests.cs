using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class ProgressBarTests : BaseTestClass
{
    // A visual with only the parts ProgressBar looks up, so these tests don't depend on the V3 visual.
    private static ContainerRuntime CreateVisual(out ContainerRuntime indicator)
    {
        ContainerRuntime visual = new() { Width = 200 };
        ContainerRuntime track = new() { Name = "TrackInstance", Width = 0, WidthUnits = DimensionUnitType.RelativeToParent };
        visual.AddChild(track);
        indicator = new ContainerRuntime { Name = "IndicatorInstance" };
        track.AddChild(indicator);
        return visual;
    }

    private static ProgressBar CreateProgressBar(out ContainerRuntime indicator)
    {
        return new ProgressBar(CreateVisual(out indicator));
    }

    [Fact]
    public void Constructor_ShouldDefaultToZeroThroughOneHundred()
    {
        ProgressBar progressBar = CreateProgressBar(out _);

        progressBar.Minimum.ShouldBe(0);
        progressBar.Maximum.ShouldBe(100);
        progressBar.Value.ShouldBe(0);
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenVisualHasNoParts()
    {
        ProgressBar progressBar = new(new ContainerRuntime());

        progressBar.Value = 50;

        progressBar.Value.ShouldBe(50);
    }

    [Fact]
    public void Value_ShouldClamp()
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime indicator);

        progressBar.Value = 150;
        progressBar.Value.ShouldBe(100);
        indicator.Width.ShouldBe(100);

        progressBar.Value = -10;
        progressBar.Value.ShouldBe(0);
        indicator.Width.ShouldBe(0);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(35, 35)]
    [InlineData(100, 100)]
    public void Value_ShouldSetIndicatorWidthPercentage(double value, float expectedWidth)
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime indicator);

        progressBar.Value = value;

        indicator.WidthUnits.ShouldBe(DimensionUnitType.PercentageOfParent);
        indicator.Width.ShouldBe(expectedWidth);
    }

    [Fact]
    public void Value_ShouldSetIndicatorWidthRelativeToMinimum_WhenMinimumIsNotZero()
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime indicator);
        progressBar.Minimum = 50;
        progressBar.Maximum = 150;

        progressBar.Value = 75;

        indicator.Width.ShouldBe(25);
    }

    [Fact]
    public void Maximum_ShouldUpdateIndicatorWidth()
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime indicator);
        progressBar.Value = 50;

        progressBar.Maximum = 200;

        indicator.Width.ShouldBe(25);
    }

    [Fact]
    public void Minimum_ShouldUpdateIndicatorWidth()
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime indicator);
        progressBar.Value = 50;

        progressBar.Minimum = 25;

        indicator.Width.ShouldBe(100 * (50 - 25) / 75f, tolerance: 0.001f);
    }

    [Fact]
    public void Value_ShouldSetIndicatorWidthToZero_WhenMaximumEqualsMinimum()
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime indicator);
        progressBar.Maximum = 0;

        progressBar.Value = 0;

        progressBar.Value.ShouldBe(0);
        float.IsNaN(indicator.Width).ShouldBeFalse();
        indicator.Width.ShouldBe(0);
    }

    [Fact]
    public void Value_ShouldUpdateNewIndicator_AfterVisualIsReassigned()
    {
        ProgressBar progressBar = CreateProgressBar(out ContainerRuntime oldIndicator);
        progressBar.Value = 20;

        progressBar.Visual = CreateVisual(out ContainerRuntime newIndicator);
        progressBar.Value = 60;

        newIndicator.Width.ShouldBe(60);
        oldIndicator.Width.ShouldBe(20);
    }
}
