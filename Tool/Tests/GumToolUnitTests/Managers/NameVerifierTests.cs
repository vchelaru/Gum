using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Managers;
public class NameVerifierTests
{
    NameVerifier _nameVerifier;

    public NameVerifierTests()
    {
        _nameVerifier = new NameVerifier();
    }


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
    public void IsCategoryNameValid_ShouldReturnFalse_ForNamesWithMatchingWhitespace_Component()
    {
        ComponentSave component = new ComponentSave();
        component.Name = "TestComponent";
        component.Categories.Add(new StateSaveCategory { Name = "Existing_Category" });
        var isValid = _nameVerifier.IsCategoryNameValid("Existing Category", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("A category with the name Existing_Category is already defined in TestComponent");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForNamesWithMatchingWhitespace_Behavior()
    {
        BehaviorSave behavior = new BehaviorSave();
        behavior.Name = "TestComponent";
        behavior.Categories.Add(new StateSaveCategory { Name = "Existing_Category" });
        var isValid = _nameVerifier.IsCategoryNameValid("Existing Category", behavior, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("A category with the name Existing_Category is already defined in TestComponent");
    }

    #endregion
}
