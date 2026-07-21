using System.IO;
using System.Xml.Serialization;
using Gum.Settings;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins that an older <c>GeneralSettings.xml</c> written while <see cref="GeneralSettingsFile"/>
/// .MainWindowState was still <c>System.Windows.Forms.FormWindowState</c> continues to deserialize
/// correctly now that the property is typed as the neutral <see cref="LegacyMainWindowState"/>
/// instead -- <see cref="XmlSerializer"/> round-trips an enum by its member name, so this only holds
/// because the two enums' member names match exactly.
/// </summary>
public class GeneralSettingsFileLegacyWindowStateTests
{
    [Theory]
    [InlineData("Normal", LegacyMainWindowState.Normal)]
    [InlineData("Minimized", LegacyMainWindowState.Minimized)]
    [InlineData("Maximized", LegacyMainWindowState.Maximized)]
    public void MainWindowState_DeserializesFromLegacyXmlValue(string xmlValue, LegacyMainWindowState expected)
    {
        string xml = $"<GeneralSettingsFile><MainWindowState>{xmlValue}</MainWindowState></GeneralSettingsFile>";
        XmlSerializer serializer = new XmlSerializer(typeof(GeneralSettingsFile));
        using StringReader reader = new StringReader(xml);

        GeneralSettingsFile settings = (GeneralSettingsFile)serializer.Deserialize(reader)!;

        settings.MainWindowState.ShouldBe(expected);
    }

    [Fact]
    public void MainWindowState_DefaultsToNormal_WhenAbsentFromXml()
    {
        string xml = "<GeneralSettingsFile></GeneralSettingsFile>";
        XmlSerializer serializer = new XmlSerializer(typeof(GeneralSettingsFile));
        using StringReader reader = new StringReader(xml);

        GeneralSettingsFile settings = (GeneralSettingsFile)serializer.Deserialize(reader)!;

        settings.MainWindowState.ShouldBe(LegacyMainWindowState.Normal);
    }
}
