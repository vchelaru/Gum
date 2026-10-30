using Gum.ProjectServices;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The one rule for which screen or file is a theme's demo screen.</summary>
public class DemoScreenRuleTests
{
    [Theory]
    [InlineData("DemoScreenGum", true)]
    [InlineData("Bubblegum/DemoScreenGum", true)]
    [InlineData("A/B/DemoScreenGum", true)]
    [InlineData("MainScreen", false)]
    [InlineData("MyDemoScreenGum", false)]
    [InlineData("DemoScreenGum2", false)]
    [InlineData("DemoScreenGum/Other", false)]
    public void IsDemoScreenName_MatchesTheScreenWithOrWithoutAThemeFolder(string name, bool expected)
    {
        DemoScreenRule.IsDemoScreenName(name).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Screens/DemoScreenGum.gusj", true)]
    [InlineData("Screens/Bubblegum/DemoScreenGum.gusj", true)]
    [InlineData("Content/GumProject/Screens/Bubblegum/DemoScreenGum.gusx", true)]
    [InlineData(@"Content\GumProject\Screens\Bubblegum\DemoScreenGum.gusj", true)]
    [InlineData("Screens/KeyboardScreenGum.gusj", false)]
    [InlineData("Screens/Bubblegum/StylesScreen.gusj", false)]
    [InlineData("Components/DemoScreenGum.gucj", false)]
    [InlineData("DemoScreenGum.gusj", false)]
    [InlineData("MyScreens/DemoScreenGum.gusj", false)]
    public void IsDemoScreenFile_MatchesTheScreenFileWhateverTheSeparatorAndTheme(string path, bool expected)
    {
        DemoScreenRule.IsDemoScreenFile(path).ShouldBe(expected);
    }
}
