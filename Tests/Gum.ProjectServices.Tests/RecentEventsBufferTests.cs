using Gum.Diagnostics;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class RecentEventsBufferTests
{
    [Fact]
    public void Snapshot_ReturnsEventsInTheOrderAdded()
    {
        RecentEventsBuffer buffer = new RecentEventsBuffer(capacity: 3);

        buffer.Add("first");
        buffer.Add("second");

        buffer.Snapshot().ShouldBe(new[] { "first", "second" });
    }

    [Fact]
    public void Add_PastCapacity_DropsTheOldestEvent()
    {
        RecentEventsBuffer buffer = new RecentEventsBuffer(capacity: 2);

        buffer.Add("first");
        buffer.Add("second");
        buffer.Add("third");

        buffer.Snapshot().ShouldBe(new[] { "second", "third" });
    }
}
