using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// A Raylib renderable's Width and Height treat +infinity the same (#6477). RaylibGum always defines
/// FULL_DIAGNOSTICS in Debug and Release, so both axes reject it. Before, the shared base rejected a Height of
/// +infinity but silently accepted it as a Width.
/// </summary>
public class RenderableSizeNonFiniteTests
{
    public static TheoryData<string> RenderableNames() => new()
    {
        nameof(SolidRectangle),
        nameof(Sprite),
        nameof(NineSlice),
        nameof(LineRectangle),
        nameof(LineCircle),
        nameof(LinePolygon),
        nameof(LineArc),
    };

    private static RenderableBase Create(string name) => name switch
    {
        nameof(SolidRectangle) => new SolidRectangle(),
        nameof(Sprite) => new Sprite(),
        nameof(NineSlice) => new NineSlice(),
        nameof(LineRectangle) => new LineRectangle(),
        nameof(LineCircle) => new LineCircle(),
        nameof(LinePolygon) => new LinePolygon(),
        nameof(LineArc) => new LineArc(),
        _ => throw new ArgumentException(name),
    };

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void PositiveInfinity_IsRejectedOnBothAxes(string name)
    {
        RenderableBase renderable = Create(name);

        Should.Throw<ArgumentException>(() => renderable.Height = float.PositiveInfinity);
        Should.Throw<ArgumentException>(() => renderable.Width = float.PositiveInfinity);
        renderable.Width.ShouldBe(0f, "because a rejected value is not stored");
        renderable.Height.ShouldBe(0f, "because a rejected value is not stored");
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void FiniteSizes_AreStoredUnchangedOnBothAxes(string name)
    {
        RenderableBase renderable = Create(name);

        renderable.Width = 123.5f;
        renderable.Height = -45.25f;

        renderable.Width.ShouldBe(123.5f);
        renderable.Height.ShouldBe(-45.25f);
    }
}
