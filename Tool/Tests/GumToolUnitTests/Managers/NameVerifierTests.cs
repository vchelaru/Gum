using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Managers;
public class NameVerifierTests
{
    private readonly NameVerifier _nameVerifier;

    public NameVerifierTests()
    {
        _nameVerifier = new NameVerifier();
    }

    #region ElementSave

    [Fact]
    public void IsElementNameValid_ShouldReturnTrue_ForValidComponentName()
    {
        var isValid = _nameVerifier.IsElementNameValid("ValidComponent", null, null, out string whyNotValid);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void IsElementNameValid_ShouldReturnFalse_ForEmptyName()
    {
        var isValid = _nameVerifier.IsElementNameValid("", null, null, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsElementNameValid_ShouldReturnFalse_ForWhitespaceName()
    {
        var isValid = _nameVerifier.IsElementNameValid("   ", null, null, out string whyNotValid);
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

        var isValid = _nameVerifier.IsElementNameValid("ElementName", "Folder", null, out string whyNotValid);
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
}
