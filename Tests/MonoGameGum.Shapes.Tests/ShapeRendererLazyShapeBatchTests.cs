using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using Shouldly;
using System.Runtime.CompilerServices;
using ApoShapeBatch = Apos.Shapes.ShapeBatch;

namespace MonoGameGum.Shapes.Tests;

// Issue #6153 — building an Apos ShapeBatch loads and compiles its shader, which can exceed the
// Android ANR limit on slow GPUs. Initialize must not build it; the first access to the
// ShapeBatch (the first shape draw) does. Stand-in instances for the batch and device are enough
// because only creation is observed, never drawing.
public class ShapeRendererLazyShapeBatchTests
{
    private static ApoShapeBatch CreateStandIn() =>
        (ApoShapeBatch)RuntimeHelpers.GetUninitializedObject(typeof(ApoShapeBatch));

    private static GraphicsDevice CreateDeviceStandIn() =>
        (GraphicsDevice)RuntimeHelpers.GetUninitializedObject(typeof(GraphicsDevice));

    [Fact]
    public void Initialize_DoesNotCreateShapeBatch()
    {
        int createCount = 0;
        ShapeRenderer sut = new ShapeRenderer();
        sut.ShapeBatchFactory = _ =>
        {
            createCount++;
            return CreateStandIn();
        };

        sut.Initialize(CreateDeviceStandIn(), null!);

        createCount.ShouldBe(0);
        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void ShapeBatch_FirstAccess_CreatesOnceAndReuses()
    {
        int createCount = 0;
        ShapeRenderer sut = new ShapeRenderer();
        sut.ShapeBatchFactory = _ =>
        {
            createCount++;
            return CreateStandIn();
        };
        sut.Initialize(CreateDeviceStandIn(), null!);

        ApoShapeBatch first = sut.ShapeBatch;
        ApoShapeBatch second = sut.ShapeBatch;

        createCount.ShouldBe(1);
        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void ShapeBatch_WhenNotInitialized_IsNullAndDoesNotCreate()
    {
        int createCount = 0;
        ShapeRenderer sut = new ShapeRenderer();
        sut.ShapeBatchFactory = _ =>
        {
            createCount++;
            return CreateStandIn();
        };

        sut.ShapeBatch.ShouldBeNull();

        createCount.ShouldBe(0);
    }

    [Fact]
    public void Uninitialize_BeforeShapeBatchCreated_DoesNotThrowAndAllowsReinitialize()
    {
        ShapeRenderer sut = new ShapeRenderer();
        sut.ShapeBatchFactory = _ => CreateStandIn();
        sut.Initialize(CreateDeviceStandIn(), null!);

        Should.NotThrow(() => sut.Uninitialize());

        sut.IsInitialized.ShouldBeFalse();
        Should.NotThrow(() => sut.Initialize(CreateDeviceStandIn(), null!));
    }
}
