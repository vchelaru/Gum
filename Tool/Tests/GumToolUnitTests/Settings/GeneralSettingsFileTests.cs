using System.IO;
using System.Xml.Serialization;
using Gum.Settings;
using Shouldly;

namespace GumToolUnitTests.Settings;

public class GeneralSettingsFileTests
{
    [Theory]
    [InlineData(null, true)]   // Never chosen -> the Standards palette defaults on.
    [InlineData(false, false)] // Explicit opt-out is honored.
    [InlineData(true, true)]   // Explicit opt-in is honored.
    public void EffectiveUseStandardsPalette_ResolvesUnsetToOn(bool? stored, bool expected)
    {
        GeneralSettingsFile settings = new GeneralSettingsFile
        {
            UseStandardsPalette = stored
        };

        settings.EffectiveUseStandardsPalette.ShouldBe(expected);
    }

    [Fact]
    public void UseStandardsPalette_DefaultsToNull_SoNewUsersGetPaletteOn()
    {
        GeneralSettingsFile settings = new GeneralSettingsFile();

        settings.UseStandardsPalette.HasValue.ShouldBeFalse();
        settings.EffectiveUseStandardsPalette.ShouldBeTrue();
    }

    [Theory]
    // Setting absent from the file (fresh installs / users who never toggled it) resolves to on.
    [InlineData("<GeneralSettingsFile></GeneralSettingsFile>", true)]
    // A value explicitly written to the file is honored, so an opt-out survives the flip.
    [InlineData("<GeneralSettingsFile><UseStandardsPalette>false</UseStandardsPalette></GeneralSettingsFile>", false)]
    [InlineData("<GeneralSettingsFile><UseStandardsPalette>true</UseStandardsPalette></GeneralSettingsFile>", true)]
    public void EffectiveUseStandardsPalette_ResolvesFromDeserializedXml(string xml, bool expected)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(GeneralSettingsFile));
        using StringReader reader = new StringReader(xml);
        GeneralSettingsFile settings = (GeneralSettingsFile)serializer.Deserialize(reader)!;

        settings.EffectiveUseStandardsPalette.ShouldBe(expected);
    }
}
