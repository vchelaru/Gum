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
}
