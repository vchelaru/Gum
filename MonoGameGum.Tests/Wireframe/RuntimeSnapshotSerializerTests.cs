using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

public class RuntimeSnapshotSerializerTests : BaseTestClass
{
    private static RuntimeSnapshotSerializer CreateSerializer() =>
        new RuntimeSnapshotSerializer(StandardElementsManager.Self.DefaultStates);

    [Fact]
    public void CreateStateForNode_ShouldReadColorComponents()
    {
        TextRuntime text = new();
        text.Red = 200;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(text, "Default");

        VariableSave? redVariable = state.Variables.FirstOrDefault(v => v.Name == "Red");
        redVariable.ShouldNotBeNull();
        redVariable.Value.ShouldBe(200);
    }

    [Fact]
    public void CreateStateForNode_ShouldReadTextValue()
    {
        TextRuntime text = new();
        text.Text = "Hello";

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(text, "Default");

        VariableSave? textVariable = state.Variables.FirstOrDefault(v => v.Name == "Text");
        textVariable.ShouldNotBeNull();
        textVariable.Value.ShouldBe("Hello");
    }

    [Fact]
    public void CreateScreenSave_ShouldFlattenTreeIntoInstancesWithBaseTypes()
    {
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        root.AddChild(panel);
        panel.AddChild(label);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Name.ShouldBe("Snapshot");
        screen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);
        screen.Instances.First(i => i.Name == "Panel").BaseType.ShouldBe("Container");
        screen.Instances.First(i => i.Name == "Label").BaseType.ShouldBe("Text");
    }

    [Fact]
    public void CreateScreenSave_ShouldQualifyVariablesAndLinkParents()
    {
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";
        root.AddChild(panel);
        panel.AddChild(label);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        StateSave defaultState = screen.States.First(s => s.Name == "Default");

        VariableSave? labelText = defaultState.Variables.FirstOrDefault(v => v.Name == "Label.Text");
        labelText.ShouldNotBeNull();
        labelText.Value.ShouldBe("Hi");

        VariableSave? labelParent = defaultState.Variables.FirstOrDefault(v => v.Name == "Label.Parent");
        labelParent.ShouldNotBeNull();
        labelParent.Value.ShouldBe("Panel");

        // Top-level instances (direct children of the root/screen) have no Parent variable.
        defaultState.Variables.Any(v => v.Name == "Panel.Parent").ShouldBeFalse();
    }

    [Fact]
    public void GetStandardTypeName_ShouldResolveContainerRuntime()
    {
        RuntimeSnapshotSerializer serializer = CreateSerializer();

        serializer.GetStandardTypeName(new ContainerRuntime()).ShouldBe("Container");
    }

    [Fact]
    public void GetStandardTypeName_ShouldResolveTextRuntime()
    {
        RuntimeSnapshotSerializer serializer = CreateSerializer();

        serializer.GetStandardTypeName(new TextRuntime()).ShouldBe("Text");
    }

    [Fact]
    public void GetStandardTypeName_ShouldReturnNullForBareGraphicalUiElement()
    {
        RuntimeSnapshotSerializer serializer = CreateSerializer();

        serializer.GetStandardTypeName(new GraphicalUiElement()).ShouldBeNull();
    }
}
