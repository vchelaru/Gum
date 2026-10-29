using Gum.DataTypes;
using Gum.DataTypes.Serialization.Json;
using Gum.DataTypes.Variables;
using Shouldly;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// <see cref="VariableSave.ShaderParameter"/> marks a variable as the saved value of a shader parameter
/// (#7010). It has to survive every copy and save path a variable goes through, and a variable without
/// it must save exactly the bytes it saved before the property existed.
/// </summary>
public class ShaderParameterMarkerSerializationTests
{
    private static ComponentSave CreateComponent(string? marker)
    {
        ComponentSave component = new ComponentSave { Name = "Panel" };
        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave
        {
            Name = "Icon.Outline_Thickness",
            Type = "float",
            Value = 3f,
            SetsValue = true,
            ShaderParameter = marker,
        });
        component.States.Add(state);
        return component;
    }

    private static string SerializeCompact(ComponentSave component)
    {
        XmlSerializer serializer = GumFileSerializer.GetCompactSerializer(typeof(ComponentSave));
        XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
        namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, component, namespaces);
        return writer.ToString();
    }

    [Fact]
    public void CompactXml_Marker_IsWrittenAsAnAttributeAndLoadsBack()
    {
        string xml = SerializeCompact(CreateComponent("OutlineColor#1/4"));

        xml.ShouldContain("ShaderParameter=\"OutlineColor#1/4\"");

        ComponentSave? loaded = GumFileSerializer.DeserializeElementSave<ComponentSave>(
            xml, (int)GumProjectSave.GumxVersions.AttributeVersion);
        loaded.ShouldNotBeNull();
        loaded.States[0].Variables[0].ShaderParameter.ShouldBe("OutlineColor#1/4");
    }

    [Fact]
    public void CompactXml_NoMarker_WritesNoShaderParameterAtAll()
    {
        string xml = SerializeCompact(CreateComponent(marker: null));

        xml.ShouldNotContain("ShaderParameter");

        ComponentSave? loaded = GumFileSerializer.DeserializeElementSave<ComponentSave>(
            xml, (int)GumProjectSave.GumxVersions.AttributeVersion);
        loaded.ShouldNotBeNull();
        loaded.States[0].Variables[0].ShaderParameter.ShouldBeNull();
    }

    [Fact]
    public void CompactXml_EmptyMarker_IsTreatedAsAbsent()
    {
        string xml = SerializeCompact(CreateComponent(marker: ""));

        xml.ShouldNotContain("ShaderParameter");
    }

    [Fact]
    public void CompactXml_VariableWithoutMarker_HasTheSameBytesAsBefore()
    {
        // Pins the exact attribute list of an ordinary variable, so adding the marker cannot reorder
        // or add anything for the projects that never use it.
        string xml = SerializeCompact(CreateComponent(marker: null));

        xml.ShouldContain("<Variable Type=\"float\" Name=\"Icon.Outline_Thickness\" SetsValue=\"true\">");
        xml.ShouldNotContain("ShaderParameter");
    }

    [Fact]
    public void LegacyXml_Marker_RoundTrips()
    {
        FileManager.XmlSerialize(CreateComponent("Thickness"), out string xml);

        ComponentSave? loaded = GumFileSerializer.DeserializeElementSave<ComponentSave>(xml, projectVersion: 2);

        loaded.ShouldNotBeNull();
        loaded.States[0].Variables[0].ShaderParameter.ShouldBe("Thickness");
    }

    [Fact]
    public void LegacyXml_NoMarker_WritesNoShaderParameterAtAll()
    {
        FileManager.XmlSerialize(CreateComponent(marker: null), out string xml);

        xml.ShouldNotContain("ShaderParameter");
    }

    [Fact]
    public void Json_Marker_RoundTrips()
    {
        string json = GumJsonFileSerializer.SerializeElement(CreateComponent("OutlineColor#3/4"));

        json.ShouldContain("\"ShaderParameter\": \"OutlineColor#3/4\"");

        ComponentSave loaded = GumJsonFileSerializer.DeserializeElement<ComponentSave>(json);
        loaded.States[0].Variables[0].ShaderParameter.ShouldBe("OutlineColor#3/4");
    }

    [Fact]
    public void Json_NoMarker_WritesNoShaderParameterAtAll()
    {
        string json = GumJsonFileSerializer.SerializeElement(CreateComponent(marker: null));

        json.ShouldNotContain("ShaderParameter");

        ComponentSave loaded = GumJsonFileSerializer.DeserializeElement<ComponentSave>(json);
        loaded.States[0].Variables[0].ShaderParameter.ShouldBeNull();
    }

    [Fact]
    public void Json_MarkerOnAnUnsetPlaceholder_IsKept()
    {
        // An exposed parameter nothing has set is saved as SetsValue = false with no value.
        ComponentSave component = CreateComponent("Thickness");
        VariableSave placeholder = component.States[0].Variables[0];
        placeholder.Value = null;
        placeholder.SetsValue = false;
        placeholder.ExposedAsName = "OutlineThickness";

        ComponentSave loaded = GumJsonFileSerializer.DeserializeElement<ComponentSave>(
            GumJsonFileSerializer.SerializeElement(component));

        VariableSave result = loaded.States[0].Variables[0];
        result.ShaderParameter.ShouldBe("Thickness");
        result.ExposedAsName.ShouldBe("OutlineThickness");
        result.SetsValue.ShouldBeFalse();
    }

    [Fact]
    public void VariableSaveClone_KeepsMarker()
    {
        VariableSave variable = new VariableSave { Name = "Icon.Outline_Tint__X", Type = "float", ShaderParameter = "Tint#0/4" };

        variable.Clone().ShaderParameter.ShouldBe("Tint#0/4");
    }

    [Fact]
    public void StateSaveClone_KeepsMarker()
    {
        // The undo snapshot, the cross-element undo record and copy/paste all clone state variables.
        ComponentSave component = CreateComponent("Thickness");

        StateSave clone = component.States[0].Clone();

        clone.Variables.Single().ShaderParameter.ShouldBe("Thickness");
    }

    [Fact]
    public void SaveObjectClone_OfAnElement_KeepsMarker()
    {
        // ElementUndoStrategy snapshots an element with FileManager.CloneSaveObject, which round trips
        // through the default XML serializer.
        ComponentSave component = CreateComponent("OutlineColor#2/4");

        ComponentSave clone = FileManager.CloneSaveObject(component);

        clone.States[0].Variables[0].ShaderParameter.ShouldBe("OutlineColor#2/4");
    }

    [Fact]
    public void SaveObjectClone_OfAVariable_KeepsMarker()
    {
        VariableSave variable = new VariableSave { Name = "Icon.Outline_Thickness", Type = "float", ShaderParameter = "Thickness" };

        FileManager.CloneSaveObject(variable).ShaderParameter.ShouldBe("Thickness");
    }

    [Fact]
    public void AreSaveObjectsEqual_ElementsDifferingOnlyInTheMarker_AreNotEqual()
    {
        // Undo decides whether an element changed by comparing serialized copies, so a lost or added
        // marker must register as a difference.
        ComponentSave withMarker = CreateComponent("Thickness");
        ComponentSave withoutMarker = CreateComponent(marker: null);

        FileManager.AreSaveObjectsEqual(
            FileManager.CloneSaveObject(withMarker),
            FileManager.CloneSaveObject(withoutMarker)).ShouldBeFalse();
    }
}
