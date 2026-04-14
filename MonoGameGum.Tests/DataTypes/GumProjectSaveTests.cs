using Gum.DataTypes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class GumProjectSaveTests : BaseTestClass
{
    private static GumProjectSave RoundTrip(GumProjectSave project)
    {
        FileManager.XmlSerialize(project, out string xml);
        FileManager.CustomGetStreamFromFile = _ => new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return GumProjectSave.Load("fake.gumx")!;
    }

    private static GumProjectSave LoadFromXml(string xml)
    {
        FileManager.CustomGetStreamFromFile = _ => new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return GumProjectSave.Load("fake.gumx")!;
    }

    [Fact]
    public void LocalizationFile_LegacyShim_GetterReturnsFirstEntry()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");

#pragma warning disable CS0618
        project.LocalizationFile.ShouldBe("a.resx");
#pragma warning restore CS0618
    }

    [Fact]
    public void LocalizationFile_LegacyShim_SetterClearsAndAdds()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");

#pragma warning disable CS0618
        project.LocalizationFile = "replaced.csv";
#pragma warning restore CS0618

        project.LocalizationFiles.Count.ShouldBe(1);
        project.LocalizationFiles[0].ShouldBe("replaced.csv");
    }

    [Fact]
    public void LocalizationFile_LegacyShim_SetterWithEmptyClearsList()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");

#pragma warning disable CS0618
        project.LocalizationFile = "";
