using Gum.Dialogs;
using Gum.Managers;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.ViewModels.Dialogs;
public class RenameElementDialogViewModelTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<INameVerifier> _nameVerifier;
    RenameElementDialogViewModel _viewModel;

    public RenameElementDialogViewModelTests()
    {
        _mocker = new AutoMocker();
        
        _viewModel = _mocker.CreateInstance<RenameElementDialogViewModel>();

        _nameVerifier = _mocker.GetMock<INameVerifier>();
    }

    [Fact]
    public void Validate_ShouldCheckForDuplicateNames()
    {
        _viewModel.ElementSave = new Gum.DataTypes.ComponentSave
        {
            Name = "Folder/OldName"
        };

        ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(new Gum.DataTypes.ComponentSave
        {
            Name = "Folder/OldName"
        });

        string? whyNotValid = "Cannot be duplicate.";

        _nameVerifier
            .Setup(x => x.IsElementNameValid(
                "OldName",
                "Folder/",
                _viewModel.ElementSave,
                out whyNotValid))
            .Returns(false);


        _viewModel.Validate();

        _viewModel.Error.ShouldBe("Cannot be duplicate.");

        
    }
}
