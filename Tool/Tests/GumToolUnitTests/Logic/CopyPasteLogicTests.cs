using Gum.DataTypes;
using Gum.Logic;
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
public class CopyPasteLogicTests
{
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly AutoMocker mocker;
    public CopyPasteLogicTests()
    {
        mocker = new ();

        _copyPasteLogic = mocker.CreateInstance<CopyPasteLogic>();
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
}
