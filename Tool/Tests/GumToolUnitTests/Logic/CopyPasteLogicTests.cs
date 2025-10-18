using Gum.DataTypes;
using Gum.Logic;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Logic;
public class CopyPasteLogicTests : BaseTestClass
{
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;

    private readonly AutoMocker mocker;
    public CopyPasteLogicTests()
    {
        mocker = new ();

        _copyPasteLogic = mocker.CreateInstance<CopyPasteLogic>();

        _selectedState = mocker.GetMock<ISelectedState>();
        _elementCommands = mocker.GetMock<IElementCommands>();

    }

    [Fact]
    public void OnPaste_ShouldCreateOneUndo_ForMultiplePastedObjects()
    {
        var selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(new List<InstanceSave>
            {
                new InstanceSave
                {
                    Name = "Instance1"
                },
                new InstanceSave
                {
                    Name = "Instance2"
                }
            });

        var undoManager = mocker.GetMock<IUndoManager>();

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        undoManager
            .Verify(x => x.RequestLock(), Times.Once);
    }

    [Fact(Skip ="need to inject plugin manager first")]
    public void OnPaste_ShouldSortVariables()
    {
        var element = new ScreenSave();
        element.States.Add(new Gum.DataTypes.Variables.StateSave());

        var instance = new InstanceSave();
        element.Instances.Add(instance);
        instance.ParentContainer = element;
        instance.Name = "Instance1";

        _selectedState
            .Setup(x => x.SelectedInstance).Returns(instance);
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(new List<InstanceSave> { instance });

        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        _selectedState.Setup(x => x.SelectedStateSave).Returns(element.DefaultState);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        _elementCommands.Verify(x => x.SortVariables(It.IsAny<ElementSave>()), Times.Once);
    }
}
