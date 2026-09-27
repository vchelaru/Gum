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

    [Theory]
    [InlineData("Height = Components/Button.LabelWidth", SideOfEquals.Right, "Height = Components/Button.TextWidth")]
    [InlineData("LabelWidth=Other.Width", SideOfEquals.Left, "TextWidth=Other.Width")]
    [InlineData("LabelWidth = Other.Count == 0 ? 1 : 2", SideOfEquals.Left, "TextWidth = Other.Count == 0 ? 1 : 2")]
    public void ApplyVariableRenameChanges_ReferenceLine_ReplacesOnlyTheName(string line, SideOfEquals side, string expected)
    {
        ComponentSave panel = new ComponentSave { Name = "Panel" };
        VariableListSave<string> references = new VariableListSave<string> { Name = "VariableReferences" };
        references.Value.Add(line);
        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableReferenceChanges.Add(new VariableReferenceChange
        {
            Container = panel,
            VariableReferenceList = references,
            LineIndex = 0,
            ChangedSide = side
        });
        HashSet<ElementSave> elementsNeedingSave = new HashSet<ElementSave>();

        _renameLogic.ApplyVariableRenameChanges(changes, "LabelWidth", "TextWidth", elementsNeedingSave);

        references.Value.ShouldBe(new[] { expected });
        elementsNeedingSave.ShouldBe(new ElementSave[] { panel });
    }

    [Fact]
    public void ApplyElementReferences_ReferenceLine_KeepsItsSpacing()
    {
        ComponentSave button = new ComponentSave { Name = "PrimaryButton" };
        ComponentSave panel = new ComponentSave { Name = "Panel" };
        VariableListSave<string> references = new VariableListSave<string> { Name = "VariableReferences" };
        references.Value.Add("Height=Components/Button.Width");
        ElementReferences changes = new ElementReferences();
        changes.VariableReferenceChanges.Add(new VariableReferenceChange
        {
            Container = panel,
            VariableReferenceList = references,
            LineIndex = 0,
            ChangedSide = SideOfEquals.Right
        });

        _renameLogic.ApplyElementReferences(changes, button, "Button");

        references.Value.ShouldBe(new[] { "Height=Components/PrimaryButton.Width" });
    }
}
