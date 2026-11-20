using System.Text.Json;
using Gum.DataTypes;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class EditProjectPropertyTests : BaseTestClass
{
    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private string WriteOperations(string json)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    private static string Op(string name, string valueJson) =>
        "{ \"op\": \"setProjectProperty\", \"name\": \"" + name + "\", \"value\": " + valueJson + " }";

    private CliTestHelper Edit(string projectPath, params string[] operations) =>
        CliTestHelper.Run("edit", projectPath,
            WriteOperations("{ \"version\": 1, \"operations\": [ " + string.Join(", ", operations) + " ] }"));

    [Fact]
    public void Edit_SetProjectProperty_SetsLocalizationFontAndTextureSettings_AndTheProjectStillPassesCheck()
    {
        string projectPath = CreateEmptyProject();
        string directory = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(Path.Combine(directory, "Loc"));
        File.WriteAllText(Path.Combine(directory, "Loc", "strings.csv"), "String ID,English,Spanish\nT_OK,OK,De acuerdo\n");

        CliTestHelper result = Edit(projectPath,
            Op("LocalizationFiles", "[\"Loc/strings.csv\"]"),
            Op("CurrentLanguageIndex", "1"),
            Op("ShowLocalizationInGum", "false"),
            Op("FontRanges", "\"32-126,160-255,1024-1279\""),
            Op("FontGenerator", "\"KernSmith\""),
            Op("FontSpacingHorizontal", "2"),
            Op("FontSpacingVertical", "3"),
            Op("AutoSizeFontOutputs", "true"),
            Op("SinglePixelTextureFile", "\"Textures/pixel.png\""),
            Op("SinglePixelTextureLeft", "4"),
            Op("SinglePixelTextureTop", "5"),
            Op("SinglePixelTextureRight", "6"),
            Op("SinglePixelTextureBottom", "7"),
            Op("EngineProfile", "\"Profiles/game.gumengine\""),
            Op("RestrictToUnitValues", "true"),
            Op("RestrictFileNamesForAndroid", "true"));

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.LocalizationFiles.ShouldBe(new[] { "Loc/strings.csv" });
        project.CurrentLanguageIndex.ShouldBe(1);
        project.ShowLocalizationInGum.ShouldBeFalse();
        project.FontRanges.ShouldBe("32-126,160-255,1024-1279");
        project.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        project.FontSpacingHorizontal.ShouldBe(2);
        project.FontSpacingVertical.ShouldBe(3);
        project.AutoSizeFontOutputs.ShouldBeTrue();
        project.SinglePixelTextureFile.ShouldBe("Textures/pixel.png");
        project.SinglePixelTextureLeft.ShouldBe(4);
        project.SinglePixelTextureTop.ShouldBe(5);
        project.SinglePixelTextureRight.ShouldBe(6);
        project.SinglePixelTextureBottom.ShouldBe(7);
        project.EngineProfile.ShouldBe("Profiles/game.gumengine");
        project.RestrictToUnitValues.ShouldBeTrue();
        project.RestrictFileNamesForAndroid.ShouldBeTrue();
    }

    [Fact]
    public void Edit_SetProjectProperty_LocalizationOnly_PassesCheck()
    {
        string projectPath = CreateEmptyProject();
        string directory = Path.GetDirectoryName(projectPath)!;
        File.WriteAllText(Path.Combine(directory, "strings.csv"), "String ID,English,Spanish\nT_OK,OK,De acuerdo\n");

        Edit(projectPath, Op("LocalizationFiles", "[\"strings.csv\"]"), Op("FontRanges", "\"32-126\""))
            .ExitCode.ShouldBe(0);

        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void Edit_SetProjectProperty_AnAbsolutePath_IsStoredRelativeToTheProject()
    {
        string projectPath = CreateEmptyProject();
        string directory = Path.GetDirectoryName(projectPath)!;
        string absolute = Path.Combine(directory, "Loc", "strings.csv").Replace('\\', '/');

        CliTestHelper result = Edit(projectPath,
            Op("LocalizationFiles", JsonSerializer.Serialize(new[] { absolute })),
            Op("SinglePixelTextureFile", JsonSerializer.Serialize(Path.Combine(directory, "Textures", "pixel.png").Replace('\\', '/'))));

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.LocalizationFiles.ShouldBe(new[] { "Loc/strings.csv" });
        project.SinglePixelTextureFile.ShouldBe("Textures/pixel.png");
    }

    [Fact]
    public void Edit_SetProjectProperty_EmptyListAndNull_ClearTheSettings()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath,
            Op("LocalizationFiles", "[\"a.csv\"]"),
            Op("SinglePixelTextureFile", "\"p.png\""),
            Op("SinglePixelTextureTop", "2")).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath,
            Op("LocalizationFiles", "[]"),
            Op("SinglePixelTextureFile", "\"\""),
            Op("SinglePixelTextureTop", "null"));

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.LocalizationFiles.ShouldBeEmpty();
        project.SinglePixelTextureFile.ShouldBeEmpty();
        project.SinglePixelTextureTop.ShouldBeNull();
    }

    [Theory]
    [InlineData("FontRanges", "\"abc\"", "abc")]
    [InlineData("FontRanges", "\"200-100\"", "200-100")]
    [InlineData("FontRanges", "5", "5")]
    [InlineData("FontGenerator", "\"Photoshop\"", "\"BmFont\", \"KernSmith\"")]
    [InlineData("FontSpacingHorizontal", "-1", "-1")]
    [InlineData("FontSpacingVertical", "\"two\"", "two")]
    [InlineData("AutoSizeFontOutputs", "\"yes\"", "true or false")]
    [InlineData("RestrictToUnitValues", "1", "true or false")]
    [InlineData("CurrentLanguageIndex", "-1", "-1")]
    [InlineData("LocalizationFiles", "\"strings.csv\"", "array")]
    [InlineData("LocalizationFiles", "[5]", "array")]
    [InlineData("SinglePixelTextureTop", "-3", "-3")]
    [InlineData("SinglePixelTextureFile", "5", "string")]
    [InlineData("EngineProfile", "5", "string")]
    [InlineData("UseFontCharacterFile", "true", "FontRanges")]
    [InlineData("FontRangeEntries", "[]", "Fonts page")]
    public void Edit_SetProjectProperty_AValueTheToolWouldNotAccept_ExitsWith1AndWritesNothing(string name, string valueJson, string expectedInMessage)
    {
        string projectPath = CreateEmptyProject();
        string before = File.ReadAllText(projectPath);

        CliTestHelper result = Edit(projectPath, Op(name, valueJson));

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("operations[0]");
        result.StandardError.ShouldContain(name);
        result.StandardError.ShouldContain(expectedInMessage);
        File.ReadAllText(projectPath).ShouldBe(before);
    }

    [Fact]
    public void EditSchema_Json_ListsEveryProjectPropertyWithItsValueAndAllowedValues()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        JsonElement[] properties = document.RootElement.GetProperty("projectProperties").EnumerateArray().ToArray();
        string[] names = properties.Select(item => item.GetProperty("name").GetString()!).ToArray();
        names.ShouldContain("LocalizationFiles");
        names.ShouldContain("FontRanges");
        names.ShouldContain("FontGenerator");
        names.ShouldContain("SinglePixelTextureFile");
        names.ShouldContain("TextureFilter");
        JsonElement generator = properties.Single(item => item.GetProperty("name").GetString() == "FontGenerator");
        generator.GetProperty("type").GetString().ShouldBe("string");
        generator.GetProperty("allowed").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "BmFont", "KernSmith" });
        properties.Single(item => item.GetProperty("name").GetString() == "LocalizationFiles")
            .GetProperty("type").GetString().ShouldBe("string[]");
    }

    [Fact]
    public void EditSchema_Text_ListsTheProjectProperties()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema");

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldContain("setProjectProperty names:");
        result.StandardOutput.ShouldContain("FontGenerator");
        result.StandardOutput.ShouldContain("KernSmith");
    }

    [Fact]
    public void EditSchema_EveryListedProjectProperty_IsOneSetProjectPropertyAccepts()
    {
        string projectPath = CreateEmptyProject();
        CliTestHelper schema = CliTestHelper.Run("edit", "--schema", "--json");
        using JsonDocument document = JsonDocument.Parse(schema.StandardOutput);

        foreach (JsonElement property in document.RootElement.GetProperty("projectProperties").EnumerateArray())
        {
            string name = property.GetProperty("name").GetString()!;
            // A value of the wrong JSON kind for every property: the refusal must be about the value, not the name.
            CliTestHelper result = Edit(projectPath, Op(name, "{}"));

            result.ExitCode.ShouldBe(1, name);
            result.StandardError.ShouldNotContain("is not a project property", customMessage: name);
        }
    }
}
