using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>The message each name rule gives; the dialog, the service and <c>gumcli new</c> all show these.</summary>
public class NewProjectNameRulesTests
{
    [Theory]
    [InlineData("CON", "CON is a reserved Windows name. Choose another name.")]
    [InlineData("nul", "nul is a reserved Windows name. Choose another name.")]
    [InlineData("COM1", "COM1 is a reserved Windows name. Choose another name.")]
    [InlineData("Aux.txt", "Aux is a reserved Windows name. Choose another name.")]
    [InlineData("COM\u00B9", "COM\u00B9 is a reserved Windows name. Choose another name.")]
    [InlineData("com\u00B2", "com\u00B2 is a reserved Windows name. Choose another name.")]
    [InlineData("COM\u00B3", "COM\u00B3 is a reserved Windows name. Choose another name.")]
    [InlineData("LPT\u00B3.txt", "LPT\u00B3 is a reserved Windows name. Choose another name.")]
    [InlineData("a<b", "Invalid character '<' in the name.")]
    [InlineData("a|b", "Invalid character '|' in the name.")]
    [InlineData("a\tb", "Invalid control character in the name.")]
    [InlineData("Game.", "A folder name cannot end with a dot or a space.")]
    [InlineData("Game ", "A folder name cannot end with a dot or a space.")]
    public void GetProblem_NamesTheRuleThatWasHit(string name, string expected)
    {
        NewProjectNameRules.GetProblem(name, includeCodeProject: false).ShouldBe(expected);
    }

    [Theory]
    [InlineData(false, "The name is too long: 250 bytes at most.")]
    [InlineData(true, "The name is too long: 248 bytes at most.")]
    public void GetProblem_ATooLongName_SaysTheLimit(bool codeProject, string expected)
    {
        NewProjectNameRules.GetProblem(new string('a', 251), codeProject).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Game")]
    [InlineData("COM10")]
    [InlineData("Console")]
    public void GetProblem_AValidName_IsNull(string name)
    {
        NewProjectNameRules.GetProblem(name, includeCodeProject: true).ShouldBeNull();
    }
}
