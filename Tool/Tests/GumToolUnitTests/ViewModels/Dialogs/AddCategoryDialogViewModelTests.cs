using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.ViewModels.Dialogs;
public class AddCategoryDialogViewModelTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<INameVerifier> _nameVerifier;
    AddCategoryDialogViewModel _viewModel;

    public AddCategoryDialogViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _elementCommands = new Mock<IElementCommands>();
        _undoManager = new Mock<IUndoManager>();
        _nameVerifier = new Mock<INameVerifier>();

        _viewModel = new AddCategoryDialogViewModel(
            _selectedState.Object,
            _elementCommands.Object,
            _undoManager.Object,
            _nameVerifier.Object);
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

        var error = _viewModel.Error;
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
        var error = _viewModel.Error;
        error.ShouldBe("Invalid behavior category name");
    }
}