#pragma warning restore CS0618

        project.LocalizationFiles.Count.ShouldBe(0);
    }

    [Fact]
    public void LocalizationFiles_BothOldAndNewElementsPresent_NewListWins()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <LocalizationFile>legacy.csv</LocalizationFile>
              <LocalizationFiles>
                <string>a.resx</string>
                <string>b.resx</string>
              </LocalizationFiles>
            </GumProjectSave>
            """;

        GumProjectSave project = LoadFromXml(xml);

        project.LocalizationFiles.ShouldBe(new[] { "a.resx", "b.resx" });
    }

    [Fact]
    public void LocalizationFiles_LegacySingleFileXml_PopulatesListWithOneEntry()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <LocalizationFile>Strings.csv</LocalizationFile>
            </GumProjectSave>
            """;

        GumProjectSave project = LoadFromXml(xml);

        project.LocalizationFiles.Count.ShouldBe(1);
        project.LocalizationFiles[0].ShouldBe("Strings.csv");
    }

    [Fact]
    public void LocalizationFiles_MultiFileXml_PopulatesListWithAllEntries()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <LocalizationFiles>
                <string>Strings.resx</string>
                <string>Buttons.resx</string>
                <string>Errors.resx</string>
              </LocalizationFiles>
            </GumProjectSave>
            """;

        GumProjectSave project = LoadFromXml(xml);

        project.LocalizationFiles.ShouldBe(new[] { "Strings.resx", "Buttons.resx", "Errors.resx" });
    }

    [Fact]
    public void LocalizationFiles_RoundTripEmpty_NeitherElementWritten()
    {
        GumProjectSave project = new GumProjectSave();

        FileManager.XmlSerialize(project, out string xml);

        xml.ShouldNotContain("<LocalizationFile>");
        xml.ShouldNotContain("<LocalizationFiles");

        GumProjectSave loaded = RoundTrip(project);
        loaded.LocalizationFiles.Count.ShouldBe(0);
    }

    [Fact]
    public void LocalizationFiles_RoundTripMultiFile_PreservesAllEntries()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");
        project.LocalizationFiles.Add("c.resx");

        GumProjectSave loaded = RoundTrip(project);

        loaded.LocalizationFiles.ShouldBe(new[] { "a.resx", "b.resx", "c.resx" });
    }

    [Fact]
    public void LocalizationFiles_RoundTripSingleFile_PreservesEntry()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("foo.csv");

        GumProjectSave loaded = RoundTrip(project);

        loaded.LocalizationFiles.Count.ShouldBe(1);
        loaded.LocalizationFiles[0].ShouldBe("foo.csv");
    }

    [Fact]
    public void LocalizationFiles_SerializedMultiFile_EmitsLegacyElementPointingAtFirstPath()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("first.resx");
        project.LocalizationFiles.Add("second.resx");

        FileManager.XmlSerialize(project, out string xml);

        XDocument doc = XDocument.Parse(xml);
        XElement root = doc.Root!;
        XElement? legacy = root.Elements().FirstOrDefault(e => e.Name.LocalName == "LocalizationFile");
        legacy.ShouldNotBeNull();
        legacy!.Value.ShouldBe("first.resx");

        XElement? list = root.Elements().FirstOrDefault(e => e.Name.LocalName == "LocalizationFiles");
        list.ShouldNotBeNull();
        list!.Elements().Select(e => e.Value).ShouldBe(new[] { "first.resx", "second.resx" });
    }

    [Fact]
    public void Load_ShouldUseFileManagerCustomGetStreamFromFile_IfSet()
    {
        bool wasCalled = false;

        GumProjectSave gumProject = new GumProjectSave();
        FileManager.XmlSerialize(gumProject, out string xml);

        FileManager.CustomGetStreamFromFile = (filePath) =>
        {
            wasCalled = true;
            return new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        };
        string fakeFilePath = "fakeFilePath.gumx";
        Gum.DataTypes.GumProjectSave.Load(fakeFilePath);
        wasCalled.ShouldBeTrue();
    }

    [Fact]
    public void Load_V1Format_SetsVersionToInitialVersion()
    {
        // V1 files have no <Version> element; Load must explicitly set Version = 1
        const string v1Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>600</DefaultCanvasHeight>
            </GumProjectSave>
            """;

        FileManager.CustomGetStreamFromFile = filePath =>
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(v1Xml));

        var project = GumProjectSave.Load("fake.gumx");

        project.Version.ShouldBe((int)GumProjectSave.GumxVersions.InitialVersion);
    }

    [Fact]
    public void Load_V2Format_SetsVersionToAttributeVersion()
    {
        // V2 files have a <Version>2</Version> element; Load should read it correctly
        const string v2Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Version>2</Version>
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>600</DefaultCanvasHeight>
            </GumProjectSave>
            """;

        FileManager.CustomGetStreamFromFile = filePath =>
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(v2Xml));

        var project = GumProjectSave.Load("fake.gumx");

        project.Version.ShouldBe((int)GumProjectSave.GumxVersions.AttributeVersion);
    }

    [Fact]
    public void V1GumxFormat_DeserializesScreenReferenceNamesCorrectly()
    {
        const string v1Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <ScreenReference>
                <ElementType>Screen</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
                <Name>MainMenu</Name>
              </ScreenReference>
            </GumProjectSave>
            """;

        var deserializer = FileManager.GetXmlSerializer(typeof(GumProjectSave));
        GumProjectSave project;
        using (var reader = new StringReader(v1Xml))
        {
            project = (GumProjectSave)deserializer.Deserialize(reader)!;
        }

        project.ScreenReferences.Count.ShouldBe(1);
        project.ScreenReferences[0].Name.ShouldBe("MainMenu");
    }

    [Fact]
    public void V2GumxFormat_DeserializesScreenReferenceNamesCorrectly()
    {
        const string v2Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Version>2</Version>
              <ScreenReference Name="MainMenu" />
            </GumProjectSave>
            """;

        var deserializer = GumFileSerializer.GetGumProjectCompactSerializer();
        GumProjectSave project;
        using (var reader = new StringReader(v2Xml))
        {
            project = (GumProjectSave)deserializer.Deserialize(reader)!;
        }

        project.ScreenReferences.Count.ShouldBe(1);
        project.ScreenReferences[0].Name.ShouldBe("MainMenu");
    }
}
