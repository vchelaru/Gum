using Gum.DataTypes;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.DataTypes;

/// <summary>
/// The project-level type and variable restrictions round-trip through the .gumx and add nothing to
/// a project that does not use them (#7720).
/// </summary>
public class ProjectRestrictionsSerializationTests : BaseTestClass
{
    private static string SaveToTempFolder(GumProjectSave project, out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), "gum-restrictions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "Test.gumx");
        project.Save(path, saveElements: false);
        return path;
    }

    [Fact]
    public void Save_ShouldRoundTripRestrictions()
    {
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
        project.SupportedStandardTypes.Add("Sprite");
        project.HideVariablesByDefault = true;
        project.VariableRules.Add(new VariableRule { Pattern = "Sprite.X", Action = VariableRuleAction.Show });
        string path = SaveToTempFolder(project, out string folder);

        try
        {
            GumProjectSave loaded = GumProjectSave.Load(path);

            loaded.SupportedStandardTypes.ShouldBe(new[] { "Sprite" });
            loaded.HideVariablesByDefault.ShouldBeTrue();
            loaded.VariableRules.Count.ShouldBe(1);
            loaded.VariableRules[0].Pattern.ShouldBe("Sprite.X");
            loaded.VariableRules[0].Action.ShouldBe(VariableRuleAction.Show);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Save_ShouldNotWriteRestrictionElements_WhenUnused()
    {
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
        string path = SaveToTempFolder(project, out string folder);

        try
        {
            string xml = File.ReadAllText(path);

            xml.ShouldNotContain("SupportedStandardTypes");
            xml.ShouldNotContain("HideVariablesByDefault");
            xml.ShouldNotContain("VariableRules");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
