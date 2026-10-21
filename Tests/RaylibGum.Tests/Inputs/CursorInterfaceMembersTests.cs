using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using System.Numerics;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Pins the shared <see cref="Cursor"/>'s FrameworkElement* and TransformMatrix members, and that
/// <see cref="ICursor"/> exposes the same values (#6324). On this host TransformMatrix is already a
/// <see cref="Matrix3x2"/>, so the public property implements the interface member directly.
/// </summary>
public class CursorInterfaceMembersTests : BaseTestClass
{
    [Fact]
    public void FrameworkElementMembers_ShouldBeControlsOfTheVisuals()
    {
        Cursor cursor = new Cursor();
        Button over = new Button();
        Button pushed = new Button();
        Button rightPushed = new Button();
        cursor.VisualOver = over.Visual;
        cursor.VisualPushed = pushed.Visual;
        cursor.VisualRightPushed = rightPushed.Visual;
        ICursor asInterface = cursor;

        cursor.FrameworkElementOver.ShouldBe(over);
        cursor.FrameworkElementPushed.ShouldBe(pushed);
        cursor.FrameworkElementRightPushed.ShouldBe(rightPushed);
        asInterface.FrameworkElementOver.ShouldBe(over);
        asInterface.FrameworkElementPushed.ShouldBe(pushed);
        asInterface.FrameworkElementRightPushed.ShouldBe(rightPushed);
    }

    [Fact]
    public void FrameworkElementMembers_WhenVisualHasNoControl_ShouldBeNull()
    {
        Cursor cursor = new Cursor();
        InteractiveGue plain = new InteractiveGue();
        cursor.VisualOver = plain;
        cursor.VisualPushed = plain;
        cursor.VisualRightPushed = plain;
        ICursor asInterface = cursor;

        asInterface.FrameworkElementOver.ShouldBeNull();
        asInterface.FrameworkElementPushed.ShouldBeNull();
        asInterface.FrameworkElementRightPushed.ShouldBeNull();
    }

    [Fact]
    public void TransformMatrix_ByDefault_ShouldBeIdentity()
    {
        Cursor cursor = new Cursor();
        ICursor asInterface = cursor;

        cursor.TransformMatrix.ShouldBe(Matrix3x2.Identity);
        asInterface.TransformMatrix.ShouldBe(Matrix3x2.Identity);
    }

    [Fact]
    public void TransformMatrix_ThroughInterface_ShouldFollowTheConcreteProperty()
    {
        Cursor cursor = new Cursor();
        ICursor asInterface = cursor;
        Matrix3x2 matrix = Matrix3x2.CreateScale(2, 3) * Matrix3x2.CreateTranslation(4, 9);

        cursor.TransformMatrix = matrix;

        asInterface.TransformMatrix.ShouldBe(matrix);
    }
}
