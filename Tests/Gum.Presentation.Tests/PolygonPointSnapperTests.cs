using System;
using System.Numerics;
using Gum.Wireframe.Editors.Handlers;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// #5889: a polygon point snaps to the world grid, not to a grid in its own local space.
/// </summary>
public class PolygonPointSnapperTests
{
    // Mirrors LinePolygon.AbsolutePointAt.
    private static Vector2 ToWorld(Vector2 local, Vector2 origin, Matrix4x4 rotation)
    {
        return local.X * new Vector2(rotation.M11, rotation.M12) +
            local.Y * new Vector2(rotation.M21, rotation.M22) +
            origin;
    }

    [Fact]
    public void SnapToWorldGrid_ShouldLandWorldPositionOnGrid_WhenPolygonIsOffGrid()
    {
        Vector2 origin = new Vector2(3, 5);

        Vector2 snappedLocal = PolygonPointSnapper.SnapToWorldGrid(
            new Vector2(20, 20), origin, Matrix4x4.Identity, gridSize: 16);

        // World (23, 25) rounds to (16, 32), so local is (13, 27); snapping the local value
        // itself would have given (16, 16) and left the point off the grid.
        snappedLocal.ShouldBe(new Vector2(13, 27));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(-45)]
    public void SnapToWorldGrid_ShouldLandWorldPositionOnGrid_WhenPolygonIsRotatedAndOffGrid(float degrees)
    {
        Vector2 origin = new Vector2(7.5f, -3);
        Matrix4x4 rotation = Matrix4x4.CreateRotationZ(degrees * MathF.PI / 180f);

        Vector2 snappedLocal = PolygonPointSnapper.SnapToWorldGrid(
            new Vector2(41, 17), origin, rotation, gridSize: 16);

        Vector2 world = ToWorld(snappedLocal, origin, rotation);
        (world.X / 16f).ShouldBe(MathF.Round(world.X / 16f), 0.0001f);
        (world.Y / 16f).ShouldBe(MathF.Round(world.Y / 16f), 0.0001f);
    }

    [Fact]
    public void SnapToWorldGrid_ShouldPickTheNearestGridPoint_WhenPolygonIsRotated()
    {
        Vector2 origin = new Vector2(0, 0);
        Matrix4x4 rotation = Matrix4x4.CreateRotationZ(MathF.PI / 2);
        Vector2 trueLocal = new Vector2(10, 3);

        Vector2 snappedLocal = PolygonPointSnapper.SnapToWorldGrid(trueLocal, origin, rotation, gridSize: 8);

        // Rotated 90 degrees, local (10, 3) is world (-3, 10), whose nearest grid point is (0, 8).
        Vector2 snappedWorld = ToWorld(snappedLocal, origin, rotation);
        snappedWorld.X.ShouldBe(0f, 0.0001f);
        snappedWorld.Y.ShouldBe(8f, 0.0001f);
    }
}
