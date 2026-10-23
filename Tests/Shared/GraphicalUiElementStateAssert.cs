#nullable enable
using System;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Tests.Shared;

/// <summary>
/// The check each test project's base-class tests run against its own base class: a test that changes
/// the <see cref="GraphicalUiElementState"/> settings must not leave them changed for the next test.
/// </summary>
public static class GraphicalUiElementStateAssert
{
    /// <param name="startTest">Constructs the base class, as xUnit does at the start of a test.</param>
    /// <param name="checkCanvas">False for a base whose Dispose sets the canvas to fixed values, so
    /// the canvas check could not fail without the restore.</param>
    public static void DisposeRestores(Func<IDisposable> startTest, bool checkCanvas = true)
    {
        GraphicalUiElementState before = GraphicalUiElementState.Capture();
        try
        {
            IDisposable test = startTest();
            // Read after construction: a base class may set these to fixed values on the way in.
            MissingFileBehavior missingFileBehavior = GraphicalUiElement.MissingFileBehavior;
            float canvasWidth = GraphicalUiElement.CanvasWidth;
            float canvasHeight = GraphicalUiElement.CanvasHeight;

            GraphicalUiElement.MissingFileBehavior = missingFileBehavior == MissingFileBehavior.ThrowException
                ? MissingFileBehavior.ConsumeSilently
                : MissingFileBehavior.ThrowException;
            GraphicalUiElement.CanvasWidth = canvasWidth + 123;
            GraphicalUiElement.CanvasHeight = canvasHeight + 45;

            test.Dispose();

            GraphicalUiElement.MissingFileBehavior.ShouldBe(missingFileBehavior);
            if (checkCanvas)
            {
                GraphicalUiElement.CanvasWidth.ShouldBe(canvasWidth);
                GraphicalUiElement.CanvasHeight.ShouldBe(canvasHeight);
            }
        }
        finally
        {
            before.Restore();
        }
    }
}
