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
}
