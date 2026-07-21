using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="NameVerifier"/>'s name-validation logic after its relocation to
/// Gum.Presentation (ADR-0005, #3889) — the class had no WPF/WinForms-typed members; every
/// dependency (<see cref="IVariableSaveLogic"/>, <see cref="IPluginManager"/>,
/// <see cref="StandardElementsManager"/>, <see cref="ObjectFinder"/>) was already headless. The one
/// wrinkle: <see cref="NameVerifier.IsVariableNameValid"/> used to resolve <see cref="ISelectedState"/>
/// via an ambient tool-only extension method (<c>ElementSaveExtensionMethodsGumTool.GetVariableFromThisOrBase</c>,
/// which called <c>Locator</c> internally) instead of taking it as a dependency; it is now a real
/// constructor parameter, which is also what makes the "existing active variable" branch below
/// testable for the first time (it previously required a live <c>Locator</c> container).
/// </summary>
public class NameVerifierTests : BaseTestClass
{
    private readonly NameVerifier _nameVerifier;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<IVariableSaveLogic> _variableSaveLogic;
    private readonly Mock<ISelectedState> _selectedState;

    public NameVerifierTests()
    {
        _pluginManager = new Mock<IPluginManager>();
        _variableSaveLogic = new Mock<IVariableSaveLogic>();
        _selectedState = new Mock<ISelectedState>();
        _nameVerifier = new NameVerifier(_variableSaveLogic.Object, _pluginManager.Object, _selectedState.Object);
    }

    #region ElementSave

