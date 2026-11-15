using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// A host's cursor wired to a fake mouse the test drives. A fresh rig is made for each test.
/// </summary>
public interface IHostCursorRig
{
    /// <summary>The host's cursor, attached to the fake mouse.</summary>
    ICursor Cursor { get; }

    /// <summary>Puts the fake mouse at this pixel position.</summary>
    void SetPosition(int x, int y);

    /// <summary>Presses or releases the fake mouse's primary button.</summary>
    void SetPrimaryDown(bool isDown);

    /// <summary>Scrolls the fake mouse's wheel by whole notches (positive is up).</summary>
    void Scroll(int notches);
}

/// <summary>
/// What every interactive Skia host's cursor reports for a mouse: position, the primary button and
/// accumulated scroll, read through <see cref="ICursor"/> after <c>Activity</c>. A host's test project
/// derives from this class and supplies a rig over its own fake mouse.
/// </summary>
public abstract class CursorTestsBase
{
    /// <summary>A rig with a fresh cursor and fake mouse.</summary>
    protected abstract IHostCursorRig CreateRig();

    [Fact]
    public void Activity_MapsMousePosition()
    {
        IHostCursorRig rig = CreateRig();
        rig.SetPosition(400, 300);

        rig.Cursor.Activity(0);

        rig.Cursor.X.ShouldBe(400);
        rig.Cursor.Y.ShouldBe(300);
    }

    [Fact]
    public void Activity_MapsPrimaryButtonDown()
    {
        IHostCursorRig rig = CreateRig();
        rig.SetPrimaryDown(true);

        rig.Cursor.Activity(0);

        rig.Cursor.PrimaryDown.ShouldBeTrue();
    }

    [Fact]
    public void Scroll_AccumulatesIntoScrollWheelDelta()
    {
        // Two notches up reach the cursor as ScrollWheelChange = 2 on the next Activity.
        IHostCursorRig rig = CreateRig();
        rig.Cursor.Activity(0);

        rig.Scroll(2);
        rig.Cursor.Activity(0.016);

        rig.Cursor.ScrollWheelChange.ShouldBe(2);
    }
}
