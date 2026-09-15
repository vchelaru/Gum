using Gum.Input;
using Gum.Managers;
using Gum.Services;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

/// <summary>The platform-dependent defaults of <see cref="HotkeyManager"/>.</summary>
public class HotkeyManagerTests
{
    [Theory]
    [InlineData(false, GumKey.Y, false, GumKey.Z, true)]
    [InlineData(true, GumKey.Z, true, GumKey.Y, false)]
    public void Redo_IsCtrlY_ExceptOnMacOS_WhereItIsShiftCmdZ(bool isMacOS,
        GumKey redoKey, bool redoShift, GumKey redoAltKey, bool redoAltShift)
    {
        AutoMocker mocker = new AutoMocker();
        mocker.GetMock<IOperatingSystemInfo>().Setup(o => o.IsMacOS).Returns(isMacOS);

        HotkeyManager hotkeyManager = mocker.CreateInstance<HotkeyManager>();

        hotkeyManager.Redo.Key.ShouldBe(redoKey);
        hotkeyManager.Redo.IsCtrlDown.ShouldBeTrue();
        hotkeyManager.Redo.IsShiftDown.ShouldBe(redoShift);
        hotkeyManager.RedoAlt.Key.ShouldBe(redoAltKey);
        hotkeyManager.RedoAlt.IsCtrlDown.ShouldBeTrue();
        hotkeyManager.RedoAlt.IsShiftDown.ShouldBe(redoAltShift);
    }
}
