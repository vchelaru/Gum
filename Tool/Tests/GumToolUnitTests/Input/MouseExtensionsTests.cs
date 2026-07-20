using Gum.Input;
using Shouldly;
using WinFormsMouseButtons = System.Windows.Forms.MouseButtons;
using WinFormsMouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace GumToolUnitTests.Input;

public class MouseExtensionsTests : BaseTestClass
{
    [Fact]
    public void ToGumMouseButton_Left_MapsToLeft()
    {
        WinFormsMouseButtons.Left.ToGumMouseButton().ShouldBe(GumMouseButton.Left);
    }

    [Fact]
    public void ToGumMouseButton_Middle_MapsToMiddle()
    {
        WinFormsMouseButtons.Middle.ToGumMouseButton().ShouldBe(GumMouseButton.Middle);
    }

    [Fact]
    public void ToGumMouseButton_None_MapsToNone()
    {
        WinFormsMouseButtons.None.ToGumMouseButton().ShouldBe(GumMouseButton.None);
    }

    [Fact]
    public void ToGumMouseButton_Right_MapsToRight()
    {
        WinFormsMouseButtons.Right.ToGumMouseButton().ShouldBe(GumMouseButton.Right);
    }

    [Fact]
    public void ToGumMouseEventArgs_ExtractsPositionButtonAndDelta()
    {
        var original = new WinFormsMouseEventArgs(WinFormsMouseButtons.Middle, clicks: 1, x: 42, y: 17, delta: -120);

        GumMouseEventArgs neutral = original.ToGumMouseEventArgs();

        neutral.X.ShouldBe(42);
        neutral.Y.ShouldBe(17);
        neutral.Button.ShouldBe(GumMouseButton.Middle);
        neutral.Delta.ShouldBe(-120);
    }

    [Fact]
    public void ToGumMouseEventArgs_DoesNotMarkHandled()
    {
        // Handled is written by the handler and read back at the boundary; the initial conversion
        // must not pre-set it, or a handler that never touches it would look "handled" by default.
        var original = new WinFormsMouseEventArgs(WinFormsMouseButtons.Left, clicks: 1, x: 0, y: 0, delta: 0);

        GumMouseEventArgs neutral = original.ToGumMouseEventArgs();

        neutral.Handled.ShouldBeFalse();
    }
}
