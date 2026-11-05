using Gum.DataTypes;
using Gum.DataTypes.Serialization.Json;
using Gum.DataTypes.Variables;
using Shouldly;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The optional category color (#7591) in saved files: it round-trips in the XML (compact and not) and JSON
/// formats, is not written when unset, an unknown name from a newer tool survives a save, and a file
/// written before the field existed loads uncolored.
/// </summary>
public class CategoryColorSaveTests : BaseTestClass
{
    private static ComponentSave CreateComponent(string? colorName)
    {
        ComponentSave component = new ComponentSave { Name = "Button", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        StateSaveCategory category = new StateSaveCategory { Name = "Size", ColorName = colorName };
        category.States.Add(new StateSave { Name = "Small", ParentContainer = component });
        component.Categories.Add(category);
        return component;
    }

    private static string SaveToText(ComponentSave component, bool compact)
    {
        string folder = Path.Combine(Path.GetTempPath(), "gum-7591-" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "Button." + GumProjectSave.ComponentExtension);
            component.Save(path, useCompactFormat: compact);
            return File.ReadAllText(path);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static ComponentSave LoadFromText(string text) =>
        GumFileSerializer.DeserializeElementSave<ComponentSave>(text, (int)GumProjectSave.GumxVersions.AttributeVersion)!;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Xml_WithAColor_RoundTripsIt(bool compact)
    {
        string text = SaveToText(CreateComponent("Indigo"), compact);

        LoadFromText(text).Categories.Single().ColorName.ShouldBe("Indigo");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Xml_WithoutAColor_DoesNotWriteTheField_AndLoadsUncolored(bool compact)
    {
        string text = SaveToText(CreateComponent(null), compact);

        text.ShouldNotContain("ColorName");
        LoadFromText(text).Categories.Single().ColorName.ShouldBeNull();
    }

    [Fact]
    public void Xml_AnEmptyColor_IsNotWritten()
    {
        SaveToText(CreateComponent(""), compact: true).ShouldNotContain("ColorName");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Xml_AnUnknownColorName_IsKeptThroughALoadAndSave(bool compact)
    {
        string written = SaveToText(CreateComponent("FromANewerTool"), compact);

        ComponentSave loaded = LoadFromText(written);
        string resaved = SaveToText(loaded, compact);

        loaded.Categories.Single().ColorName.ShouldBe("FromANewerTool");
        LoadFromText(resaved).Categories.Single().ColorName.ShouldBe("FromANewerTool");
    }

    [Fact]
    public void Xml_AFileWrittenBeforeTheFieldExisted_LoadsWithItsCategoryAndNoColor()
    {
        string oldFile =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<ComponentSave xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
            "  <Name>Button</Name>\n  <BaseType>Container</BaseType>\n" +
            "  <Category>\n    <Name>Size</Name>\n    <State>\n      <Name>Small</Name>\n    </State>\n  </Category>\n" +
            "</ComponentSave>";

        ComponentSave loaded = LoadFromText(oldFile);

        loaded.Categories.Single().Name.ShouldBe("Size");
        loaded.Categories.Single().States.Single().Name.ShouldBe("Small");
        loaded.Categories.Single().ColorName.ShouldBeNull();
    }

    [Fact]
    public void Json_WithAColor_RoundTripsIt()
    {
        string json = GumJsonFileSerializer.SerializeElement(CreateComponent("Teal"));

        GumJsonFileSerializer.DeserializeElement<ComponentSave>(json).Categories.Single().ColorName.ShouldBe("Teal");
    }

    [Fact]
    public void Json_WithoutAColor_DoesNotWriteTheField_AndLoadsUncolored()
    {
        string json = GumJsonFileSerializer.SerializeElement(CreateComponent(null));

        json.ShouldNotContain("ColorName");
        GumJsonFileSerializer.DeserializeElement<ComponentSave>(json).Categories.Single().ColorName.ShouldBeNull();
    }

    [Fact]
    public void Json_AnUnknownColorName_IsKeptThroughALoadAndSave()
    {
        string json = GumJsonFileSerializer.SerializeElement(CreateComponent("FromANewerTool"));

        string resaved = GumJsonFileSerializer.SerializeElement(GumJsonFileSerializer.DeserializeElement<ComponentSave>(json));

        GumJsonFileSerializer.DeserializeElement<ComponentSave>(resaved).Categories.Single().ColorName.ShouldBe("FromANewerTool");
    }

    [Fact]
    public void Clone_KeepsTheColor_AndAnUncoloredCategoryClonesUncolored()
    {
        CreateComponent("Rose").Categories.Single().Clone().ColorName.ShouldBe("Rose");
        CreateComponent(null).Categories.Single().Clone().ColorName.ShouldBeNull();
    }
}
