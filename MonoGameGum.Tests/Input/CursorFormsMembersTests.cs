using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Shouldly;
using Xunit;
using Cursor = Gum.Input.Cursor;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Pins the concrete <see cref="Cursor"/>'s FrameworkElement* and TransformMatrix members, which
/// <see cref="ICursor"/> also exposes (#6324).
/// </summary>
public class CursorFormsMembersTests
{
    [Fact]
    public void FrameworkElementOver_ShouldBeControlOfVisualOver()
    {
        Cursor cursor = new Cursor(null);
        Button button = new Button();

        cursor.VisualOver = button.Visual;

        cursor.FrameworkElementOver.ShouldBe(button);
    }

    [Fact]
    public void FrameworkElementPushed_ShouldBeControlOfVisualPushed()
    {
        Cursor cursor = new Cursor(null);
        Button button = new Button();

        cursor.VisualPushed = button.Visual;

        cursor.FrameworkElementPushed.ShouldBe(button);
    }

    [Fact]
    public void FrameworkElementRightPushed_ShouldBeControlOfVisualRightPushed()
    {
        Cursor cursor = new Cursor(null);
        Button button = new Button();

        cursor.VisualRightPushed = button.Visual;

        cursor.FrameworkElementRightPushed.ShouldBe(button);
    }

    [Fact]
    public void FrameworkElementMembers_WhenVisualsAreNull_ShouldBeNull()
    {
        Cursor cursor = new Cursor(null);

        cursor.FrameworkElementOver.ShouldBeNull();
        cursor.FrameworkElementPushed.ShouldBeNull();
        cursor.FrameworkElementRightPushed.ShouldBeNull();
    }

    [Fact]
    public void FrameworkElementMembers_WhenVisualHasNoControl_ShouldBeNull()
    {
        Cursor cursor = new Cursor(null);
        InteractiveGue plain = new InteractiveGue();

        cursor.VisualOver = plain;
        cursor.VisualPushed = plain;
        cursor.VisualRightPushed = plain;

        cursor.FrameworkElementOver.ShouldBeNull();
        cursor.FrameworkElementPushed.ShouldBeNull();
        cursor.FrameworkElementRightPushed.ShouldBeNull();
    }

    [Fact]
    public void TransformMatrix_ByDefault_ShouldBeIdentity()
    {
        Cursor cursor = new Cursor(null);

        cursor.TransformMatrix.ShouldBe(Matrix.Identity);
    }

    [Fact]
    public void Activity_ShouldApplyTransformMatrixToPosition()
    {
        Cursor cursor = new Cursor(null);
        cursor.TransformMatrix = Matrix.CreateScale(0.5f, 0.25f, 1) * Matrix.CreateTranslation(10, 20, 0);

        cursor.Activity(0, new MouseState(100, 80, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), new TouchCollection());

        cursor.X.ShouldBe(60);
        cursor.Y.ShouldBe(40);
    }

    [Fact]
    public void TransformMatrix_ThroughInterface_ByDefault_ShouldBeIdentity()
    {
        ICursor cursor = new Cursor(null);

        cursor.TransformMatrix.ShouldBe(System.Numerics.Matrix3x2.Identity);
    }

    [Fact]
    public void TransformMatrix_ThroughInterface_ShouldTransformPointsLikeTheXnaMatrix()
    {
        Cursor cursor = new Cursor(null);
        cursor.TransformMatrix = Matrix.CreateScale(2, 3, 1) * Matrix.CreateRotationZ(0.5f) * Matrix.CreateTranslation(10, 20, 0);
        ICursor asInterface = cursor;

        System.Numerics.Vector2 transformed = System.Numerics.Vector2.Transform(new System.Numerics.Vector2(5, 7), asInterface.TransformMatrix);

        XnaVector2 expected = XnaVector2.Transform(new XnaVector2(5, 7), cursor.TransformMatrix);
        transformed.X.ShouldBe(expected.X, 0.0001f);
        transformed.Y.ShouldBe(expected.Y, 0.0001f);
    }

    [Fact]
    public void TransformMatrix_ThroughInterface_ShouldFollowLaterChanges()
    {
        Cursor cursor = new Cursor(null);
        ICursor asInterface = cursor;

        cursor.TransformMatrix = Matrix.CreateTranslation(4, 9, 0);

        asInterface.TransformMatrix.Translation.ShouldBe(new System.Numerics.Vector2(4, 9));
    }

    [Fact]
    public void TransformMatrix_PublicProperty_ShouldKeepItsXnaMatrixType()
    {
        typeof(Cursor).GetProperty(nameof(Cursor.TransformMatrix))!.PropertyType.ShouldBe(typeof(Matrix));
    }
}
