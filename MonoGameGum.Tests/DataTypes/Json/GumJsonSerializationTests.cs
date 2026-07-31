using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;
using Gum.DataTypes.Variables;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Xunit;

namespace MonoGameGum.Tests.DataTypes.Json;

public class GumJsonSerializationTests
{
    [Fact]
    public void BehaviorReference_GetRelativeFilePath_JsonFormatWithSourcePath_ReturnsSourcePathVerbatim()
    {
        BehaviorReference reference = new BehaviorReference { Name = "ButtonBehavior", SourcePath = "../../FormsBehaviors/ButtonBehavior.behx" };

        string path = reference.GetRelativeFilePath(isJsonFormat: true);

        path.ShouldBe("../../FormsBehaviors/ButtonBehavior.behx");
    }

    [Fact]
    public void BehaviorReference_GetRelativeFilePath_JsonFormatWithoutSourcePath_UsesJsonExtension()
    {
        BehaviorReference reference = new BehaviorReference { Name = "ButtonBehavior" };

        string path = reference.GetRelativeFilePath(isJsonFormat: true);

        path.ShouldBe("Behaviors/ButtonBehavior.behj");
    }

    [Fact]
    public void SaveThenLoad_JsonProject_RoundTripsScreenComponentAndStandardElement()
    {
        // Exercises the real production entry points a consumer would use - GumProjectSave.Save
        // (which cascades to ElementSave.Save per element) and GumProjectSave.Load - rather than
        // calling GumJsonFileSerializer directly, proving the .gumj/.gusj/.gucj/.gutj extension
        // dispatch is wired all the way through.
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
            ScreenSave screen = new ScreenSave { Name = "MainMenu" };
            ComponentSave component = new ComponentSave { Name = "Button" };
            StandardElementSave standard = new StandardElementSave { Name = "Container" };
            project.Screens.Add(screen);
            project.Components.Add(component);
            project.StandardElements.Add(standard);
            project.ScreenReferences.Add(new ElementReference { Name = "MainMenu", ElementType = ElementType.Screen });
            project.ComponentReferences.Add(new ElementReference { Name = "Button", ElementType = ElementType.Component });
            project.StandardElementReferences.Add(new ElementReference { Name = "Container", ElementType = ElementType.Standard });

            string projectPath = Path.Combine(tempDir, "Project.gumj");
            project.Save(projectPath, saveElements: true);

            GumProjectSave? loaded = GumProjectSave.Load(projectPath, out GumLoadResult result);

            result.ErrorMessage.ShouldBeNullOrEmpty();
            result.MissingFiles.ShouldBeEmpty();
            loaded.ShouldNotBeNull();
            loaded!.Screens.Single().Name.ShouldBe("MainMenu");
            loaded.Components.Single().Name.ShouldBe("Button");
            loaded.StandardElements.Single().Name.ShouldBe("Container");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void SaveThenLoad_JsonBehavior_RoundTrips()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(tempDir, "Behaviors"));
        try
        {
            GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion };
            project.BehaviorReferences.Add(new BehaviorReference { Name = "ButtonBehavior" });
            string projectPath = Path.Combine(tempDir, "Project.gumj");
            File.WriteAllText(projectPath, GumJsonFileSerializer.SerializeProject(project));

            BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/Button" };
            behavior.Save(Path.Combine(tempDir, "Behaviors", "ButtonBehavior.behj"));

            GumProjectSave? loaded = GumProjectSave.Load(projectPath, out GumLoadResult result);

            result.ErrorMessage.ShouldBeNullOrEmpty();
            result.MissingFiles.ShouldBeEmpty();
            loaded.ShouldNotBeNull();
            loaded!.Behaviors.Single().Name.ShouldBe("ButtonBehavior");
            loaded.Behaviors.Single().DefaultImplementation.ShouldBe("Controls/Button");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void SerializeDeserialize_BehaviorSave_PreservesFormsPropertiesAndRequiredInstances()
    {
        BehaviorSave original = new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/Button" };
        original.FormsProperties.Add(new VariableSave { Name = "ToolTip", Type = "string", Value = "Click me" });
        original.RequiredInstances.Add(new BehaviorInstanceSave { Name = "TextInstance", BaseType = "Text" });
        original.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        string json = GumJsonFileSerializer.SerializeBehavior(original);
        BehaviorSave result = GumJsonFileSerializer.DeserializeBehavior(json);

        result.Name.ShouldBe("ButtonBehavior");
        result.DefaultImplementation.ShouldBe("Controls/Button");
        result.FormsProperties.Count.ShouldBe(1);
        result.FormsProperties[0].Name.ShouldBe("ToolTip");
        result.FormsProperties[0].Value.ShouldBe("Click me");
        result.RequiredInstances.Count.ShouldBe(1);
        result.RequiredInstances[0].Name.ShouldBe("TextInstance");
        result.RequiredInstances[0].BaseType.ShouldBe("Text");
        result.ToolOnlyVariableReferences.ShouldBe(new[] { "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"" });
    }

    [Fact]
    public void SerializeDeserialize_ElementSave_PreservesStatesInstancesCategoriesAndEvents()
    {
        ScreenSave original = new ScreenSave { Name = "MainMenu" };
        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 10f, SetsValue = true });
        original.States.Add(state);
        original.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Sprite" });
        original.Events.Add(new EventSave { Name = "Background.Click", Enabled = true, ExposedAsName = "BackgroundClicked" });
        StateSaveCategory category = new StateSaveCategory { Name = "VisibilityCategory" };
        category.States.Add(new StateSave { Name = "Visible" });
        original.Categories.Add(category);

        string json = GumJsonFileSerializer.SerializeElement(original);
        ScreenSave result = GumJsonFileSerializer.DeserializeElement<ScreenSave>(json);

        result.Name.ShouldBe("MainMenu");
        result.States.Count.ShouldBe(1);
        result.States[0].Variables[0].Value.ShouldBe(10f);
        result.Instances.Count.ShouldBe(1);
        result.Instances[0].Name.ShouldBe("Background");
        result.Events.Count.ShouldBe(1);
        result.Events[0].ExposedAsName.ShouldBe("BackgroundClicked");
        result.Categories.Count.ShouldBe(1);
        result.Categories[0].States[0].Name.ShouldBe("Visible");
    }

