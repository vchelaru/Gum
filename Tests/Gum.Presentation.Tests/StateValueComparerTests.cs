using Gum.Wireframe.Editors;
using Shouldly;
using System.Collections.Generic;
using System.Numerics;

namespace Gum.Presentation.Tests;

public class StateValueComparerTests
{
    [Fact]
    public void DoValuesDiffer_ShouldCompareListsByCountAndElements()
    {
        List<Vector2> original = new List<Vector2> { new Vector2(0, 0), new Vector2(10, 0) };
        List<Vector2> equalCopy = new List<Vector2> { new Vector2(0, 0), new Vector2(10, 0) };
        List<Vector2> movedPoint = new List<Vector2> { new Vector2(0, 0), new Vector2(20, 0) };
        List<Vector2> shorter = new List<Vector2> { new Vector2(0, 0) };
        List<Vector2> longer = new List<Vector2> { new Vector2(0, 0), new Vector2(10, 0), new Vector2(5, 5) };

        StateValueComparer.DoValuesDiffer(original, equalCopy).ShouldBeFalse();
        StateValueComparer.DoValuesDiffer(original, movedPoint).ShouldBeTrue();
        StateValueComparer.DoValuesDiffer(original, shorter).ShouldBeTrue();
        StateValueComparer.DoValuesDiffer(original, longer).ShouldBeTrue();
    }

    [Fact]
    public void DoValuesDiffer_ShouldTreatBothNullAsUnchanged()
    {
        StateValueComparer.DoValuesDiffer(null, null).ShouldBeFalse();
        StateValueComparer.DoValuesDiffer(null, 1f).ShouldBeTrue();
        StateValueComparer.DoValuesDiffer(1f, null).ShouldBeTrue();
        StateValueComparer.DoValuesDiffer(1f, 1f).ShouldBeFalse();
        StateValueComparer.DoValuesDiffer(1f, 2f).ShouldBeTrue();
    }
}