    [Fact]
    public void IsElementNameValid_ShouldReturnTrue_ForValidComponentName()
    {
        var isValid = _nameVerifier.IsElementNameValid("ValidComponent", null, null, out _);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void IsElementNameValid_ShouldReturnFalse_ForEmptyName()
    {
        var isValid = _nameVerifier.IsElementNameValid("", null, null, out string? whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsElementNameValid_ShouldReturnFalse_ForWhitespaceName()
    {
        var isValid = _nameVerifier.IsElementNameValid("   ", null, null, out string? whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsElementNameValid_ShouldReturnFalse_ForMatchingNameInFolder()
    {
        GumProjectSave project = new();
        ObjectFinder.Self.GumProjectSave = project;

        project.Components.Add(new ComponentSave
        {
            Name = "Folder/ElementName"
        });

        var isValid = _nameVerifier.IsElementNameValid("ElementName", "Folder", null, out string? whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("There is a component named Folder/ElementName so this name can't be used.");
    }

    #endregion

    #region Variables

    [Fact]
    public void IsVariableNameValid_ShouldReturnFalse_ForVariablesWithSpaces()
    {
        var isValid = _nameVerifier.IsVariableNameValid("Name with space", new ComponentSave(), new VariableSave(), out string whyNotValid);

        isValid.ShouldBeFalse("Because spaces are not allowed. This makes variable references difficult to parse");

        whyNotValid.ShouldBe("Variable names cannot contain spaces");
    }

    [Fact]
    public void IsVariableNameValid_ShouldReturnFalse_ForExistingActiveVariable_InDefaultState_WhenElementIsNotSelected()
    {
        ComponentSave element = new ComponentSave { Name = "MyComponent" };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        VariableSave existingVariable = new VariableSave { Name = "ExistingVar" };
        element.DefaultState.Variables.Add(existingVariable);

        _selectedState.SetupGet(x => x.SelectedElement).Returns((ElementSave?)null);
        _selectedState.SetupGet(x => x.SelectedStateSave).Returns((StateSave?)null);
        _variableSaveLogic.Setup(x => x.GetIfVariableIsActive(existingVariable, element, null)).Returns(true);

        bool isValid = _nameVerifier.IsVariableNameValid("ExistingVar", element, new VariableSave(), out string whyNotValid);

        isValid.ShouldBeFalse("Because the element is not the selected element, so the default state (not any selected state) should be searched.");
        whyNotValid.ShouldBe("The variable name ExistingVar is already used");
    }

    [Fact]
    public void IsVariableNameValid_ShouldReturnFalse_ForExistingActiveVariable_InSelectedState_WhenElementIsSelected()
    {
        ComponentSave element = new ComponentSave { Name = "MyComponent" };
        StateSave selectedStateSave = new StateSave();
        VariableSave existingVariable = new VariableSave { Name = "ExistingVar" };
        selectedStateSave.Variables.Add(existingVariable);
        // Deliberately NOT added to element.DefaultState, to prove the selected state (not the
        // default state) is what gets searched when the element is currently selected.

        _selectedState.SetupGet(x => x.SelectedElement).Returns(element);
        _selectedState.SetupGet(x => x.SelectedStateSave).Returns(selectedStateSave);
        _variableSaveLogic.Setup(x => x.GetIfVariableIsActive(existingVariable, element, null)).Returns(true);

        bool isValid = _nameVerifier.IsVariableNameValid("ExistingVar", element, new VariableSave(), out string whyNotValid);

        isValid.ShouldBeFalse("Because the element is the selected element with a selected state, so that state should be searched.");
        whyNotValid.ShouldBe("The variable name ExistingVar is already used");
    }

    [Fact]
    public void IsVariableNameValid_ShouldReturnTrue_WhenExistingVariableIsNotActive()
    {
        ComponentSave element = new ComponentSave { Name = "MyComponent" };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        VariableSave existingVariable = new VariableSave { Name = "ExistingVar" };
        element.DefaultState.Variables.Add(existingVariable);

        _selectedState.SetupGet(x => x.SelectedElement).Returns((ElementSave?)null);
        _selectedState.SetupGet(x => x.SelectedStateSave).Returns((StateSave?)null);
        _variableSaveLogic.Setup(x => x.GetIfVariableIsActive(existingVariable, element, null)).Returns(false);

        bool isValid = _nameVerifier.IsVariableNameValid("ExistingVar", element, new VariableSave(), out string whyNotValid);

        isValid.ShouldBeTrue("Because an inactive variable (e.g. a leftover from a type change) should not block reuse of its name.");
    }

    #endregion

    #region StateSaveCategory

    [Fact]
    public void IsCategoryNameValid_ShouldReturnTrue_ForValidComponentName()
    {
        IStateContainer component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("ValidCategory", component, out string whyNotValid);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnTrue_ForValidBehaviorName()
    {
        IStateContainer behavior = new BehaviorSave();
        var isValid = _nameVerifier.IsCategoryNameValid("ValidCategory", behavior, out string whyNotValid);
        isValid.ShouldBeTrue();
    }
    
    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForEmptyName_Component()
    {
        IStateContainer component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForEmptyName_Behavior()
    {
        IStateContainer behavior = new BehaviorSave();
        var isValid = _nameVerifier.IsCategoryNameValid("", behavior, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForWhitespaceName_Component()
    {
        ComponentSave component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("   ", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForWhitespaceName_Behavior()
    {
        IStateContainer behavior = new BehaviorSave();
        var isValid = _nameVerifier.IsCategoryNameValid("   ", behavior, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForInvalidCharacters_Components()
    {
        IStateContainer component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("Invalid@Category", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("The name can't contain invalid character @");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForInvalidCharacters_Behaviors()
    {
        IStateContainer behavior = new BehaviorSave();
        var isValid = _nameVerifier.IsCategoryNameValid("Invalid@Category", behavior, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("The name can't contain invalid character @");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForDuplicateName_Component()
    {
        ComponentSave component = new ComponentSave();
        component.Name = "TestComponent";
        component.Categories.Add(new StateSaveCategory { Name = "ExistingCategory" });
        var isValid = _nameVerifier.IsCategoryNameValid("ExistingCategory", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("A category with the name ExistingCategory is already defined in TestComponent");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForDuplicateName_Behavior()
    {
        BehaviorSave behavior = new BehaviorSave();
        behavior.Name = "TestComponent";
        behavior.Categories.Add(new StateSaveCategory { Name = "ExistingCategory" });
        var isValid = _nameVerifier.IsCategoryNameValid("ExistingCategory", behavior, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("A category with the name ExistingCategory is already defined in TestComponent");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForNamesWithSpaces()
    {
        var isValid = _nameVerifier.IsCategoryNameValid("name with spaces", new ComponentSave(), out string whyNotValid);

        isValid.ShouldBeFalse("Because spaces are not allowed. This makes variable references difficult to parse");

        whyNotValid.ShouldBe("Category names cannot contain spaces");
    }
    
    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForSameNameAsContainer()
    {
        IStateContainer component = new ComponentSave { Name = "ContainerName" };
        bool isValid = _nameVerifier.IsCategoryNameValid(component.Name, component, out string whyNotValid);

        isValid.ShouldBeFalse("Because category name can't be the same as it container's. " +
                              "This would cause compiler errors when generating Forms code.");
        
        whyNotValid.ShouldBe("Category name cannot be the same as its container's");
    }

    #endregion

    #region StateSave

    [Fact]
    public void IsStateNameValid_ShouldBeTrue_ForValidName()
    {
        StateSaveCategory category = new();
        StateSave state = new();
        bool isValid = _nameVerifier.IsStateNameValid(category.Name, category, state, out _);
        isValid.ShouldBeTrue();
    }
    
    [Fact]
    public void IsStateNameValid_ShouldBeFalse_ForSameAsCategory()
    {
        StateSaveCategory category = new() { Name = "CategoryName" };
        StateSave state = new();
        bool isValid = _nameVerifier.IsStateNameValid(category.Name, category, state, out string whyNotValid);
        
        isValid.ShouldBeFalse("Because category name can't be the same as it category's. " +
                              "This would cause compiler errors when generating Forms code.");
        
        whyNotValid.ShouldBe("State name cannot be the same as its category's");
    }

    #endregion

    #region TopLevelNameCollisions

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_IfCollidesWithAnimation()
    {
        var element = new ComponentSave();
        _pluginManager.Setup(x => x.FillTopLevelNames(element, It.IsAny<List<TopLevelName>>()))
            .Callback<ElementSave, List<TopLevelName>>((e, names) =>
            {
                names.Add(new TopLevelName("Anim1", "Animation", new object()));
            });

        var isValid = _nameVerifier.IsCategoryNameValid("Anim1", element, out string whyNotValid);

        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("The name Anim1 is already used by a(n) Animation");
    }

    [Fact]
    public void IsInstanceNameValid_ShouldReturnFalse_IfCollidesWithCategory()
    {
        var element = new ComponentSave();
        element.Categories.Add(new StateSaveCategory { Name = "Category1" });

        var isValid = _nameVerifier.IsInstanceNameValid("Category1", new InstanceSave(), element, out string whyNotValid);

        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("The name Category1 is already used by a(n) Category");
    }

    [Fact]
    public void IsVariableNameValid_ShouldReturnFalse_IfCollidesWithAnimation()
    {
        var element = new ComponentSave();
        _pluginManager.Setup(x => x.FillTopLevelNames(element, It.IsAny<List<TopLevelName>>()))
            .Callback<ElementSave, List<TopLevelName>>((e, names) =>
            {
                names.Add(new TopLevelName("Anim1", "Animation", new object()));
            });

        var isValid = _nameVerifier.IsVariableNameValid("Anim1", element, new VariableSave(), out string whyNotValid);

        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("The name Anim1 is already used by a(n) Animation");
    }

    #endregion
}
