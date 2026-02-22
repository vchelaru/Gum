using Gum.DataTypes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class GumProjectSaveTests : BaseTestClass
{
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
            project = (GumProjectSave)deserializer.Deserialize(reader);
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

        var deserializer = VariableSaveSerializer.GetGumProjectCompactSerializer();
        GumProjectSave project;
        using (var reader = new StringReader(v2Xml))
        {
            project = (GumProjectSave)deserializer.Deserialize(reader);
        }

        project.ScreenReferences.Count.ShouldBe(1);
        project.ScreenReferences[0].Name.ShouldBe("MainMenu");
    }
}
