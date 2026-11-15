using Gum.Input;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace SkiaHostTests;

/// <summary>
/// A host's keyboard wired to a fake device the test drives. A fresh rig is made for each test.
/// </summary>
public interface IHostKeyboardRig
{
    /// <summary>The host's keyboard, attached to the fake device.</summary>
    PolledKeyboard Keyboard { get; }

    /// <summary>
    /// Makes exactly these keys the held set from the next <c>Activity</c> on. The rig reports it the
    /// way its engine does (a poll of the down state, or per-frame pressed/released/down sets).
    /// Supports <c>A</c>, <c>Left</c>, <c>Right</c> and <c>Space</c>.
    /// </summary>
    void SetHeld(params GumKeys[] keys);

    /// <summary>
    /// Called before every <c>Activity</c>. A rig whose engine reports per-frame pressed/released sets
    /// drops last frame's sets here unless <see cref="SetHeld"/> changed the state since the last frame.
    /// </summary>
    void BeginFrame();

    /// <summary>The device reports this text as typed before the next <c>Activity</c>, one character at a time.</summary>
    void TypeText(string text);
}

/// <summary>
/// What every polled Skia host keyboard reports: down state, push and release edges, key repeat timing
/// and typed text, read through <see cref="PolledKeyboard"/> after <c>Activity</c>. A host's test
/// project derives from this class and supplies a rig over its own fake device. The key translation
/// table cases and engine-specific text events stay in the host's own tests.
/// </summary>
public abstract class PolledKeyboardTestsBase
{
    /// <summary>A rig with a fresh keyboard and fake device.</summary>
    protected abstract IHostKeyboardRig CreateRig();

    private static void Frame(IHostKeyboardRig rig, double gameTime)
    {
        rig.BeginFrame();
        rig.Keyboard.Activity(gameTime);
    }

    [Fact]
    public void Activity_ReportsHeldKeyAsDown()
    {
        IHostKeyboardRig rig = CreateRig();
        rig.SetHeld(GumKeys.Left);

        Frame(rig, 0);

        rig.Keyboard.KeyDown(GumKeys.Left).ShouldBeTrue();
        rig.Keyboard.KeyDown(GumKeys.Right).ShouldBeFalse();
    }

    [Fact]
    public void GetStringTyped_ReturnsTypedCharacters()
    {
        IHostKeyboardRig rig = CreateRig();

        rig.TypeText("hi");
        Frame(rig, 0);

        rig.Keyboard.GetStringTyped().ShouldBe("hi");
    }

    [Fact]
    public void KeyTyped_DoesNotRepeatBeforeRepeatDelayElapses()
    {
        IHostKeyboardRig rig = CreateRig();
        PolledKeyboard keyboard = rig.Keyboard;
        rig.SetHeld(GumKeys.A);

        Frame(rig, 0);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // initial push

        Frame(rig, keyboard.RepeatDelay.TotalSeconds - 0.01);
        keyboard.KeyTyped(GumKeys.A).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_RepeatsAtRepeatRateAfterRepeatDelayElapses()
    {
        IHostKeyboardRig rig = CreateRig();
        PolledKeyboard keyboard = rig.Keyboard;
        rig.SetHeld(GumKeys.A);

        Frame(rig, 0);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // initial push

        Frame(rig, keyboard.RepeatDelay.TotalSeconds);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // first repeat

        Frame(rig, keyboard.RepeatDelay.TotalSeconds + keyboard.RepeatRate.TotalSeconds - 0.01);
        keyboard.KeyTyped(GumKeys.A).ShouldBeFalse(); // too soon for the next repeat

        Frame(rig, keyboard.RepeatDelay.TotalSeconds + keyboard.RepeatRate.TotalSeconds);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // second repeat
    }

    [Fact]
    public void KeyTyped_ResetsRepeatTimingAfterKeyIsReleasedAndPressedAgain()
    {
        IHostKeyboardRig rig = CreateRig();
        PolledKeyboard keyboard = rig.Keyboard;
        rig.SetHeld(GumKeys.A);
        Frame(rig, 0);
        Frame(rig, keyboard.RepeatDelay.TotalSeconds); // would repeat if still held

        rig.SetHeld();
        Frame(rig, keyboard.RepeatDelay.TotalSeconds + 0.01);

        rig.SetHeld(GumKeys.A);
        Frame(rig, keyboard.RepeatDelay.TotalSeconds + 0.02);

        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // fresh push, not a stale repeat
    }

    [Fact]
    public void KeyPushed_IsTrueOnlyOnTheFrameOfInitialPress()
    {
        IHostKeyboardRig rig = CreateRig();
        PolledKeyboard keyboard = rig.Keyboard;
        rig.SetHeld(GumKeys.A);

        Frame(rig, 0);
        keyboard.KeyPushed(GumKeys.A).ShouldBeTrue();

        // Still held on the next frame -> no longer a fresh push.
        Frame(rig, 1);
        keyboard.KeyPushed(GumKeys.A).ShouldBeFalse();
        keyboard.KeyDown(GumKeys.A).ShouldBeTrue();
    }

    [Fact]
    public void KeyReleased_IsTrueOnTheFrameOfRelease()
    {
        IHostKeyboardRig rig = CreateRig();
        PolledKeyboard keyboard = rig.Keyboard;
        rig.SetHeld(GumKeys.Space);
        Frame(rig, 0);

        rig.SetHeld();
        Frame(rig, 1);

        keyboard.KeyReleased(GumKeys.Space).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.Space).ShouldBeFalse();
    }
}
