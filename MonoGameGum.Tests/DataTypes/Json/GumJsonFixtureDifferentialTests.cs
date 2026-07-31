using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.DataTypes.Json;

/// <summary>
/// Differential check against every real <c>.gumx</c> file already checked into this repo: loads each
/// via the existing XML path, then proves the JSON serializer round-trips its content losslessly
/// (serialize -> deserialize -> serialize again, comparing the two JSON strings). This is a backstop,
/// not the primary proof - <see cref="GumJsonSerializationTests"/> covers the same shapes with
/// hand-constructed, in-memory data. If this ever catches something those tests miss, the fix is a
/// new targeted case in <see cref="GumJsonSerializationTests"/>, not more reliance on this sweep.
/// </summary>
public class GumJsonFixtureDifferentialTests
{
    public static IEnumerable<object[]> FixtureGumxPaths()
    {
        string[] relativePaths =
        {
            "Samples/FnaGum/FnaSample/Content/GumProject/GumProject.gumx",
            "Samples/GameUiSamples/Content/GumProject/GameUiSamplesGumProject.gumx",
            "Samples/GumFormsSample/MonoGameGumFormsSample/Content/FormsGumProject/GumProject.gumx",
            "Samples/GumFromZipFile/Content/GumProject/FromZipFileGumProject.gumx",
            "Samples/KniGumFromFile/KniGumFromFileContent/GumProject.gumx",
            "Samples/MVVM/Content/GumProject/GumProject.gumx",
            "Samples/MauiSkiaGum/GumProject/MuaiSkiaGumProject.gumx",
            "Samples/MonoGameGumCodeGeneration/Content/GumProject/GumProject.gumx",
            "Samples/MonoGameGumFromFile/MonoGameGumFromFile/Content/GumProject.gumx",
            "Samples/MonoGameGumFromFile/MonoGameGumFromFileAndroid/Content/GumProject.gumx",
            "Samples/MonoGameGumFromFile/MonoGameGumFromFileDX/Content/GumProject.gumx",
            "Samples/SilkNetGum/SilkNetGumSample/Content/GumProject/GumProject.gumx",
            "Samples/SokolGumFromFile/Content/GumProject/GumProject.gumx",
            "Tests/CodeGen_Maui_FullCodegen/Content/GumProject/CodeGenTestProject.gumx",
            "Tests/CodeGen_MonoGameForms_ByReference/Content/GumProject/CodeGenTestProject.gumx",
            "Tests/CodeGen_MonoGameForms_FullCodegen/Content/CodeGenProject.gumx",
            "Tests/CodeGen_MonoGameForms_Localization_ByReference/Content/GumProject/LocalizationCodeGenTestProject.gumx",
            "Tests/CodeGen_MonoGame_ByReference/Content/GumProject/CodeGenTestProject.gumx",
            "Tests/CodeGen_Raylib_ByReference/Content/GumProject/CodeGenTestProject.gumx",
            "Tests/CodeGen_Skia_ByReference/Content/GumProject/CodeGenTestProject.gumx",
            "Tools/Gum.ProjectServices/Templates/Default/CliTemplate.gumx",
            "Tools/Gum.ProjectServices/Templates/FormsTemplate/GumProject.gumx",
            "Tools/Gum.ProjectServices/Templates/FormsThemes/Bubblegum/GumProject.gumx",
        };

        string repoRoot = LocateRepoRoot();
        foreach (string relativePath in relativePaths)
        {
            yield return new object[] { Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)) };
        }
    }

    [Theory]
    [MemberData(nameof(FixtureGumxPaths))]
    public void JsonRoundTrip_RealGumxFixture_IsLossless(string gumxPath)
    {
        File.Exists(gumxPath).ShouldBeTrue($"Fixture .gumx missing at {gumxPath}");

        GumProjectSave original = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult)!;
        loadResult.ErrorMessage.ShouldBeNullOrEmpty();
        loadResult.MissingFiles.ShouldBeEmpty();

        string projectJson = GumJsonFileSerializer.SerializeProject(original);
        GumProjectSave reloadedProject = GumJsonFileSerializer.DeserializeProject(projectJson);
        GumJsonFileSerializer.SerializeProject(reloadedProject).ShouldBe(projectJson);

        foreach (ScreenSave screen in original.Screens)
        {
            string json = GumJsonFileSerializer.SerializeElement(screen);
            ScreenSave reloaded = GumJsonFileSerializer.DeserializeElement<ScreenSave>(json);
            GumJsonFileSerializer.SerializeElement(reloaded).ShouldBe(json);
        }

        foreach (ComponentSave component in original.Components)
        {
            string json = GumJsonFileSerializer.SerializeElement(component);
            ComponentSave reloaded = GumJsonFileSerializer.DeserializeElement<ComponentSave>(json);
            GumJsonFileSerializer.SerializeElement(reloaded).ShouldBe(json);
        }

        foreach (StandardElementSave standard in original.StandardElements)
        {
            string json = GumJsonFileSerializer.SerializeElement(standard);
            StandardElementSave reloaded = GumJsonFileSerializer.DeserializeElement<StandardElementSave>(json);
            GumJsonFileSerializer.SerializeElement(reloaded).ShouldBe(json);
        }

        foreach (BehaviorSave behavior in original.Behaviors)
        {
            string json = GumJsonFileSerializer.SerializeBehavior(behavior);
            BehaviorSave reloaded = GumJsonFileSerializer.DeserializeBehavior(json);
            GumJsonFileSerializer.SerializeBehavior(reloaded).ShouldBe(json);
        }
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }
}
