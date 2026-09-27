using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

// #5315: pasting a cut screen or component where it cannot move says why in the Output tab.
public class CopyPasteLogicCutElementTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new AutoMocker();
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly GumTreeNode _componentsRoot = new GumTreeNode("Components");
    private readonly Mock<ISelectedState> _selectedState;
    private readonly CopyPasteLogic _copyPasteLogic;

    public CopyPasteLogicCutElementTests()
    {
        _selectedState = _mocker.GetMock<ISelectedState>();
        _selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());
        _mocker.GetMock<ICopyPasteProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        _copyPasteLogic = _mocker.CreateInstance<CopyPasteLogic>();
    }

    [Fact]
    public void OnPaste_CutComponentOnANonFolder_WritesThatItNeedsAComponentsFolder()
    {
        ComponentSave toggle = AddComponent("Toggle");
        ComponentSave panel = AddComponent("Panel");
        GumTreeNode panelNode = (GumTreeNode)_componentsRoot.AddChild("Panel");
        panelNode.SetTag(panel);
        Cut(toggle);

        Paste(on: panelNode);

        VerifyOutput("Cut Toggle can only be pasted on a Components folder.");
    }

    [Fact]
    public void OnPaste_CutScreenOnAComponentsFolder_WritesThatItIsAScreen()
    {
        ScreenSave mainMenu = new ScreenSave { Name = "MainMenu" };
        _project.Screens.Add(mainMenu);
        GumTreeNode controlsFolder = (GumTreeNode)_componentsRoot.AddChild("Controls");
        Cut(mainMenu);

        Paste(on: controlsFolder);

        VerifyOutput("Cut MainMenu is a screen, so it can only be pasted on a Screens folder, not on Components/Controls.");
    }

    [Fact]
    public void OnPaste_CutComponentOnTheFolderItIsIn_WritesThatItIsAlreadyThere()
    {
        ComponentSave toggle = AddComponent("Controls/Toggle");
        GumTreeNode controlsFolder = (GumTreeNode)_componentsRoot.AddChild("Controls");
        Cut(toggle);

        Paste(on: controlsFolder);

        VerifyOutput("Cut Controls/Toggle is already in Components/Controls.");
    }

    [Fact]
    public void OnPaste_CutComponentDeletedSinceTheCut_WritesThatItWasDeleted_AndForgetsTheCut()
    {
        ComponentSave toggle = AddComponent("Toggle");
        Cut(toggle);
        _project.Components.Remove(toggle);

        Paste(on: _componentsRoot);

        VerifyOutput("Cut Toggle was deleted, so there is nothing to paste.");
        // A cut never copies, so a later paste must not bring the deleted component back.
        _copyPasteLogic.CopiedData.CopiedElement.ShouldBeNull();
    }

    private ComponentSave AddComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        _project.Components.Add(component);
        return component;
    }

    private void Cut(ElementSave element)
    {
        _selectedState.Setup(x => x.SelectedElement).Returns(element);
        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);
    }

    private void Paste(ITreeNode on)
    {
        _selectedState.Setup(x => x.SelectedTreeNode).Returns(on);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
    }

    private void VerifyOutput(string expected)
    {
        Mock<IOutputManager> output = _mocker.GetMock<IOutputManager>();
        output.Verify(x => x.AddOutput(expected), Times.Once);
        output.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }
}
