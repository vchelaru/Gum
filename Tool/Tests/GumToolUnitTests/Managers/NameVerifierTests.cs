using Gum.DataTypes;
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
    public void IsCategoryNameValid_ShouldReturnTrue_ForValidNamej()
    {
        ComponentSave component = new ComponentSave();

        var isValid = _nameVerifier.IsCategoryNameValid("ValidCategory", component, out string whyNotValid);

        isValid.ShouldBeTrue();
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForEmptyName()
    {
        ComponentSave component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForWhitespaceName()
    {
        ComponentSave component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("   ", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("Empty names are not valid");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForInvalidCharacters()
    {
        ComponentSave component = new ComponentSave();
        var isValid = _nameVerifier.IsCategoryNameValid("Invalid@Category", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("The name can't contain invalid character @");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForDuplicateName()
    {
        ComponentSave component = new ComponentSave();
        component.Name = "TestComponent";
        component.Categories.Add(new StateSaveCategory { Name = "ExistingCategory" });
        var isValid = _nameVerifier.IsCategoryNameValid("ExistingCategory", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("A category with the name ExistingCategory is already defined in TestComponent");
    }

    [Fact]
    public void IsCategoryNameValid_ShouldReturnFalse_ForNamesWithMatchingWhitespace()
    {
        ComponentSave component = new ComponentSave();
        component.Name = "TestComponent";
        component.Categories.Add(new StateSaveCategory { Name = "Existing_Category" });
        var isValid = _nameVerifier.IsCategoryNameValid("Existing Category", component, out string whyNotValid);
        isValid.ShouldBeFalse();
        whyNotValid.ShouldBe("A category with the name Existing_Category is already defined in TestComponent");
    }

    #endregion
}
