using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// What auto-sized Skia Texts in a stack allocate per frame (Update + Draw) while layout moves them or changes
/// the stack's width (#7865). Layout measures an auto-sized Text with no width limit and then sets its Width to the
/// measured width; the Text keeps its shaped block through that, so a layout that changes neither the text nor the
/// wrapping reshapes nothing.
/// </summary>
public class TextStackLayoutAllocationTests : BaseTestClass
{
    private const int TextCount = 20;

    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();

    public TextStackLayoutAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public override void Dispose()
    {
        _host.Dispose();
        base.Dispose();
    }

    private sealed class Stack
    {
        public ContainerRuntime Container = null!;
        public ContainerRuntime Spacer = null!;
        public TextRuntime[] Texts = null!;
    }

    private Stack BuildStack(HorizontalAlignment alignment, bool plainContainers = false)
    {
        Stack stack = new Stack();
        stack.Container = new ContainerRuntime();
        stack.Container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Container.Width = 300;
        stack.Container.WidthUnits = DimensionUnitType.Absolute;
        stack.Container.Height = 0;
        stack.Container.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Container.AddToRoot();

        stack.Spacer = new ContainerRuntime();
        stack.Spacer.Width = 10;
        stack.Spacer.Height = 10;
        stack.Container.AddChild(stack.Spacer);

        stack.Texts = new TextRuntime[TextCount];
        for (int i = 0; i < TextCount; i++)
        {
            if (plainContainers)
            {
                ContainerRuntime item = new ContainerRuntime { Width = 100, Height = 16 };
                stack.Container.AddChild(item);
                continue;
            }

            TextRuntime text = new TextRuntime();
            text.Text = "Item number " + i + " of the list";
            text.HorizontalAlignment = alignment;
            stack.Container.AddChild(text);
            stack.Texts[i] = text;
        }

        stack.Container.UpdateLayout();
        // Two frames, so the first draw's recording and any first-use state are behind the measurement.
        _host.Update();
        _host.Draw();
        _host.Update();
        _host.Draw();
        return stack;
    }

    private AllocationResult MeasureFrames(Action<int> change, out int layoutCalls)
    {
        const int warmup = 60;
        const int measured = 300;
        int before = GraphicalUiElement.UpdateLayoutCallCount;

        AllocationResult result = SkiaAllocationMeasurer.MeasureMinimum(
            frame =>
            {
                change(frame);
                _host.Update();
                _host.Draw();
            },
            attempts: 3,
            warmupIterations: warmup,
            measuredIterations: measured);

        layoutCalls = GraphicalUiElement.UpdateLayoutCallCount - before;
        return result;
    }

    // The action ran and laid out on every frame of every attempt, so a low figure is not a stack that stopped changing.
    private static void LayoutShouldHaveRunEveryFrame(int layoutCalls)
    {
        layoutCalls.ShouldBeGreaterThanOrEqualTo(3 * (360 + 60 + 300));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void SpacerHeightChange_AutoSizedTexts_DoNotReshape(HorizontalAlignment alignment)
    {
        Stack stack = BuildStack(alignment);
        float top = stack.Texts[0].AbsoluteTop;

        AllocationResult result = MeasureFrames(frame => stack.Spacer.Height = 10 + (frame % 2), out int layoutCalls);

        _output.WriteLine($"{alignment}, spacer Height change, {TextCount} Texts: {result.BytesPerIteration:N0} bytes/frame");
        LayoutShouldHaveRunEveryFrame(layoutCalls);
        stack.Texts[0].AbsoluteTop.ShouldNotBe(top, "the spacer moved the Texts");
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void StackWidthChange_AutoSizedTexts_DoNotReshape(HorizontalAlignment alignment)
    {
        Stack stack = BuildStack(alignment);

        AllocationResult result = MeasureFrames(frame => stack.Container.Width = 300 + (frame % 2), out int layoutCalls);

        _output.WriteLine($"{alignment}, stack Width change, {TextCount} Texts: {result.BytesPerIteration:N0} bytes/frame");
        LayoutShouldHaveRunEveryFrame(layoutCalls);
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void LoneText_YToggle_DoesNotReshape()
    {
        TextRuntime text = new TextRuntime { Text = "Hello lone text" };
        text.AddToRoot();
        _host.Update();
        _host.Draw();

        AllocationResult result = MeasureFrames(frame => text.Y = frame % 2, out int layoutCalls);

        _output.WriteLine($"Lone Text Y toggle: {result.BytesPerIteration:N0} bytes/frame");
        layoutCalls.ShouldBeGreaterThan(0);
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void LoneText_XToggle_DoesNotReshape()
    {
        TextRuntime text = new TextRuntime { Text = "Hello lone text" };
        text.AddToRoot();
        _host.Update();
        _host.Draw();

        AllocationResult result = MeasureFrames(frame => text.X = frame % 2, out _);

        _output.WriteLine($"Lone Text X toggle: {result.BytesPerIteration:N0} bytes/frame");
        text.AbsoluteLeft.ShouldBe(text.X, "X moved the Text");
        result.TotalBytes.ShouldBe(0);
    }

    // The control: containers of the same size in the same stack allocate nothing, so anything above is the Texts.
    [Fact]
    public void SpacerHeightChange_PlainContainers_AreZero()
    {
        Stack stack = BuildStack(HorizontalAlignment.Left, plainContainers: true);

        AllocationResult result = MeasureFrames(frame => stack.Spacer.Height = 10 + (frame % 2), out int layoutCalls);

        _output.WriteLine($"Spacer Height change, {TextCount} plain containers: {result.BytesPerIteration:N0} bytes/frame");
        LayoutShouldHaveRunEveryFrame(layoutCalls);
        result.TotalBytes.ShouldBe(0);
    }

    // A changed Text still shapes itself, and pays for nothing else in the stack.
    [Fact]
    public void SiblingTextChange_OnlyTheChangedTextPays()
    {
        Stack stack = BuildStack(HorizontalAlignment.Left);

        AllocationResult result = MeasureFrames(
            frame => stack.Texts[0].Text = frame % 2 == 0 ? "Item number 0 of the list" : "Item number 1 of the list",
            out int layoutCalls);

        _output.WriteLine($"Item 0 Text change, {TextCount} Texts: {result.BytesPerIteration:N0} bytes/frame");
        LayoutShouldHaveRunEveryFrame(layoutCalls);
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        // One Text reshaped per frame: 2,520 bytes measured on macOS, Debug. The 19 other Texts reshaping
        // would add about 48,000.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(4000);
    }
}
