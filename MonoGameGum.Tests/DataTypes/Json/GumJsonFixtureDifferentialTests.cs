using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Xunit;

namespace MonoGameGum.Tests.DataTypes.Json;

/// <summary>
/// Differential check against every real <c>.gumx</c> file already checked into this repo: loads each
/// via the existing XML path, then proves the JSON serializer round-trips its content losslessly.
/// This is a backstop, not the primary proof - <see cref="GumJsonSerializationTests"/> covers the same
/// shapes with hand-constructed, in-memory data. If this ever catches something those tests miss, the
/// fix is a new targeted case in <see cref="GumJsonSerializationTests"/>, not more reliance on this sweep.
/// </summary>
/// <remarks>
/// Two independent checks per object, not one: (1) JSON self-consistency (serialize -> deserialize ->
/// serialize again, same string) catches non-determinism in the JSON mapping itself, but would NOT
/// catch a field the original <c>ToJson</c> mapper silently drops or mismaps, since both sides of that
/// comparison went through the same (buggy) mapper. (2) Re-serializing the JSON-round-tripped object
/// back through the already-proven XML compact serializer and diffing against the original XML-loaded
/// object's own XML serialization gives an independent oracle that a dropped/mismapped field would
/// actually fail.
/// </remarks>
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
        SerializeProjectToXml(reloadedProject).ShouldBe(SerializeProjectToXml(original));

        foreach (ScreenSave screen in original.Screens)
        {
            string json = GumJsonFileSerializer.SerializeElement(screen);
            ScreenSave reloaded = GumJsonFileSerializer.DeserializeElement<ScreenSave>(json);
            GumJsonFileSerializer.SerializeElement(reloaded).ShouldBe(json);
            SerializeElementToXml(reloaded).ShouldBe(SerializeElementToXml(screen));
        }

        foreach (ComponentSave component in original.Components)
        {
            string json = GumJsonFileSerializer.SerializeElement(component);
            ComponentSave reloaded = GumJsonFileSerializer.DeserializeElement<ComponentSave>(json);
            GumJsonFileSerializer.SerializeElement(reloaded).ShouldBe(json);
            SerializeElementToXml(reloaded).ShouldBe(SerializeElementToXml(component));
        }

        foreach (StandardElementSave standard in original.StandardElements)
        {
            string json = GumJsonFileSerializer.SerializeElement(standard);
            StandardElementSave reloaded = GumJsonFileSerializer.DeserializeElement<StandardElementSave>(json);
            GumJsonFileSerializer.SerializeElement(reloaded).ShouldBe(json);
            SerializeElementToXml(reloaded).ShouldBe(SerializeElementToXml(standard));
        }

        foreach (BehaviorSave behavior in original.Behaviors)
        {
            string json = GumJsonFileSerializer.SerializeBehavior(behavior);
            BehaviorSave reloaded = GumJsonFileSerializer.DeserializeBehavior(json);
            GumJsonFileSerializer.SerializeBehavior(reloaded).ShouldBe(json);
            SerializeBehaviorToXml(reloaded).ShouldBe(SerializeBehaviorToXml(behavior));
        }
    }

    /// <summary>
    /// Loads each real, standalone <c>.behx</c> file under the FormsBehaviors template folder - not
    /// referenced by any of the 23 project fixtures above - and round-trips it through JSON, using the
    /// same independent-XML-oracle approach as <see cref="JsonRoundTrip_RealGumxFixture_IsLossless"/>.
    /// These are the only real, checked-in files anywhere in the repo with <c>double</c>-typed
    /// <c>FormsProperty</c> values (13 instances across the three files; every other real project uses
    /// only int/float/bool/string, already covered by the 23-fixture sweep above).
    /// </summary>
    [Theory]
    [InlineData("Tools/Gum.ProjectServices/Templates/FormsBehaviors/ScrollBarBehavior.behx")]
    [InlineData("Tools/Gum.ProjectServices/Templates/FormsBehaviors/ScrollViewerBehavior.behx")]
    [InlineData("Tools/Gum.ProjectServices/Templates/FormsBehaviors/SliderBehavior.behx")]
    public void JsonRoundTrip_RealFormsBehaviorFixture_IsLossless(string relativePath)
    {
        string behxPath = Path.Combine(LocateRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(behxPath).ShouldBeTrue($"Fixture .behx missing at {behxPath}");

        BehaviorSave original = BehaviorReference.DeserializeBehavior(behxPath, projectVersion: GumProjectSave.NativeVersion);

        string json = GumJsonFileSerializer.SerializeBehavior(original);
        BehaviorSave reloaded = GumJsonFileSerializer.DeserializeBehavior(json);

        GumJsonFileSerializer.SerializeBehavior(reloaded).ShouldBe(json);
        SerializeBehaviorToXml(reloaded).ShouldBe(SerializeBehaviorToXml(original));
    }

    private static string SerializeProjectToXml(GumProjectSave project)
    {
        XmlSerializer serializer = GumFileSerializer.GetGumProjectCompactSerializer();
        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, project);
        return writer.ToString();
    }

    private static string SerializeElementToXml<T>(T element) where T : ElementSave
    {
        XmlSerializer serializer = GumFileSerializer.GetCompactSerializer(typeof(T));
        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, element);
        return writer.ToString();
    }

    private static string SerializeBehaviorToXml(BehaviorSave behavior)
    {
        XmlSerializer serializer = GumFileSerializer.GetCompactSerializer(typeof(BehaviorSave));
        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, behavior);
        return writer.ToString();
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
