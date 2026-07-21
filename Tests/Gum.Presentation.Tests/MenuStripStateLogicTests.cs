using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for MenuStripStateLogic, extracted out of
/// <c>MenuStripManager.RefreshUI</c> into the headless Gum.Presentation assembly (ADR-0005): the
/// enable/disable + header-text decisions for the menu strip's selection-dependent items.
/// </summary>
public class MenuStripStateLogicTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly MenuStripStateLogic _sut;

    public MenuStripStateLogicTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _projectManager = new Mock<IProjectManager>();

        _sut = new MenuStripStateLogic(_selectedState.Object, _projectManager.Object);
    }

    [Fact]
    public void GetRefreshState_ShouldDisableRemoveElement_WhenElementIsStandardElementSave()
    {
        StandardElementSave element = new();
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveElementHeader.ShouldBe("<no element selected>");
        result.RemoveElementEnabled.ShouldBeFalse();
    }

    [Fact]
    public void GetRefreshState_ShouldDisableRemoveState_WhenSelectedStateIsDefault()
    {
        StateSave stateSave = new() { Name = "Default" };
        _selectedState.Setup(x => x.SelectedStateSave).Returns(stateSave);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveStateHeader.ShouldBe("<no state selected>");
        result.RemoveStateEnabled.ShouldBeFalse();
    }

    [Fact]
    public void GetRefreshState_ShouldEnableRemoveElement_AndShowElementName_WhenNonStandardElementSelected()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveElementHeader.ShouldBe("MyComponent");
        result.RemoveElementEnabled.ShouldBeTrue();
    }

    [Fact]
    public void GetRefreshState_ShouldEnableRemoveState_AndShowCategoryName_WhenOnlyCategorySelected()
    {
        StateSaveCategory category = new() { Name = "Visibility" };
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveStateHeader.ShouldBe("Category Visibility");
        result.RemoveStateEnabled.ShouldBeTrue();
    }

    [Fact]
    public void GetRefreshState_ShouldEnableRemoveState_AndShowStateName_WhenNonDefaultStateSelected()
    {
        StateSave stateSave = new() { Name = "Hovered" };
        _selectedState.Setup(x => x.SelectedStateSave).Returns(stateSave);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveStateHeader.ShouldBe("State Hovered");
        result.RemoveStateEnabled.ShouldBeTrue();
    }

    [Fact]
    public void GetRefreshState_ShouldEnableRemoveVariable_AndShowVariableText_WhenBehaviorVariableSelected()
    {
        VariableSave variable = new() { Name = "MyVariable" };
        _selectedState.Setup(x => x.SelectedBehaviorVariable).Returns(variable);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveVariableHeader.ShouldBe(variable.ToString());
        result.RemoveVariableEnabled.ShouldBeTrue();
    }

    [Fact]
    public void GetRefreshState_ShouldReflectStandardsPaletteSetting_WhenDisabled()
    {
        _projectManager.Setup(x => x.EffectiveUseStandardsPalette).Returns(false);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.StandardsPaletteChecked.ShouldBeFalse();
    }

    [Fact]
    public void GetRefreshState_ShouldReflectStandardsPaletteSetting_WhenEnabled()
    {
        _projectManager.Setup(x => x.EffectiveUseStandardsPalette).Returns(true);

        MenuStripRefreshState result = _sut.GetRefreshState();

        result.StandardsPaletteChecked.ShouldBeTrue();
    }

    [Fact]
    public void GetRefreshState_ShouldShowNoElementSelected_WhenNothingSelected()
    {
        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveElementHeader.ShouldBe("<no element selected>");
        result.RemoveElementEnabled.ShouldBeFalse();
    }

    [Fact]
    public void GetRefreshState_ShouldShowNoStateSelected_WhenNothingSelected()
    {
        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveStateHeader.ShouldBe("<no state selected>");
        result.RemoveStateEnabled.ShouldBeFalse();
    }

    [Fact]
    public void GetRefreshState_ShouldShowNoVariableSelected_WhenNothingSelected()
    {
        MenuStripRefreshState result = _sut.GetRefreshState();

        result.RemoveVariableHeader.ShouldBe("<no behavior variable selected>");
        result.RemoveVariableEnabled.ShouldBeFalse();
    }
}
