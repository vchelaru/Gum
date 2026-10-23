using Gum.Automation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Automation;

/// <summary>Ported from FlatRedBall2's parker tests: park every tick, return focus once.</summary>
public class AutomationWindowParkerTests
{
    private sealed class FakePlacer : IWindowPlacer
    {
        public IntPtr MainWindow { get; set; } = (IntPtr)100;
        public bool Foreground { get; set; } = true;
        public IntPtr Below { get; set; } = (IntPtr)200;
        public List<string> Calls { get; } = new();

        public bool IsForeground(IntPtr window) { Calls.Add("IsForeground"); return Foreground; }
        public IntPtr WindowBelow(IntPtr window) { Calls.Add("WindowBelow"); return Below; }
        public void MoveOffScreenToBottom(IntPtr window) => Calls.Add("Move");
        public void Focus(IntPtr window) => Calls.Add($"Focus:{window}");
    }

    [Fact]
    public void Tick_WhileFocused_ReadsTheWindowBelowBeforeMovingAndFocusesItOnce()
    {
        FakePlacer placer = new FakePlacer();
        AutomationWindowParker parker = new AutomationWindowParker(placer);

        parker.Tick();
        parker.Tick();

        placer.Calls.ShouldBe(new[] { "IsForeground", "WindowBelow", "Move", "Focus:200", "Move" });
    }

    [Fact]
    public void Tick_WhenUserAlreadyMovedOn_OnlyParks()
    {
        FakePlacer placer = new FakePlacer { Foreground = false };

        new AutomationWindowParker(placer).Tick();

        placer.Calls.ShouldBe(new[] { "IsForeground", "Move" });
    }

    [Fact]
    public void Tick_BeforeTheWindowExists_DoesNothing()
    {
        FakePlacer placer = new FakePlacer { MainWindow = IntPtr.Zero };

        new AutomationWindowParker(placer).Tick();

        placer.Calls.ShouldBeEmpty();
    }

    private class FakeGameBase
    {
        public bool IsActive { get; internal set; }
    }

    private sealed class FakeGame : FakeGameBase { }

    private sealed class FakeStrategy
    {
        // Written only by ForceActive through reflection, as KNI's focus events write it.
        private bool _isActive = false;
        public bool IsActive => _isActive;
    }

    private sealed class FakeKniGame
    {
        internal FakeStrategy Strategy { get; } = new FakeStrategy();
    }

    [Fact]
    public void ForceActive_SetsAnInternalSetterOnABaseClassOrABackingFieldOnTheStrategy()
    {
        FakeGame game = new FakeGame();
        AutomationWindowParker.ForceActive(game).ShouldBeTrue();
        game.IsActive.ShouldBeTrue();

        FakeKniGame kni = new FakeKniGame();
        AutomationWindowParker.ForceActive(kni).ShouldBeTrue();
        kni.Strategy.IsActive.ShouldBeTrue();

        AutomationWindowParker.ForceActive(new object()).ShouldBeFalse();
    }
}
