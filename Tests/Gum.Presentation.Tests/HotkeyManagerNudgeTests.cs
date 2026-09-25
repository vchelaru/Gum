using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Input;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

public class HotkeyManagerNudgeTests : BaseTestClass
{
    [Fact]
    public void ProcessCmdKeyWireframe_NudgingABehaviorInstance_DoesNotThrow()
    {
        // A behavior's instances have no element, so nothing is selected as the element.
        AutoMocker mocker = new();
        BehaviorSave behavior = new() { Name = "ButtonBehavior" };
        BehaviorInstanceSave instance = new() { Name = "TextInstance", BaseType = "Text" };
        behavior.RequiredInstances.Add(instance);
        mocker.GetMock<ISelectedState>().SetupGet(x => x.SelectedBehavior).Returns(behavior);
        mocker.GetMock<ISelectedState>().SetupGet(x => x.SelectedInstance).Returns(instance);
        mocker.GetMock<ISelectedState>().SetupGet(x => x.SelectedElement).Returns((ElementSave?)null);
        HotkeyManager sut = mocker.CreateInstance<HotkeyManager>();

        sut.ProcessCmdKeyWireframe(GumKey.Right, isShiftDown: false, isCtrlDown: false, isAltDown: false);
    }
}
