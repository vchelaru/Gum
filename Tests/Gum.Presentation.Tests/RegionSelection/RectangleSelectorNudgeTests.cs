using Gum.Services;
using InputLibrary;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using KeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
// See RectangleSelectorDragRoundingTests for why this alias needs its own name.
using TexCoordRectangleSelector = TextureCoordinateSelectionPlugin.RegionSelection.RectangleSelector;

namespace Gum.Presentation.Tests.RegionSelection;

/// <summary>
/// An arrow key nudges the region one texture pixel, except while the command modifier is held:
/// Ctrl+Arrow (Cmd+Arrow on macOS) pans the camera instead (#5540).
/// </summary>
public class RectangleSelectorNudgeTests
{
    private readonly Mock<IInputHostControl> _host = new();
    private readonly Cursor _cursor = new();
    private readonly Keyboard _keyboard = new();
    private readonly TexCoordRectangleSelector _selector;
    private Keys[] _keysDown = new Keys[0];

    public RectangleSelectorNudgeTests()
    {
        _host.SetupGet(h => h.Focused).Returns(true);
        _host.SetupGet(h => h.Width).Returns(200);
        _host.SetupGet(h => h.Height).Returns(200);
        _host.SetupProperty(h => h.Cursor, CursorKind.Arrow);
        _host.Setup(h => h.GetPointerState()).Returns(() => new HostPointerState(150, 150, false, false, false));
        _host.Setup(h => h.GetKeyboardState()).Returns(() => new KeyboardState(_keysDown));
        _cursor.Initialize(_host.Object);
        _keyboard.Initialize(_host.Object);

        _selector = new TexCoordRectangleSelector(new SystemManagers { Renderer = new Renderer() }, new CanvasDisplayScale())
        {
            Visible = true,
            Left = 10,
            Top = 10,
            Width = 40,
            Height = 40
        };
    }

    private void Frame(params Keys[] keysDown)
    {
        _keysDown = keysDown;
        _cursor.Activity(0);
        _keyboard.Activity();
        _selector.Activity(_cursor, _keyboard, _host.Object);
    }

    [Theory]
    [InlineData(Keys.LeftControl)]
    [InlineData(Keys.RightControl)]
    [InlineData(Keys.LeftWindows)]
    [InlineData(Keys.RightWindows)]
    public void Activity_ShouldNotNudge_WhileACommandModifierIsHeld(Keys modifier)
    {
        Frame(modifier);
        Frame(modifier, Keys.Left);

        _selector.Left.ShouldBe(10);
    }

    [Fact]
    public void Activity_ShouldNudgeOnePixel_ForABareArrow()
    {
        Frame();
        Frame(Keys.Left);

        _selector.Left.ShouldBe(9);
    }
}
