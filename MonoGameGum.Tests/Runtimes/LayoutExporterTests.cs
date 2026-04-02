using Gum.DataTypes;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.GueDeriving;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class LayoutExporterTests : BaseTestClass
{
    [Fact]
    public void ExportLayoutJson_ShouldWriteFile()
    {
        ContainerRuntime root = new();
        root.Name = "Root";
        root.Width = 800;
        root.Height = 600;
        root.WidthUnits = DimensionUnitType.Absolute;
        root.HeightUnits = DimensionUnitType.Absolute;

        string filePath = Path.Combine(Path.GetTempPath(), $"layout_test_{Guid.NewGuid()}.json");

        try
        {
            root.ExportLayoutJson(filePath);

            File.Exists(filePath).ShouldBeTrue();

            string json = File.ReadAllText(filePath);
            JsonDocument doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("name").GetString().ShouldBe("Root");
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ToLayoutJson_ShouldIncludeAbsolutePosition()
    {
        ContainerRuntime parent = new();
        parent.Width = 800;
        parent.Height = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Name = "Offset";
        child.X = 100;
        child.Y = 50;
        child.Width = 200;
        child.Height = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        string json = parent.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        JsonElement childElement = doc.RootElement.GetProperty("children")[0];
        childElement.GetProperty("x").GetSingle().ShouldBe(100);
        childElement.GetProperty("y").GetSingle().ShouldBe(50);
        childElement.GetProperty("width").GetSingle().ShouldBe(200);
        childElement.GetProperty("height").GetSingle().ShouldBe(100);
    }

    [Fact]
    public void ToLayoutJson_ShouldIncludeChildren_Recursively()
    {
        ContainerRuntime root = new();
        root.Name = "Root";
        root.Width = 800;
        root.Height = 600;
        root.WidthUnits = DimensionUnitType.Absolute;
        root.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime childA = new();
        childA.Name = "ChildA";
        root.AddChild(childA);

        ContainerRuntime grandchild = new();
        grandchild.Name = "Grandchild";
        childA.AddChild(grandchild);

        string json = root.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        JsonElement childAElement = doc.RootElement.GetProperty("children")[0];
        childAElement.GetProperty("name").GetString().ShouldBe("ChildA");

        JsonElement grandchildElement = childAElement.GetProperty("children")[0];
        grandchildElement.GetProperty("name").GetString().ShouldBe("Grandchild");
    }

    [Fact]
    public void ToLayoutJson_ShouldIncludeInvisibleElements()
    {
        ContainerRuntime parent = new();
        parent.Width = 800;
        parent.Height = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime hiddenChild = new();
        hiddenChild.Name = "Hidden";
        hiddenChild.Visible = false;
        parent.AddChild(hiddenChild);

        string json = parent.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        JsonElement childElement = doc.RootElement.GetProperty("children")[0];
        childElement.GetProperty("name").GetString().ShouldBe("Hidden");
        childElement.GetProperty("visible").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void ToLayoutJson_ShouldIncludeTextContent()
    {
        ContainerRuntime parent = new();
        parent.Width = 800;
        parent.Height = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        TextRuntime textElement = new();
        textElement.Name = "Title";
        textElement.Text = "Hello World";
        parent.AddChild(textElement);

        string json = parent.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        JsonElement childElement = doc.RootElement.GetProperty("children")[0];
        childElement.GetProperty("text").GetString().ShouldBe("Hello World");
    }

    [Fact]
    public void ToLayoutJson_ShouldNotIncludeChildrenArray_WhenNoChildren()
    {
        ContainerRuntime leaf = new();
        leaf.Name = "Leaf";
        leaf.Width = 100;
        leaf.Height = 100;
        leaf.WidthUnits = DimensionUnitType.Absolute;
        leaf.HeightUnits = DimensionUnitType.Absolute;

        string json = leaf.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("children", out _).ShouldBeFalse();
    }

    [Fact]
    public void ToLayoutJson_ShouldOmitName_WhenNull()
    {
        ContainerRuntime element = new();
        element.Width = 100;
        element.Height = 100;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;

        string json = element.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("name", out _).ShouldBeFalse();
    }

    [Fact]
    public void ToLayoutJson_ShouldProduceValidJson()
    {
        ContainerRuntime root = new();
        root.Name = "Root";
        root.Width = 800;
        root.Height = 600;
        root.WidthUnits = DimensionUnitType.Absolute;
        root.HeightUnits = DimensionUnitType.Absolute;

        for (int i = 0; i < 3; i++)
        {
            ContainerRuntime child = new();
            child.Name = $"Child{i}";
            child.Width = 100;
            child.Height = 50;
            child.WidthUnits = DimensionUnitType.Absolute;
            child.HeightUnits = DimensionUnitType.Absolute;
            root.AddChild(child);
        }

        string json = root.ToLayoutJson();

        // Should not throw
        JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("type").GetString().ShouldNotBeNullOrEmpty();
        doc.RootElement.GetProperty("children").GetArrayLength().ShouldBe(3);
    }

    [Fact]
    public void ToLayoutJson_ShouldReportType()
    {
        ContainerRuntime container = new();
        container.Name = "MyContainer";
        container.Width = 100;
        container.Height = 100;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;

        string json = container.ToLayoutJson();
        JsonDocument doc = JsonDocument.Parse(json);

        string type = doc.RootElement.GetProperty("type").GetString()!;
        type.ShouldNotBeNullOrEmpty();
    }
}
