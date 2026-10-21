using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Input;

public class ICursorDefaultMembersTests
{
    private static Mock<ICursor> CreateMinimalCursor()
    {
        // CallBase makes Moq run the interface's default implementations, as a third-party
        // implementer that predates the members would get.
        return new Mock<ICursor> { CallBase = true };
    }

    [Fact]
    public void LastXAndLastY_ByDefault_ShouldBePositionMinusChange()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();
        cursor.Setup(x => x.X).Returns(110);
        cursor.Setup(x => x.XChange).Returns(10);
        cursor.Setup(x => x.Y).Returns(45);
        cursor.Setup(x => x.YChange).Returns(-5);

        cursor.Object.LastX.ShouldBe(100);
        cursor.Object.LastY.ShouldBe(50);
    }

    [Fact]
    public void VisualPushed_ByDefault_ShouldAliasWindowPushed()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();
        InteractiveGue pushed = new InteractiveGue();
        cursor.SetupProperty(x => x.WindowPushed);

        cursor.Object.VisualPushed = pushed;

        cursor.Object.WindowPushed.ShouldBe(pushed);
        cursor.Object.VisualPushed.ShouldBe(pushed);
    }

    [Fact]
    public void ClearInputValues_ByDefault_ShouldNotThrow()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();

        Should.NotThrow(() => cursor.Object.ClearInputValues());
    }

    [Fact]
    public void FrameworkElementOver_ByDefault_ShouldBeControlOfVisualOver()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();
        Button button = new Button();
        cursor.SetupProperty(x => x.VisualOver);
        cursor.Object.FrameworkElementOver.ShouldBeNull();

        cursor.Object.VisualOver = button.Visual;

        cursor.Object.FrameworkElementOver.ShouldBe(button);
    }

    [Fact]
    public void FrameworkElementPushed_ByDefault_ShouldBeControlOfVisualPushed()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();
        Button button = new Button();
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Object.FrameworkElementPushed.ShouldBeNull();

        cursor.Object.VisualPushed = button.Visual;

        cursor.Object.FrameworkElementPushed.ShouldBe(button);
    }

    [Fact]
    public void FrameworkElementRightPushed_ByDefault_ShouldBeControlOfVisualRightPushed()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();
        Button button = new Button();
        cursor.SetupProperty(x => x.VisualRightPushed);
        cursor.Object.FrameworkElementRightPushed.ShouldBeNull();

        cursor.Object.VisualRightPushed = button.Visual;

        cursor.Object.FrameworkElementRightPushed.ShouldBe(button);
    }

    [Fact]
    public void FrameworkElementMembers_ByDefault_WhenVisualHasNoControl_ShouldBeNull()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();
        InteractiveGue plain = new InteractiveGue();
        cursor.SetupProperty(x => x.VisualOver, plain);
        cursor.SetupProperty(x => x.WindowPushed, plain);
        cursor.SetupProperty(x => x.VisualRightPushed, plain);

        cursor.Object.FrameworkElementOver.ShouldBeNull();
        cursor.Object.FrameworkElementPushed.ShouldBeNull();
        cursor.Object.FrameworkElementRightPushed.ShouldBeNull();
    }

    [Fact]
    public void TransformMatrix_ByDefault_ShouldBeIdentity()
    {
        Mock<ICursor> cursor = CreateMinimalCursor();

        cursor.Object.TransformMatrix.ShouldBe(System.Numerics.Matrix3x2.Identity);
    }

    [Fact]
    public void Cursor_FrameworkElementMembers_ThroughInterface_ShouldMatchConcreteMembers()
    {
        Gum.Input.Cursor cursor = new Gum.Input.Cursor(null);
        Button over = new Button();
        Button pushed = new Button();
        Button rightPushed = new Button();
        cursor.VisualOver = over.Visual;
        cursor.VisualPushed = pushed.Visual;
        cursor.VisualRightPushed = rightPushed.Visual;
        ICursor asInterface = cursor;

        asInterface.FrameworkElementOver.ShouldBe(over);
        asInterface.FrameworkElementPushed.ShouldBe(pushed);
        asInterface.FrameworkElementRightPushed.ShouldBe(rightPushed);
    }

    [Fact]
    public void Cursor_ShouldImplementDefaultMembersWithItsOwnValues()
    {
        Gum.Input.Cursor cursor = new Gum.Input.Cursor(null);
        InteractiveGue pushed = new InteractiveGue();
        cursor.VisualPushed = pushed;
        ICursor asInterface = cursor;

        asInterface.VisualPushed.ShouldBe(pushed);
        asInterface.LastX.ShouldBe(cursor.LastX);
    }
}
