using System.Collections.Generic;
using Gum.Converters;
using Shouldly;

namespace GumToolUnitTests.Converters;

/// <summary>
/// Pins <see cref="RatioResizeCalculator.ApplyResize"/>, the math for dragging a
/// <c>DimensionUnitType.Ratio</c>-typed Width/Height (#4395).
/// </summary>
public class RatioResizeCalculatorTests
{
    [Fact]
    public void ApplyResize_WithOneSibling_ShouldRedistributeComplementaryChangeToSibling()
    {
        // 100px-wide container, two Ratio=1 children -> 50px each. Shrink the dragged one by 1px.
        float draggedCurrentRatio = 1f;
        float draggedCurrentPixelSize = 50f;
        float pixelDelta = -1f;
        List<float> siblingRatios = new() { 1f };

        RatioResizeCalculator.ApplyResize(draggedCurrentRatio, draggedCurrentPixelSize, pixelDelta, siblingRatios,
            out float draggedNewRatio, out float[] siblingNewRatios);

        draggedNewRatio.ShouldBe(0.98f, tolerance: 0.0001f);
        siblingNewRatios.ShouldBe(new[] { 1.02f }, tolerance: 0.0001f);
    }

    [Fact]
    public void ApplyResize_WithNoSiblings_ShouldLeaveDraggedRatioUnchanged()
    {
        float draggedCurrentRatio = 1f;
        float draggedCurrentPixelSize = 100f;
        float pixelDelta = -1f;
        List<float> siblingRatios = new();

        RatioResizeCalculator.ApplyResize(draggedCurrentRatio, draggedCurrentPixelSize, pixelDelta, siblingRatios,
            out float draggedNewRatio, out float[] siblingNewRatios);

        draggedNewRatio.ShouldBe(1f);
        siblingNewRatios.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyResize_WithMultipleSiblings_ShouldRedistributeProportionally()
    {
        // 120px-wide container, ratios 2/1/3 (sum 6) -> pixel sizes 40/20/60. Grow the dragged one by 12px.
        float draggedCurrentRatio = 2f;
        float draggedCurrentPixelSize = 40f;
        float pixelDelta = 12f;
        List<float> siblingRatios = new() { 1f, 3f };

        RatioResizeCalculator.ApplyResize(draggedCurrentRatio, draggedCurrentPixelSize, pixelDelta, siblingRatios,
            out float draggedNewRatio, out float[] siblingNewRatios);

        draggedNewRatio.ShouldBe(2.6f, tolerance: 0.0001f);
        siblingNewRatios.ShouldBe(new[] { 0.85f, 2.55f }, tolerance: 0.0001f);
    }
}
