using Gum.DataTypes;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.DataTypes;

/// <summary>
/// The project's engine profile reference round-trips through .gumx and .gumj and adds nothing to
/// a project that does not use it (#7720).
/// </summary>
public class ProjectRestrictionsSerializationTests : BaseTestClass
{
    private static string SaveToTempFolder(GumProjectSave project, string fileName, out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), "gum-restrictions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, fileName);
        project.Save(path, saveElements: false);
        return path;
    }

    [Theory]
    [InlineData("Test.gumx")]
    [InlineData("Test.gumj")]
    public void Save_ShouldRoundTripEngineProfile(string fileName)
    {
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
        project.EngineProfile = "../../engine/MonoGameSprites.gumengine";
        string path = SaveToTempFolder(project, fileName, out string folder);

        try
        {
            GumProjectSave loaded = GumProjectSave.Load(path);

            loaded.EngineProfile.ShouldBe("../../engine/MonoGameSprites.gumengine");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("Test.gumx")]
    [InlineData("Test.gumj")]
    public void Save_ShouldNotWriteEngineProfile_WhenEmpty(string fileName)
    {
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
        string path = SaveToTempFolder(project, fileName, out string folder);

        try
        {
            string text = File.ReadAllText(path);

            text.ShouldNotContain("EngineProfile");
            GumProjectSave.Load(path).EngineProfile.ShouldBe(string.Empty);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
