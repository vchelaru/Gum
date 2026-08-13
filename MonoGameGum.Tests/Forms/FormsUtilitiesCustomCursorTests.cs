using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

// Covers issue #4442: a cursor set directly via ICursor.CustomCursor (e.g. a game calling
// Mouse.SetCursor / GumUI.Cursor.CustomCursor outside of any FrameworkElement.CustomCursor)
// was being reset to Cursors.Arrow every time the hovered FrameworkElement changed, even when
// neither the old nor the new element had a CustomCursor of its own -- e.g. simply hovering
// onto or off of a plain Button.
public class FormsUtilitiesCustomCursorTests : BaseTestClass
{
    [Fact]
    public void Update_ShouldNotResetCustomCursor_WhenHoveringOntoElementWithNoCustomCursorOfItsOwn()
    {
        Mock<ICursor> cursor = CreateMockCursorWithPosition(x: 500, y: 500);

        Button button = new();
        button.AddToRoot();
        button.X = 0;
        button.Y = 0;
        button.Width = 100;
        button.Height = 100;
        button.Visual.WidthUnits = DimensionUnitType.Absolute;
        button.Visual.HeightUnits = DimensionUnitType.Absolute;

        GumService.Default.Update(new GameTime());

        // Simulates a game setting the cursor directly (e.g. Mouse.SetCursor), independent of
        // any FrameworkElement.CustomCursor.
        cursor.Object.CustomCursor = Cursors.SizeWE;

        cursor.Setup(c => c.X).Returns(10);
        cursor.Setup(c => c.Y).Returns(10);

        GumService.Default.Update(new GameTime());

        cursor.Object.CustomCursor.ShouldBe(Cursors.SizeWE);
    }

    [Fact]
    public void Update_ShouldRevertToArrow_WhenLeavingElementThatHadCustomCursor()
    {
        Mock<ICursor> cursor = CreateMockCursorWithPosition(x: 500, y: 500);

        FrameworkElement withCustomCursor = new(new ContainerRuntime());
        withCustomCursor.Visual.AddToRoot();
        withCustomCursor.Visual.Click += (_, _) => { };
        withCustomCursor.X = 0;
        withCustomCursor.Y = 0;
        withCustomCursor.Width = 100;
        withCustomCursor.Height = 100;
        withCustomCursor.Visual.WidthUnits = DimensionUnitType.Absolute;
        withCustomCursor.Visual.HeightUnits = DimensionUnitType.Absolute;
        withCustomCursor.CustomCursor = Cursors.SizeWE;

        cursor.Setup(c => c.X).Returns(10);
        cursor.Setup(c => c.Y).Returns(10);
        GumService.Default.Update(new GameTime());

        cursor.Object.CustomCursor.ShouldBe(Cursors.SizeWE);

        cursor.Setup(c => c.X).Returns(500);
        cursor.Setup(c => c.Y).Returns(500);
        GumService.Default.Update(new GameTime());

        cursor.Object.CustomCursor.ShouldBe(Cursors.Arrow);
    }

    private static Mock<ICursor> CreateMockCursorWithPosition(int x, int y)
    {
        Mock<ICursor> cursor = new();
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.CustomCursor);
        cursor.Setup(c => c.X).Returns(x);
        cursor.Setup(c => c.Y).Returns(y);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        return cursor;
    }
}
