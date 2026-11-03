using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class GameSetupCodeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.GumForms)]
    public void Generate_XnaFamilyOrUnset_ShowsTheMonoGameSetupWithTheScreen(OutputLibrary? library)
    {
        string text = GameSetupCode.Generate(library, "My.gumx", "Screens/TitleScreen").ShouldNotBeNull();

        text.ShouldContain("GumUI.Initialize(this, \"My.gumx\");");
        text.ShouldContain("var screen = new TitleScreen();");
        text.ShouldContain("screen.AddToRoot();");
    }

    [Fact]
    public void Generate_XnaSetup_ContainsEveryPreviewDefaultWithItsExplanation()
    {
        string text = GameSetupCode.Generate(null, "My.gumx", null).ShouldNotBeNull();

        foreach (PreviewDefault item in PreviewDefaults.All)
        {
            text.ShouldContain("// " + item.Explanation);
            foreach (string line in item.CodeLines)
            {
                text.ShouldContain(line);
            }
        }
    }

    [Fact]
    public void Generate_XnaSetup_PlacesConstructorDefaultsBeforeInitializeDefaults()
    {
        string text = GameSetupCode.Generate(null, "My.gumx", null).ShouldNotBeNull();

        text.IndexOf("GraphicsProfile.HiDef").ShouldBeLessThan(text.IndexOf("GumUI.Initialize"));
        text.IndexOf("GumUI.Initialize").ShouldBeLessThan(text.IndexOf("ShapeRenderer"));
    }

    [Fact]
    public void Generate_NoScreenSelected_AsksForOne()
    {
        string text = GameSetupCode.Generate(null, "My.gumx", null).ShouldNotBeNull();

        text.ShouldNotContain("new ");
        text.ShouldContain("Select a Screen");
    }

    [Theory]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Skia)]
    [InlineData(OutputLibrary.Silk)]
    public void Generate_NonMonoGameRuntime_ShowsOnlyANoteWithNoCode(OutputLibrary library)
    {
        string text = GameSetupCode.Generate(library, "My.gumx", "TitleScreen").ShouldNotBeNull();

        text.ShouldContain("Preview runs on MonoGame");
        text.ShouldContain(library.GetName());
        foreach (string line in text.Split('\n'))
        {
            line.TrimStart().ShouldStartWith("//");
        }
    }

    [Theory]
    [InlineData(OutputLibrary.XamarinForms)]
    [InlineData(OutputLibrary.WPF)]
    [InlineData(OutputLibrary.Maui)]
    public void Generate_LegacyLibrary_HidesTheSection(OutputLibrary library)
    {
        GameSetupCode.Generate(library, "My.gumx", "TitleScreen").ShouldBeNull();
    }
}
