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
