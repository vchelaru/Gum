using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Gum.GueDeriving;

namespace SkiaGum.Tests.Runtimes;

/// <summary>
/// The data-driven path a loaded .gumx uses (<c>ApplyState</c> to <c>SetProperty(string, object)</c>)
/// for the Sprite/NineSlice <c>ColorOperation</c> variable (#4880). A file stores the enum as an int.
/// </summary>
public class DataDrivenColorOperationPropertyTests
{
    public DataDrivenColorOperationPropertyTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Theory]
    [InlineData(ColorOperation.Add)]
    [InlineData(ColorOperation.ColorTextureAlpha)]
    public void SetProperty_ColorOperation_OnSprite_AppliesEnumAndRawInteger(ColorOperation operation)
    {
        SpriteRuntime fromEnum = new();
        SpriteRuntime fromInt = new();

        fromEnum.SetProperty("ColorOperation", operation);
        fromInt.SetProperty("ColorOperation", (int)operation);

        fromEnum.ColorOperation.ShouldBe(operation);
        fromInt.ColorOperation.ShouldBe(operation);
    }

    [Theory]
    [InlineData(ColorOperation.Add)]
    [InlineData(ColorOperation.ColorTextureAlpha)]
    public void SetProperty_ColorOperation_OnNineSlice_AppliesEnumAndRawInteger(ColorOperation operation)
    {
        NineSliceRuntime fromEnum = new();
        NineSliceRuntime fromInt = new();

        fromEnum.SetProperty("ColorOperation", operation);
        fromInt.SetProperty("ColorOperation", (int)operation);

        fromEnum.ColorOperation.ShouldBe(operation);
        fromInt.ColorOperation.ShouldBe(operation);
    }
}
