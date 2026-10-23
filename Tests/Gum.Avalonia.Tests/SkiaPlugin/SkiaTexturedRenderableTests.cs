using System;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using Xunit;

namespace Gum.Avalonia.Tests.SkiaPlugin;

/// <summary>
/// The tool's Skia renderable rejects NaN and infinity on X, Y, Width, Height and Rotation like every runtime
/// renderable (#6481).
/// </summary>
public class SkiaTexturedRenderableTests
{
    private sealed class EmptyDrawable : RenderableSkiaObject
    {
        public override void DrawToSurface(SKSurface surface) { }
    }

    [Theory]
    [InlineData("X", float.NaN)]
    [InlineData("Y", float.PositiveInfinity)]
    [InlineData("Width", float.NegativeInfinity)]
    [InlineData("Height", float.NaN)]
    [InlineData("Rotation", float.PositiveInfinity)]
    public void NonFiniteValue_IsRejectedAndNotStored(string variable, float value)
    {
        SkiaTexturedRenderable renderable = new SkiaTexturedRenderable(new EmptyDrawable());
        renderable.X = 1;
        renderable.Y = 2;
        renderable.Width = 3;
        renderable.Height = 4;
        renderable.Rotation = 5;

        ArgumentException exception = Should.Throw<ArgumentException>(() => Assign(renderable, variable, value));

        exception.Message.ShouldBe("Invalid " + variable + ": " + value);
        renderable.X.ShouldBe(1);
        renderable.Y.ShouldBe(2);
        renderable.Width.ShouldBe(3);
        renderable.Height.ShouldBe(4);
        renderable.Rotation.ShouldBe(5);
    }

    private static void Assign(SkiaTexturedRenderable renderable, string variable, float value)
    {
        switch (variable)
        {
            case "X": renderable.X = value; break;
            case "Y": renderable.Y = value; break;
            case "Width": renderable.Width = value; break;
            case "Height": renderable.Height = value; break;
            case "Rotation": renderable.Rotation = value; break;
        }
    }
}
