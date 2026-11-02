#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>The theme chosen for a new project, on every platform.</summary>
public class NewProjectThemeTests : NewProjectServiceTestBase
{
    [Fact]
    public async Task Execute_EveryTheme_OnARotatingPlatform_LoadsWithTheThemesFontsAndTheCodeProjectAroundIt()
    {
        string[] themes = ThemeNames(ThemesRoot);
        themes.Length.ShouldBeGreaterThanOrEqualTo(8);

        // A theme's files do not depend on the platform, so each theme takes the next platform in turn: every theme and
        // every platform is covered without the cost of the cross product (each creation copies the theme's fonts).
        string[] allThemes = themes.Append("Standard").ToArray();
        for (int i = 0; i < allThemes.Length; i++)
        {
            string theme = allThemes[i];
            string name = Platforms.ElementAt(i % Platforms.Count()).Name;
            string folderName = $"{theme}_{name}";
            string label = $"theme={theme} platform={name}";
            NewProjectResult result = await Run(Request(name, folderName, theme: theme, sourceLinked: name == "kniskia"));

            result.Files.Success.ShouldBeTrue(label + " " + result.Files.ErrorMessage);
            string gumFolder = Path.Combine(Game(folderName), "Content", "GumProject");
            File.Exists(Path.Combine(gumFolder, "GumProject.gumj")).ShouldBeTrue(label);
            Gum.DataTypes.GumProjectSave project = Gum.DataTypes.GumProjectSave.Load(Path.Combine(gumFolder, "GumProject.gumj"));
            project.Screens.Count.ShouldBe(1, label);
            project.Components.Count.ShouldBeGreaterThan(10, label);

            if (theme != "Standard")
            {
                string[] expectedFonts = Directory.GetFiles(Path.Combine(ThemesRoot, theme, "FontCache")).Select(Path.GetFileName).Order().ToArray()!;
                string[] actualFonts = Directory.GetFiles(Path.Combine(gumFolder, "FontCache")).Select(Path.GetFileName).Order().ToArray()!;
                actualFonts.ShouldBe(expectedFonts, label);
                Directory.GetFiles(Path.Combine(gumFolder, "Fonts")).Length.ShouldBe(
                    Directory.GetFiles(Path.Combine(ThemesRoot, theme, "Fonts")).Length, label);
            }

            string csproj = File.ReadAllText(Path.Combine(Game(folderName), folderName + ".csproj"));
            csproj.ShouldContain(@"Content\**\*.*", Case.Sensitive, label);
            csproj.ShouldContain("CopyToOutputDirectory", Case.Sensitive, label);
            Directory.Delete(Game(folderName), recursive: true);
        }
    }

    [Fact]
    public async Task Execute_ThemeStandard_WritesTheSameFilesAsNoTheme()
    {
        NewProjectResult plain = await Run(Request("monogame", "Plain"));
        NewProjectResult standard = await Run(Request("monogame", "Std", theme: "Standard"));

        plain.Files.Success.ShouldBeTrue(plain.Files.ErrorMessage);
        standard.Files.Success.ShouldBeTrue(standard.Files.ErrorMessage);
        RelativeFiles(Game("Std")).Select(file => file.Replace("Std", "Plain")).Order().ShouldBe(RelativeFiles(Game("Plain")).Order());
    }

    [Fact]
    public async Task Execute_ThemeNameInAnotherCase_WritesTheThemesFonts()
    {
        NewProjectResult result = await Run(Request("raylib", theme: "bUbBlEgUm"));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string cache = Path.Combine(Game(), "Content", "GumProject", "FontCache");
        Directory.GetFiles(cache).Select(Path.GetFileName).Order()
            .ShouldBe(Directory.GetFiles(Path.Combine(ThemesRoot, "Bubblegum", "FontCache")).Select(Path.GetFileName).Order());
    }
}
