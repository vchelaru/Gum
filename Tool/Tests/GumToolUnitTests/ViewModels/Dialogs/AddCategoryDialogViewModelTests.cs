using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.ViewModels.Dialogs;
public class AddCategoryDialogViewModelTests
{
    private readonly AutoMocker mocker;

    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<INameVerifier> _nameVerifier;
    AddCategoryDialogViewModel _viewModel;

    public AddCategoryDialogViewModelTests()
    {
        mocker = new();

        _viewModel = mocker.CreateInstance<AddCategoryDialogViewModel>();

        _selectedState = mocker.GetMock<ISelectedState>();
        _nameVerifier = mocker.GetMock<INameVerifier>();
    }

    [Fact]
    public void Validate_ShouldValidateComponentCategories_UsingNameVerifier()
    {
        string errorMessage = "Invalid category name";
        _nameVerifier.Setup(
            x => x.IsCategoryNameValid(
                It.IsAny<string>(), 
                It.IsAny<ElementSave>(), 
                out errorMessage)) ;

        _selectedState
            .Setup(x => x.SelectedStateContainer)
            .Returns(new ComponentSave());

        _viewModel.Value = "NewCategory";

        _viewModel.Validate();

        string? error = _viewModel.Error;
        error.ShouldBe("Invalid category name");
    }

    [Fact]
    public void Validate_ShouldValidateBehaviorCategories_UsingNameVerifier()
    {
        string errorMessage = "Invalid behavior category name";
        _nameVerifier.Setup(
            x => x.IsCategoryNameValid(
                It.IsAny<string>(), 
                It.IsAny<BehaviorSave>(), 
                out errorMessage));
        
        _selectedState
            .Setup(x => x.SelectedStateContainer)
            .Returns(new BehaviorSave());

        _viewModel.Value = "NewCategory";
        _viewModel.Validate();
        string? error = _viewModel.Error;
        error.ShouldBe("Invalid behavior category name");
    }
}
