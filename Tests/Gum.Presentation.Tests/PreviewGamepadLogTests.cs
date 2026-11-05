using GumPreview;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class PreviewGamepadLogTests
{
    [Fact]
    public void Changed_SameTextTwice_ReturnsNullSecondTime()
    {
        PreviewGamepadLog log = new PreviewGamepadLog();

        log.Changed("pad0", "a").ShouldBe("a");
        log.Changed("pad0", "a").ShouldBeNull();
        log.Changed("pad0", "b").ShouldBe("b");
    }

    [Fact]
    public void Changed_DifferentKeys_AreIndependent()
    {
        PreviewGamepadLog log = new PreviewGamepadLog();

        log.Changed("pad0", "a");

        log.Changed("pad1", "a").ShouldBe("a");
    }

    [Fact]
    public void FormatStick_InsideDeadZone_IsNull()
    {
        PreviewGamepadLog.FormatStick(0.1f, -0.14f).ShouldBeNull();
    }

    [Fact]
    public void FormatStick_BeyondDeadZone_RoundsToTwoDecimals()
    {
        PreviewGamepadLog.FormatStick(0.5234f, -0.3099f).ShouldBe("0.52,-0.31");
    }

    [Fact]
    public void FormatTrigger_AtRest_IsNull()
    {
        PreviewGamepadLog.FormatTrigger(0f).ShouldBeNull();
        PreviewGamepadLog.FormatTrigger(0.8f).ShouldBe("0.80");
    }

    [Fact]
    public void FormatRaw_ListsPressedButtonsAndOnlyActiveAxes()
    {
        string text = PreviewGamepadLog.FormatRaw(true, 42, new[] { "A", "DPadDown" }, 0.9f, 0f, 0f, 0f, 0f, 0.5f);

        text.ShouldBe("connected=True pressed=[A,DPadDown] leftStick=0.90,0.00 rightTrigger=0.50 packet=42");
    }

    [Fact]
    public void FormatRaw_NothingPressed_HasEmptyList()
    {
        PreviewGamepadLog.FormatRaw(true, 1, System.Array.Empty<string>(), 0, 0, 0, 0, 0, 0)
            .ShouldBe("connected=True pressed=[] packet=1");
    }

    [Fact]
    public void FormatGumButton_ShowsAllFourAnswers()
    {
        PreviewGamepadLog.FormatGumButton(true, true, false, true)
            .ShouldBe("down=True pushed=True released=False repeat=True");
    }

    [Fact]
    public void HeartbeatDue_FirstCallThenOncePerInterval()
    {
        PreviewGamepadLog log = new PreviewGamepadLog();

        log.HeartbeatDue(1000).ShouldBeTrue();
        log.HeartbeatDue(3000).ShouldBeFalse();
        log.HeartbeatDue(6000).ShouldBeTrue();
        log.HeartbeatDue(6001).ShouldBeFalse();
    }

    [Fact]
    public void FormatHeartbeat_ShowsActivePacketAndPressed()
    {
        PreviewGamepadLog.FormatHeartbeat(true, true, 7, new[] { "A" })
            .ShouldBe("heartbeat active=True connected=True packet=7 pressed=[A]");
    }

    [Fact]
    public void FormatJoystickCaps_ShowsIdentityAndCounts()
    {
        PreviewGamepadLog.FormatJoystickCaps(true, "Xbox", "abc", 11, 6, 1)
            .ShouldBe("connected=True name='Xbox' id='abc' buttons=11 axes=6 hats=1");
    }

    [Fact]
    public void FormatJoystickState_ListsOnlyPressedMovedAndActiveHats()
    {
        string text = PreviewGamepadLog.FormatJoystickState(true,
            new[] { false, true, false, true }, new[] { 100, -20000, 0 }, new[] { "", "UR" });

        text.ShouldBe("connected=True buttons=[1,3] axes=[1:-20000] hats=[1:UR]");
    }

    [Fact]
    public void FormatXInput_Disconnected_IsOneWord()
    {
        PreviewGamepadLog.FormatXInput(false, 0, 0, 0, 0, 0, 0, 0, 0).ShouldBe("connected=False");
    }

    [Fact]
    public void FormatXInput_NamesButtonsAndShowsOnlyMovedSticksAndTriggers()
    {
        string text = PreviewGamepadLog.FormatXInput(true, 9, 0x1002, 0, 200, 0, 0, 12000, -100);

        text.ShouldBe("connected=True packet=9 pressed=[DPadDown,A] rightStick=12000,-100 rightTrigger=200");
    }

    [Theory]
    [InlineData(null, "all", true, true, false)]
    [InlineData("", "all", true, true, false)]
    [InlineData("all", "all", true, true, false)]
    [InlineData("none", "none", false, false, false)]
    [InlineData("joystick", "joystick", true, false, false)]
    [InlineData("xinput", "xinput", false, true, false)]
    [InlineData("fix", "fix", true, true, true)]
    [InlineData("  XInput ", "xinput", false, true, false)]
    public void ParseProbes_KnownValue_SelectsLayers(string? value, string active, bool joystick, bool xinput, bool fix)
    {
        PreviewGamepadLog.GamepadProbes probes = PreviewGamepadLog.ParseProbes(value);

        probes.Active.ShouldBe(active);
        probes.Joystick.ShouldBe(joystick);
        probes.XInput.ShouldBe(xinput);
        probes.Fix.ShouldBe(fix);
        probes.Warning.ShouldBeNull();
    }

    [Fact]
    public void ParseProbes_UnknownValue_FallsBackToAllAndWarns()
    {
        PreviewGamepadLog.GamepadProbes probes = PreviewGamepadLog.ParseProbes("joystik");

        probes.Active.ShouldBe("all");
        probes.Joystick.ShouldBeTrue();
        probes.XInput.ShouldBeTrue();
        probes.Fix.ShouldBeFalse();
        probes.Warning.ShouldNotBeNull();
        probes.Warning!.ShouldContain("joystik");
    }

    [Fact]
    public void InputLostHint_MentionsOtherAppsAndBluetooth()
    {
        PreviewGamepadLog.InputLostHint.ShouldContain("Steam");
        PreviewGamepadLog.InputLostHint.ShouldContain("Bluetooth");
    }
}
