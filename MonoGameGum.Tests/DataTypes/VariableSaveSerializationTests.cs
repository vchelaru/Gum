using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Shouldly;
using System.IO;
using System.Xml.Serialization;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class VariableSaveSerializationTests
{
    [Fact]
    public void DeserializeBehaviorSave_CompactFormat_LoadsVariables()
    {
        BehaviorSave original = new BehaviorSave();
        original.RequiredVariables.Variables.Add(new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true });

        XmlSerializer compactSerializer = VariableSaveSerializer.GetCompactSerializer(typeof(BehaviorSave));
        string xml = SerializeToString(compactSerializer, original);

        BehaviorSave? result = VariableSaveSerializer.DeserializeBehaviorSave(xml, projectVersion: 2);

        result.ShouldNotBeNull();
        result.RequiredVariables.Variables.Count.ShouldBe(1);
        result.RequiredVariables.Variables[0].Type.ShouldBe("float");
        result.RequiredVariables.Variables[0].Name.ShouldBe("X");
        result.RequiredVariables.Variables[0].Value.ShouldBe(51f);
    }

    [Fact]
    public void DeserializeBehaviorSave_LegacyFormat_LoadsVariables()
    {
        BehaviorSave original = new BehaviorSave();
        original.RequiredVariables.Variables.Add(new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true });

        FileManager.XmlSerialize(original, out string xml);

        BehaviorSave? result = VariableSaveSerializer.DeserializeBehaviorSave(xml, projectVersion: 2);

        result.ShouldNotBeNull();
        result.RequiredVariables.Variables.Count.ShouldBe(1);
        result.RequiredVariables.Variables[0].Type.ShouldBe("float");
        result.RequiredVariables.Variables[0].Name.ShouldBe("X");
        result.RequiredVariables.Variables[0].Value.ShouldBe(51f);
    }

    [Fact]
    public void DeserializeElementSave_CompactFormat_LoadsVariables()
    {
        ScreenSave original = new ScreenSave();
        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true });
        original.States.Add(state);

        XmlSerializer compactSerializer = VariableSaveSerializer.GetCompactSerializer(typeof(ScreenSave));
        string xml = SerializeToString(compactSerializer, original);

        ScreenSave? result = VariableSaveSerializer.DeserializeElementSave<ScreenSave>(xml, projectVersion: 2);

        result.ShouldNotBeNull();
        result.DefaultState.ShouldNotBeNull();
        result.DefaultState.Variables.Count.ShouldBe(1);
        result.DefaultState.Variables[0].Type.ShouldBe("float");
        result.DefaultState.Variables[0].Name.ShouldBe("X");
        result.DefaultState.Variables[0].Value.ShouldBe(51f);
    }

    [Fact]
    public void DeserializeElementSave_LegacyFormatInV1Project_LoadsVariables()
    {
        ScreenSave original = new ScreenSave();
        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true });
        original.States.Add(state);

        FileManager.XmlSerialize(original, out string xml);

        ScreenSave? result = VariableSaveSerializer.DeserializeElementSave<ScreenSave>(xml, projectVersion: 1);

        result.ShouldNotBeNull();
        result.DefaultState.ShouldNotBeNull();
        result.DefaultState.Variables.Count.ShouldBe(1);
        result.DefaultState.Variables[0].Type.ShouldBe("float");
        result.DefaultState.Variables[0].Name.ShouldBe("X");
        result.DefaultState.Variables[0].Value.ShouldBe(51f);
    }

    [Fact]
    public void DeserializeElementSave_LegacyFormatInV2Project_LoadsVariables()
    {
        ScreenSave original = new ScreenSave();
        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true });
        original.States.Add(state);

        FileManager.XmlSerialize(original, out string xml);

        ScreenSave? result = VariableSaveSerializer.DeserializeElementSave<ScreenSave>(xml, projectVersion: 2);

        result.ShouldNotBeNull();
        result.DefaultState.ShouldNotBeNull();
        result.DefaultState.Variables.Count.ShouldBe(1);
        result.DefaultState.Variables[0].Type.ShouldBe("float");
        result.DefaultState.Variables[0].Name.ShouldBe("X");
        result.DefaultState.Variables[0].Value.ShouldBe(51f);
    }

    [Fact]
    public void DeserializeElementSave_MixedFormat_LoadsInstances()
    {
        const string mixedXml = """
            <ScreenSave>
              <Instance>
                <Name>Background</Name>
                <BaseType>Sprite</BaseType>
              </Instance>
              <State>
                <Variable Type="float" Name="X" SetsValue="true">
                  <Value xsi:type="xsd:float" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">51</Value>
                </Variable>
              </State>
            </ScreenSave>
            """;

        ScreenSave? result = VariableSaveSerializer.DeserializeElementSave<ScreenSave>(mixedXml, projectVersion: 2);

        result.ShouldNotBeNull();
        result.Instances.Count.ShouldBe(1);
        result.Instances[0].Name.ShouldBe("Background");
        result.Instances[0].BaseType.ShouldBe("Sprite");
    }

    [Fact]
    public void DeserializeElementSave_NoVariables_ReturnsEmptyElement()
    {
        const string xml = "<ScreenSave />";

        ScreenSave? result = VariableSaveSerializer.DeserializeElementSave<ScreenSave>(xml, projectVersion: 2);

        result.ShouldNotBeNull();
        result.States.ShouldBeEmpty();
        result.Instances.ShouldBeEmpty();
    }

    [Fact]
    public void CompactFormat_DefinedByBase_OmittedWhenFalse()
    {
        var screen = new ScreenSave();
        screen.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Sprite", DefinedByBase = false });

        var serializer = VariableSaveSerializer.GetCompactSerializer(typeof(ScreenSave));
        string xml = SerializeToString(serializer, screen);

        xml.ShouldNotContain("DefinedByBase");
    }

    [Fact]
    public void CompactFormat_InstanceSave_ProducesAttributesInXml()
    {
        var screen = new ScreenSave();
        screen.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Sprite" });

        var serializer = VariableSaveSerializer.GetCompactSerializer(typeof(ScreenSave));
        string xml = SerializeToString(serializer, screen);

        xml.ShouldContain("Name=\"Background\"");
        xml.ShouldContain("BaseType=\"Sprite\"");
        xml.ShouldNotContain("<Name>Background</Name>");
    }

    [Fact]
    public void CompactFormat_ProducesAttributesInXml()
    {
        var state = new StateSave
        {
            Name = "Default",
            Variables =
            {
                new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true }
            }
        };

        var serializer = VariableSaveSerializer.GetCompactSerializer(typeof(StateSave));
        string xml = SerializeToString(serializer, state);

        xml.ShouldContain("Type=\"float\"");
        xml.ShouldNotContain("<Type>float</Type>");
        xml.ShouldContain("Name=\"X\"");
        xml.ShouldNotContain("<Name>X</Name>");
    }

    [Fact]
    public void CompactFormat_RoundTrip_PreservesAllProperties()
    {
        var original = new StateSave
        {
            Name = "Default",
            Variables =
            {
                new VariableSave
                {
                    Type = "float",
                    Name = "X",
                    Value = 51f,
                    SetsValue = true,
                    Category = "Position"
                }
            }
        };

        var serializer = VariableSaveSerializer.GetCompactSerializer(typeof(StateSave));
        string xml = SerializeToString(serializer, original);

        StateSave result;
        using (var reader = new StringReader(xml))
        {
            result = (StateSave)serializer.Deserialize(reader);
        }

        result.Name.ShouldBe("Default");
        result.Variables.Count.ShouldBe(1);
        result.Variables[0].Type.ShouldBe("float");
        result.Variables[0].Name.ShouldBe("X");
        result.Variables[0].Value.ShouldBe(51f);
        result.Variables[0].SetsValue.ShouldBeTrue();
        result.Variables[0].Category.ShouldBe("Position");
    }

    [Fact]
    public void CompactFormat_ValueStaysAsChildElement()
    {
        var state = new StateSave
        {
            Name = "Default",
            Variables =
            {
                new VariableSave { Type = "float", Name = "X", Value = 51f }
            }
        };

        var serializer = VariableSaveSerializer.GetCompactSerializer(typeof(StateSave));
        string xml = SerializeToString(serializer, state);

        xml.ShouldContain("<Value");
        xml.ShouldNotContain(" Value=\"");
    }

    [Fact]
    public void FullV2Format_CompactInstances_LoadsInstanceNamesCorrectly()
    {
        const string v2Xml = """
            <ScreenSave>
              <Instance Name="Background" BaseType="Sprite" />
            </ScreenSave>
            """;

        var serializer = VariableSaveSerializer.GetCompactSerializer(typeof(ScreenSave));
        ScreenSave screen;
        using (var reader = new StringReader(v2Xml))
        {
            screen = (ScreenSave)serializer.Deserialize(reader);
        }

        screen.Instances.Count.ShouldBe(1);
        screen.Instances[0].Name.ShouldBe("Background");
        screen.Instances[0].BaseType.ShouldBe("Sprite");
    }

    [Fact]
    public void LegacyFormat_RoundTrip_StillWorks()
    {
        var original = new StateSave
        {
            Name = "Default",
            Variables =
            {
                new VariableSave { Type = "float", Name = "X", Value = 51f, SetsValue = true }
            }
        };

        FileManager.XmlSerialize(original, out string xml);

        var standardSerializer = FileManager.GetXmlSerializer(typeof(StateSave));
        StateSave result;
        using (var reader = new StringReader(xml))
        {
            result = (StateSave)standardSerializer.Deserialize(reader);
        }

        result.Name.ShouldBe("Default");
        result.Variables.Count.ShouldBe(1);
        result.Variables[0].Type.ShouldBe("float");
        result.Variables[0].Name.ShouldBe("X");
        result.Variables[0].Value.ShouldBe(51f);
    }

    [Fact]
    public void MixedFormat_LegacyInstances_LoadsInstanceNamesCorrectly()
    {
        const string mixedXml = """
            <ScreenSave>
              <Instance>
                <Name>Background</Name>
                <BaseType>Sprite</BaseType>
              </Instance>
            </ScreenSave>
            """;

        var serializer = VariableSaveSerializer.GetLegacyInstancesCompactSerializer(typeof(ScreenSave));
        ScreenSave screen;
        using (var reader = new StringReader(mixedXml))
        {
            screen = (ScreenSave)serializer.Deserialize(reader);
        }

        screen.Instances.Count.ShouldBe(1);
        screen.Instances[0].Name.ShouldBe("Background");
        screen.Instances[0].BaseType.ShouldBe("Sprite");
    }

    [Fact]
    public void NewProjectDefaultsToAttributeVersion()
    {
        var project = new GumProjectSave();
        project.Version.ShouldBe((int)GumProjectSave.GumxVersions.AttributeVersion);
    }

    [Fact]
    public void UpgradePath_V2Project_LoadsV1ElementFiles()
    {
        const string v1Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>TestScreen</Name>
              <State>
                <Name>Default</Name>
                <Variable>
                  <Type>float</Type>
                  <Name>X</Name>
                  <Value xsi:type="xsd:float">51</Value>
                </Variable>
              </State>
            </ScreenSave>
            """;

        string projectRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + Path.DirectorySeparatorChar;
        string screensDir = projectRoot + "Screens";
        Directory.CreateDirectory(screensDir);
        string filePath = Path.Combine(screensDir, "TestScreen.gusx");
        File.WriteAllText(filePath, v1Xml);

        try
        {
            var reference = new ElementReference { Name = "TestScreen", ElementType = ElementType.Screen };
            var loadResult = new GumLoadResult();
            ScreenSave screen = reference.ToElementSave<ScreenSave>(
                projectRoot, GumProjectSave.ScreenExtension, loadResult, projectVersion: 2);

            screen.ShouldNotBeNull();
            screen.Name.ShouldBe("TestScreen");
            screen.DefaultState.ShouldNotBeNull();
            screen.DefaultState.Variables.Count.ShouldBe(1);
            screen.DefaultState.Variables[0].Type.ShouldBe("float");
            screen.DefaultState.Variables[0].Name.ShouldBe("X");
            screen.DefaultState.Variables[0].Value.ShouldBe(51f);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    private static string SerializeToString(XmlSerializer serializer, object obj)
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        using var writer = new StringWriter();
        serializer.Serialize(writer, obj, namespaces);
        return writer.ToString();
    }
}
