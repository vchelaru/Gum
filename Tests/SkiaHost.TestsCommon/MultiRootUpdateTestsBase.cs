using Gum;
using Gum.Forms;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// The multi-root <c>Update(double, roots)</c> and <c>Update(double, root)</c> overloads on every
/// interactive Skia host, matching MonoGame's <c>Update(gameTime, roots)</c>: only the roots passed
/// animate and receive input. A host's test project derives from this class and supplies its concrete
/// service.
/// </summary>
public abstract class MultiRootUpdateTestsBase : IDisposable
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    /// <summary>The host's concrete <c>GumService.Default</c>.</summary>
    protected abstract IGumService ConcreteService { get; }

    private GumServiceSkiaInteractiveBase Service => (GumServiceSkiaInteractiveBase)ConcreteService;

    public void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
    }

    private class FrameRecorder : IUpdateEveryFrame
    {
        public int ActivityCount;
        public void Activity(double secondDifference) => ActivityCount++;
    }

    private static InteractiveGue CreateAnimatedRoot(out FrameRecorder recorder)
    {
        recorder = new FrameRecorder();
        InteractiveGue root = new InteractiveGue(new InvisibleRenderable());
        root.FormsControlAsObject = recorder;
        return root;
    }

    private static InteractiveGue CreateHoverTarget()
    {
        InteractiveGue target = new InteractiveGue(new InvisibleRenderable());
        target.X = 0;
        target.Y = 0;
        target.Width = 100;
        target.Height = 100;
        target.HasEvents = true;
        target.AddToManagers(SystemManagers.Default, null);
        return target;
    }

    private static Mock<ICursor> CreateCursorAt(int x, int y)
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        cursor.SetupAllProperties();
        cursor.SetupGet(c => c.X).Returns(x);
        cursor.SetupGet(c => c.Y).Returns(y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        return cursor;
    }

    [Fact]
    public void UpdateWithRoots_AnimatesOnlyThePassedRoots()
    {
        InteractiveGue passed = CreateAnimatedRoot(out FrameRecorder passedRecorder);
        InteractiveGue notPassed = CreateAnimatedRoot(out FrameRecorder notPassedRecorder);
        Service.Root.AddChild(notPassed);
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        try
        {
            Service.Update(1.0, new[] { passed });

            passedRecorder.ActivityCount.ShouldBe(1);
            notPassedRecorder.ActivityCount.ShouldBe(0);
        }
        finally
        {
            Service.Root.Children.Clear();
        }
    }

    [Fact]
    public void UpdateWithSingleRoot_AnimatesOnlyThatRoot()
    {
        InteractiveGue passed = CreateAnimatedRoot(out FrameRecorder passedRecorder);
        InteractiveGue notPassed = CreateAnimatedRoot(out FrameRecorder notPassedRecorder);
        Service.Root.AddChild(notPassed);
        FormsUtilities.SetCursor(new Mock<ICursor>().Object);

        try
        {
            Service.Update(1.0, (GraphicalUiElement)passed);

            passedRecorder.ActivityCount.ShouldBe(1);
            notPassedRecorder.ActivityCount.ShouldBe(0);
        }
        finally
        {
            Service.Root.Children.Clear();
        }
    }

    [Fact]
    public void UpdateWithRoots_PumpsInputToAControlUnderAPassedRoot()
    {
        InteractiveGue target = CreateHoverTarget();
        Mock<ICursor> cursor = CreateCursorAt(10, 10);
        FormsUtilities.SetCursor(cursor.Object);

        try
        {
            Service.Update(1.0, new GraphicalUiElement[] { target });

            cursor.Object.VisualOver.ShouldBeSameAs(target);
            cursor.Verify(c => c.Activity(1.0), Times.Once());
        }
        finally
        {
            target.RemoveFromManagers();
        }
    }

    [Fact]
    public void UpdateWithRoots_DoesNotPumpInputToAControlUnderARootNotPassed()
    {
        InteractiveGue target = CreateHoverTarget();
        InteractiveGue other = CreateAnimatedRoot(out _);
        Mock<ICursor> cursor = CreateCursorAt(10, 10);
        FormsUtilities.SetCursor(cursor.Object);

        try
        {
            Service.Update(1.0, new GraphicalUiElement[] { other });

            cursor.Object.VisualOver.ShouldBeNull();
        }
        finally
        {
            target.RemoveFromManagers();
        }
    }
}
