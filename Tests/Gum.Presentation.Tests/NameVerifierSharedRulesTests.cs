using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The tool's <see cref="NameVerifier"/> must give the same answers as the <see cref="NameRules"/> the CLI
/// uses, for the rules they share (#7025).
/// </summary>
public class NameVerifierSharedRulesTests : BaseTestClass
{
    private readonly NameVerifier _nameVerifier = new NameVerifier(
        new Mock<IVariableSaveLogic>().Object, new Mock<IPluginManager>().Object, new Mock<ISelectedState>().Object);
    private readonly NameRules _rules = new NameRules();

    public static TheoryData<string> Names => new TheoryData<string>
    {
        "", " ", "Button", "My Button", "_x", "1st", "class", "Class", "con", "LPT1", "a/b", " lead", "trail ",
        "Ünï", "x\U0001F600", "-", "While", "string", "Other", "other", "my_behavior", "My Behavior"
    };

    [Theory]
    [MemberData(nameof(Names))]
    public void IsValidCSharpName_MatchesTheSharedRule(string name)
    {
        bool toolResult = _nameVerifier.IsValidCSharpName(name, out string? toolWhy, out CommonValidationError toolError);
        bool sharedResult = _rules.IsValidCSharpName(name, out string? sharedWhy, out CommonValidationError sharedError);

        toolResult.ShouldBe(sharedResult);
        toolWhy.ShouldBe(sharedWhy);
        toolError.ShouldBe(sharedError);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void IsNameValidCommon_MatchesTheSharedRule(string name)
    {
        bool toolResult = _nameVerifier.IsNameValidCommon(name, out string toolWhy, out CommonValidationError toolError);
        bool sharedResult = _rules.IsNameValidCommon(name, out string sharedWhy, out CommonValidationError sharedError);

        toolResult.ShouldBe(sharedResult);
        toolWhy.ShouldBe(sharedWhy);
        toolError.ShouldBe(sharedError);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void IsNameValidAndroidFile_MatchesTheSharedRule(string name)
    {
        bool toolResult = _nameVerifier.IsNameValidAndroidFile(name, out string? toolWhy);
        bool sharedResult = _rules.IsNameValidAndroidFile(name, out string? sharedWhy);

        toolResult.ShouldBe(sharedResult);
        toolWhy.ShouldBe(sharedWhy);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void IsBehaviorNameValid_MatchesTheSharedRule(string name)
    {
        BehaviorSave clickable = new BehaviorSave { Name = "Clickable" };
        BehaviorSave other = new BehaviorSave { Name = "Other" };
        BehaviorSave underscored = new BehaviorSave { Name = "My_Behavior" };
        GumProjectSave project = new GumProjectSave();
        project.Behaviors.Add(clickable);
        project.Behaviors.Add(other);
        project.Behaviors.Add(underscored);
        ObjectFinder.Self.GumProjectSave = project;

        bool toolResult = _nameVerifier.IsBehaviorNameValid(name, clickable, out string? toolWhy);
        bool sharedResult = _rules.IsBehaviorNameValid(name, clickable, project.Behaviors, out string? sharedWhy);

        toolResult.ShouldBe(sharedResult);
        toolWhy.ShouldBe(sharedWhy);
    }

    [Fact]
    public void IsValidCSharpName_OnAnEmptyName_IsRejectedInsteadOfThrowing()
    {
        _nameVerifier.IsValidCSharpName("", out _, out CommonValidationError error).ShouldBeFalse();

        error.ShouldBe(CommonValidationError.IsEmpty);
    }
}
