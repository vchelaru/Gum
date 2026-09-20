using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests.Logic;

/// <summary>
/// Pins <see cref="RenameLogic.AskToRenameStateCategory"/>'s ahead-of-time name validation --
/// see https://github.com/vchelaru/Gum/issues/4889
/// </summary>
public class RenameLogicTests
{
    private readonly AutoMocker _mocker;
    private readonly RenameLogic _renameLogic;

    public RenameLogicTests()
    {
        _mocker = new AutoMocker();
        _renameLogic = _mocker.CreateInstance<RenameLogic>();
    }

    [Fact]
    public void AskToRenameStateCategory_ElementOwner_PassesValidatorThatUsesNameVerifier()
    {
        var element = new ComponentSave { Name = "MyComponent" };
        var category = new StateSaveCategory { Name = "OldCategory" };
        element.Categories.Add(category);

        _mocker.GetMock<IDeleteLogic>()
            .Setup(d => d.GetBehaviorsNeedingCategory(category, element))
            .Returns(new List<BehaviorSave>());
        _mocker.GetMock<IReferenceFinder>()
            .Setup(r => r.GetReferencesToStateCategory(element, category, "OldCategory"))
            .Returns(new CategoryReferences());

        GetUserStringOptions capturedOptions = null;
        _mocker.GetMock<IDialogService>()
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Callback<string, string, GetUserStringOptions>((_, _, options) => capturedOptions = options)
            .Returns((string)null);

        string whyNotValid = "bad name";
        _mocker.GetMock<INameVerifier>()
            .Setup(v => v.IsCategoryNameValid("Invalid@Name", element, out whyNotValid, category))
            .Returns(false);

        _renameLogic.AskToRenameStateCategory(category, element);

        capturedOptions.ShouldNotBeNull();
        capturedOptions.Validator.ShouldNotBeNull();
        capturedOptions.Validator!("Invalid@Name").ShouldBe("bad name");
    }

    [Fact]
    public void AskToRenameStateCategory_BehaviorOwner_PassesValidatorThatUsesNameVerifier()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        var category = new StateSaveCategory { Name = "OldCategory" };
        behavior.Categories.Add(category);

        GetUserStringOptions capturedOptions = null;
        _mocker.GetMock<IDialogService>()
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Callback<string, string, GetUserStringOptions>((_, _, options) => capturedOptions = options)
            .Returns((string)null);

        string whyNotValid = "bad name";
        _mocker.GetMock<INameVerifier>()
            .Setup(v => v.IsCategoryNameValid("Invalid@Name", behavior, out whyNotValid, category))
            .Returns(false);

        _renameLogic.AskToRenameStateCategory(category, behavior);

        capturedOptions.ShouldNotBeNull();
        capturedOptions.Validator.ShouldNotBeNull();
        capturedOptions.Validator!("Invalid@Name").ShouldBe("bad name");
    }
}
