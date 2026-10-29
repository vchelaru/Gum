using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The pure name rules shared by the tool and the CLI (#7025).
/// </summary>
public class NameRulesTests
{
    private readonly NameRules _rules = new NameRules();

    #region C# names

    [Theory]
    [InlineData("class")]
    [InlineData("while")]
    [InlineData("string")]
    public void IsValidCSharpName_OnAReservedKeyword_IsRejected(string name)
    {
        _rules.IsValidCSharpName(name, out string? whyNotValid, out CommonValidationError error).ShouldBeFalse();

        error.ShouldBe(CommonValidationError.ReservedCSharpKeyword);
        whyNotValid.ShouldBe("Name is a C# reserved keyword");
    }

    [Theory]
    [InlineData("1stButton", '1')]
    [InlineData("-Name", '-')]
    public void IsValidCSharpName_OnAnInvalidStartingCharacter_IsRejected(string name, char startingCharacter)
    {
        _rules.IsValidCSharpName(name, out string? whyNotValid, out CommonValidationError error).ShouldBeFalse();

        error.ShouldBe(CommonValidationError.InvalidStartingCharacterForCSharp);
        whyNotValid.ShouldBe($"Name may not begin with character {startingCharacter}");
    }

    [Theory]
    [InlineData("Button")]
    [InlineData("_button")]
    [InlineData("Class")]
    public void IsValidCSharpName_OnALegalName_IsAccepted(string name)
    {
        _rules.IsValidCSharpName(name, out string? whyNotValid, out CommonValidationError error).ShouldBeTrue();

        whyNotValid.ShouldBeNull();
        error.ShouldBe(CommonValidationError.None);
    }

    [Fact]
    public void IsValidCSharpName_OnAnEmptyName_IsRejectedInsteadOfThrowing()
    {
        _rules.IsValidCSharpName("", out string? whyNotValid, out CommonValidationError error).ShouldBeFalse();

        error.ShouldBe(CommonValidationError.IsEmpty);
        whyNotValid.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Common rules

    [Theory]
    [InlineData("", CommonValidationError.IsEmpty)]
    [InlineData("   ", CommonValidationError.IsEmpty)]
    [InlineData(null, CommonValidationError.IsEmpty)]
    [InlineData(" Name", CommonValidationError.StartsWithSpace)]
    [InlineData("Name ", CommonValidationError.EndsWithSpace)]
    [InlineData("Na/me", CommonValidationError.InvalidCharacter)]
    [InlineData("Na\U0001F600me", CommonValidationError.InvalidCharacter)]
    public void IsNameValidCommon_OnABadName_ReportsWhy(string? name, CommonValidationError expected)
    {
        _rules.IsNameValidCommon(name, out string whyNotValid, out CommonValidationError error).ShouldBeFalse();

        error.ShouldBe(expected);
        whyNotValid.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("Button")]
    [InlineData("My Button")]
    [InlineData("Button_2")]
    [InlineData("Ünïcode")]
    public void IsNameValidCommon_OnAGoodName_IsAccepted(string name)
    {
        _rules.IsNameValidCommon(name, out string whyNotValid, out CommonValidationError error).ShouldBeTrue();

        whyNotValid.ShouldBeEmpty();
        error.ShouldBe(CommonValidationError.None);
    }

    [Theory]
    [InlineData("my-file name", "my_file_name")]
    [InlineData("Ok_Name1", "Ok_Name1")]
    public void ToValidName_ReplacesEveryCharacterTheCommonRuleRejects(string candidate, string expected)
    {
        string result = _rules.ToValidName(candidate);

        result.ShouldBe(expected);
        _rules.IsNameValidCommon(result, out _, out _).ShouldBeTrue();
    }

    #endregion

    #region Windows reserved file names

    [Theory]
    [InlineData("con")]
    [InlineData("CON")]
    [InlineData("Lpt1")]
    [InlineData("aux")]
    public void IsFileNameWindowsReserved_OnAReservedName_IsReportedWhateverTheCase(string name)
    {
        _rules.IsFileNameWindowsReserved(name, out string? whyReserved).ShouldBeTrue();

        whyReserved.ShouldBe($"The name {name} is a reserved file name in Windows");
    }

    [Theory]
    [InlineData("Console")]
    [InlineData("com10")]
    [InlineData(null)]
    public void IsFileNameWindowsReserved_OnAnyOtherName_IsNotReported(string? name)
    {
        _rules.IsFileNameWindowsReserved(name, out string? whyReserved).ShouldBeFalse();

        whyReserved.ShouldBeNull();
    }

    #endregion

    #region Android files

    [Theory]
    [InlineData("good_name_1", true)]
    [InlineData("BadName", false)]
    [InlineData("bad-name", false)]
    public void IsNameValidAndroidFile_AllowsOnlyLowerCaseDigitsAndUnderscore(string name, bool expected)
    {
        _rules.IsNameValidAndroidFile(name, out string? whyNotValid).ShouldBe(expected);

        (whyNotValid == null).ShouldBe(expected);
    }

    #endregion

    #region Standardize

    [Fact]
    public void Standardize_ReplacesSpacesAndIgnoresCase()
    {
        _rules.Standardize("My Button").ShouldBe("my_button");
        _rules.Standardize(null).ShouldBeNull();
    }

    #endregion

    #region Behavior names

    [Theory]
    [InlineData("Other")]
    [InlineData("other")]
    [InlineData("OTHER")]
    public void IsBehaviorNameValid_OnAnotherBehaviorsName_IsRejectedCaseInsensitively(string name)
    {
        BehaviorSave clickable = new BehaviorSave { Name = "Clickable" };
        BehaviorSave other = new BehaviorSave { Name = "Other" };

        _rules.IsBehaviorNameValid(name, clickable, [clickable, other], out string? whyNotValid).ShouldBeFalse();

        whyNotValid.ShouldBe("There is already a behavior named Other so this name can't be used.");
    }

    [Fact]
    public void IsBehaviorNameValid_OnADuplicateWhenAdding_IsRejected()
    {
        BehaviorSave clickable = new BehaviorSave { Name = "Clickable" };

        _rules.IsBehaviorNameValid("Clickable", null, [clickable], out string? whyNotValid).ShouldBeFalse();

        whyNotValid.ShouldBe("There is already a behavior named Clickable so this name can't be used.");
    }

    [Fact]
    public void IsBehaviorNameValid_OnSpaceVersusUnderscore_IsRejected()
    {
        BehaviorSave existing = new BehaviorSave { Name = "My_Behavior" };

        _rules.IsBehaviorNameValid("My Behavior", null, [existing], out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Clickable")]
    [InlineData("clickable")]
    [InlineData("Pressable")]
    public void IsBehaviorNameValid_OnTheBehaviorsOwnNameOrAFreeName_IsAccepted(string name)
    {
        BehaviorSave clickable = new BehaviorSave { Name = "Clickable" };
        BehaviorSave other = new BehaviorSave { Name = "Other" };

        _rules.IsBehaviorNameValid(name, clickable, [clickable, other], out string? whyNotValid).ShouldBeTrue();

        whyNotValid.ShouldBeNull();
    }

    [Theory]
    [InlineData("con")]
    [InlineData("Na/me")]
    [InlineData("")]
    public void IsBehaviorNameValid_OnANameTheCommonOrFileRulesReject_IsRejected(string name)
    {
        _rules.IsBehaviorNameValid(name, null, [], out string? whyNotValid).ShouldBeFalse();

        whyNotValid.ShouldNotBeNullOrEmpty();
    }

    #endregion
}
