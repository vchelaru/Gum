using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Gum.GueDeriving;
using SkiaGum.Renderables;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Text hit testing (#6571): a click inside rotated text, or text under a rotated parent, resolves
/// to the character drawn there. Skia's renderer turns text by its absolute rotation about the
/// renderable's top-left, so each test builds the screen point by turning a local point the same way.
/// </summary>
public class TextHitTestRotationTests
{
    public TextHitTestRotationTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    static (float x, float y) LocalToScreen(IRenderableIpso text, float localX, float localY)
    {
        double radians = text.GetAbsoluteRotation() * Math.PI / 180;
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        return (
            text.GetAbsoluteX() + localX * cos + localY * sin,
            text.GetAbsoluteY() - localX * sin + localY * cos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(30)]
    [InlineData(-135)]
    public void GetCharacterIndexAtPosition_RotatedText_ShouldReturnCharacterDrawnAtPoint(float rotation)
    {
        TextRuntime sut = new();
        sut.X = 100;
        sut.Y = 80;
        sut.Width = 200;
        sut.Text = "Line1\nLine2";
        sut.Rotation = rotation;
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier;
        // Just past the start of the fourth character ('e') on the second line.
        float localX = contained.MeasureString("Lin") + 1;
        float localY = lineHeight * 1.5f;

        (float screenX, float screenY) = LocalToScreen(contained, localX, localY);

        sut.GetCharacterIndexAtPosition(screenX, screenY).ShouldBe("Line1\n".Length + 3);
    }

    [Fact]
    public void GetCharacterIndexAtPosition_UnderRotatedParent_ShouldReturnCharacterDrawnAtPoint()
    {
        ContainerRuntime parent = new();
        parent.X = 150;
        parent.Y = 120;
        parent.Rotation = 60;
        TextRuntime sut = new();
        sut.X = 10;
        sut.Y = 20;
        sut.Width = 200;
        sut.Text = "Line1\nLine2";
        sut.Rotation = 30;
        parent.AddChild(sut);
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier;
        float localX = contained.MeasureString("Lin") + 1;
        float localY = lineHeight * 1.5f;

        (float screenX, float screenY) = LocalToScreen(contained, localX, localY);

        sut.GetCharacterIndexAtPosition(screenX, screenY).ShouldBe("Line1\n".Length + 3);
    }
}
