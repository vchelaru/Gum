using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ListMoverTests
{
    private sealed record Item(string Name);

    private static (List<Item> List, Item A, Item B, Item C, Item D) Four()
    {
        Item a = new("A"), b = new("B"), c = new("C"), d = new("D");
        return (new List<Item> { a, b, c, d }, a, b, c, d);
    }

    [Fact]
    public void Move_ToStart_PutsItFirst()
    {
        var (list, _, _, _, d) = Four();

        ListMover.Move(list, d, ReorderPlacement.ToStart);

        list.Select(item => item.Name).ShouldBe(new[] { "D", "A", "B", "C" });
    }

    [Fact]
    public void Move_ToEnd_PutsItLast()
    {
        var (list, a, _, _, _) = Four();

        ListMover.Move(list, a, ReorderPlacement.ToEnd);

        list.Select(item => item.Name).ShouldBe(new[] { "B", "C", "D", "A" });
    }

    [Fact]
    public void Move_BeforeALaterAnchor_LandsDirectlyBeforeIt()
    {
        var (list, a, _, _, d) = Four();

        ListMover.Move(list, a, ReorderPlacement.Before, d);

        list.Select(item => item.Name).ShouldBe(new[] { "B", "C", "A", "D" });
    }

    [Fact]
    public void Move_AfterAnEarlierAnchor_LandsDirectlyAfterIt()
    {
        var (list, a, _, _, d) = Four();

        ListMover.Move(list, d, ReorderPlacement.After, a);

        list.Select(item => item.Name).ShouldBe(new[] { "A", "D", "B", "C" });
    }

    [Fact]
    public void Move_AfterALaterAnchor_LandsDirectlyAfterIt()
    {
        var (list, a, _, c, _) = Four();

        ListMover.Move(list, a, ReorderPlacement.After, c);

        list.Select(item => item.Name).ShouldBe(new[] { "B", "C", "A", "D" });
    }

    [Fact]
    public void Move_BeforeAnEarlierAnchor_LandsDirectlyBeforeIt()
    {
        var (list, _, b, _, d) = Four();

        ListMover.Move(list, d, ReorderPlacement.Before, b);

        list.Select(item => item.Name).ShouldBe(new[] { "A", "D", "B", "C" });
    }

    [Fact]
    public void MoveBetween_DefaultsToTheEndOfTheTarget()
    {
        var (from, a, _, _, _) = Four();
        List<Item> to = new() { new Item("X") };

        ListMover.MoveBetween(from, to, a);

        from.Select(item => item.Name).ShouldBe(new[] { "B", "C", "D" });
        to.Select(item => item.Name).ShouldBe(new[] { "X", "A" });
    }

    [Fact]
    public void MoveBetween_BeforeAnAnchor_LandsDirectlyBeforeIt()
    {
        var (from, a, _, _, _) = Four();
        Item x = new("X"), y = new("Y");
        List<Item> to = new() { x, y };

        ListMover.MoveBetween(from, to, a, ReorderPlacement.Before, y);

        to.Select(item => item.Name).ShouldBe(new[] { "X", "A", "Y" });
    }
}
