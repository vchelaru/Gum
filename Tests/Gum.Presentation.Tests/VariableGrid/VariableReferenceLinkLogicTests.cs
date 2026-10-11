using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

public class VariableReferenceLinkLogicTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly VariableReferenceLinkLogic _sut;
    private readonly ComponentSave _button;
    private readonly InstanceSave _leader;
    private readonly ComponentSave _styles;
    private readonly InstanceSave _primary;

    public VariableReferenceLinkLogicTests()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        _button = new ComponentSave { Name = "Components/Button" };
        _leader = new InstanceSave { Name = "Leader", BaseType = "Container", ParentContainer = _button };
        _button.Instances.Add(_leader);

        _styles = new ComponentSave { Name = "Components/Styles" };
        _primary = new InstanceSave { Name = "Primary", BaseType = "Container", ParentContainer = _styles };
        _styles.Instances.Add(_primary);

        project.Components.Add(_button);
        project.Components.Add(_styles);
        ObjectFinder.Self.GumProjectSave = project;

        _sut = new VariableReferenceLinkLogic(_selectedState.Object, ObjectFinder.Self);
    }

    [Fact]
    public void GetLink_ShouldSelectInstance_WhenPathNamesSiblingInstance()
    {
        DetailTextLink? link = _sut.GetLink("Leader.X", _button);

        link.ShouldNotBeNull();
        link.Text.ShouldBe("Leader.X");
        link.Navigate();

        _selectedState.VerifySet(state => state.SelectedInstance = _leader);
    }

    [Fact]
    public void GetLink_ShouldSelectOwner_WhenPathIsBareVariable()
    {
        DetailTextLink? link = _sut.GetLink("Width", _button);

        link.ShouldNotBeNull();
        link.Navigate();

        _selectedState.VerifySet(state => state.SelectedElement = _button);
    }

    [Fact]
    public void GetLink_ShouldSelectInstanceInOtherElement_WhenPathIsQualifiedByElement()
    {
        DetailTextLink? link = _sut.GetLink("Components/Styles.Primary.FillRed", _button);

        link.ShouldNotBeNull();
        link.Navigate();

        _selectedState.VerifySet(state => state.SelectedElement = _styles);
        _selectedState.VerifySet(state => state.SelectedInstance = _primary);
    }

    [Theory]
    [InlineData("Width + 10")]
    [InlineData("Leader.Width * 2")]
    [InlineData("@Height")]
    [InlineData("global::Localization.CurrentLanguage")]
    [InlineData("Missing.X")]
    [InlineData("42")]
    public void GetLink_ShouldReturnNull_WhenRightSideIsNotAPathToSomethingThatExists(string rightSide)
    {
        _sut.GetLink(rightSide, _button).ShouldBeNull();
    }
}
