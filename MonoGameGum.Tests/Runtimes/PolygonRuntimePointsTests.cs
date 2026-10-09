using Gum.GueDeriving;
using Shouldly;
using System.Numerics;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class PolygonRuntimePointsTests : BaseTestClass
{
    [Fact]
    public void GetPointAt_ShouldReturnPointsPassedToSetPoints()
    {
        PolygonRuntime sut = new();

        sut.SetPoints(new[]
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6),
        });

        sut.PointCount.ShouldBe(3);
        sut.GetPointAt(0).ShouldBe(new Vector2(1, 2));
        sut.GetPointAt(1).ShouldBe(new Vector2(3, 4));
        sut.GetPointAt(2).ShouldBe(new Vector2(5, 6));
    }

    [Fact]
    public void GetPointAt_ShouldReflectInsertSetAndRemove()
    {
        PolygonRuntime sut = new();
        sut.SetPoints(new[] { new Vector2(0, 0), new Vector2(10, 10) });

        sut.InsertPointAt(new Vector2(5, 5), 1);
        sut.SetPointAt(new Vector2(99, 99), 2);
        sut.RemovePointAtIndex(0);

        sut.PointCount.ShouldBe(2);
        sut.GetPointAt(0).ShouldBe(new Vector2(5, 5));
        sut.GetPointAt(1).ShouldBe(new Vector2(99, 99));
    }
}