    [Fact]
    public void SerializeDeserialize_GumProjectSave_PreservesScalarAndReferenceFields()
    {
        GumProjectSave original = new GumProjectSave
        {
            FontRanges = "32-126",
            DefaultCanvasWidth = 1920,
            DefaultCanvasHeight = 1080,
            Version = GumProjectSave.NativeVersion,
        };
        original.Guides.Add(new GuideRectangle { Name = "Guide1", X = 10, Y = 20, Width = 100, Height = 50 });
        original.ScreenReferences.Add(new ElementReference { Name = "MainMenu", ElementType = ElementType.Screen });
        original.LocalizationFiles.Add("Strings.resx");

        string json = GumJsonFileSerializer.SerializeProject(original);
        GumProjectSave result = GumJsonFileSerializer.DeserializeProject(json);

        result.FontRanges.ShouldBe("32-126");
        result.DefaultCanvasWidth.ShouldBe(1920);
        result.Version.ShouldBe(GumProjectSave.NativeVersion);
        result.Guides.Count.ShouldBe(1);
        result.Guides[0].Name.ShouldBe("Guide1");
        result.ScreenReferences.Count.ShouldBe(1);
        result.ScreenReferences[0].Name.ShouldBe("MainMenu");
        result.LocalizationFiles.ShouldBe(new[] { "Strings.resx" });
    }

    [Fact]
    public void SerializeDeserialize_VariableSave_PreservesEachBoxedValueType()
    {
        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "FloatVar", Type = "float", Value = 1.5f });
        state.Variables.Add(new VariableSave { Name = "IntVar", Type = "int", Value = 42 });
        state.Variables.Add(new VariableSave { Name = "BoolVar", Type = "bool", Value = true });
        state.Variables.Add(new VariableSave { Name = "StringVar", Type = "string", Value = "hello" });
        state.Variables.Add(new VariableSave { Name = "NullVar", Type = "string", Value = null, SetsValue = false });
        StandardElementSave element = new StandardElementSave { Name = "Container" };
        element.States.Add(state);

        string json = GumJsonFileSerializer.SerializeElement(element);
        StandardElementSave result = GumJsonFileSerializer.DeserializeElement<StandardElementSave>(json);

        List<VariableSave> resultVariables = result.States[0].Variables;
        resultVariables.First(v => v.Name == "FloatVar").Value.ShouldBe(1.5f);
        resultVariables.First(v => v.Name == "IntVar").Value.ShouldBe(42);
        resultVariables.First(v => v.Name == "BoolVar").Value.ShouldBe(true);
        resultVariables.First(v => v.Name == "StringVar").Value.ShouldBe("hello");
        resultVariables.First(v => v.Name == "NullVar").Value.ShouldBeNull();
    }

    [Fact]
    public void SerializeDeserialize_VariableListSave_PreservesVector2List()
    {
        StateSave state = new StateSave { Name = "Default" };
        VariableListSave<Vector2> pointsList = new VariableListSave<Vector2> { Name = "Points", Type = "Vector2" };
        pointsList.Value.Add(new Vector2(1, 2));
        pointsList.Value.Add(new Vector2(3, 4));
        state.VariableLists.Add(pointsList);
        StandardElementSave element = new StandardElementSave { Name = "Polygon" };
        element.States.Add(state);

        string json = GumJsonFileSerializer.SerializeElement(element);
        StandardElementSave result = GumJsonFileSerializer.DeserializeElement<StandardElementSave>(json);

        VariableListSave<Vector2> resultList = (VariableListSave<Vector2>)result.States[0].VariableLists[0];
        resultList.Name.ShouldBe("Points");
        resultList.Value.ShouldBe(new[] { new Vector2(1, 2), new Vector2(3, 4) });
    }
}
