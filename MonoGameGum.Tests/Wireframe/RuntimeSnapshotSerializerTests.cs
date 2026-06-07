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
    public void CreateStateForNode_Shaken_ShouldKeepValueDifferentFromDefault()
    {
        // The standard Container default has Visible = true; a live element that differs must be kept.
        ContainerRuntime container = new();
        container.Visible = false;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(container, "Default", shake: true);

        state.Variables.ShouldContain(v => v.Name == "Visible");
    }

    [Fact]
    public void CreateStateForNode_Shaken_ShouldOmitValueEqualToDefault()
    {
        // Visible = true matches the standard Container default, so the shake prunes it.
        ContainerRuntime container = new();
        container.Visible = true;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(container, "Default", shake: true);

        state.Variables.ShouldNotContain(v => v.Name == "Visible");
    }

    [Fact]
    public void CreateStateForNode_Unshaken_ShouldKeepValueEqualToDefault()
    {
        // Without shaking, even a default-valued variable is emitted (the always-correct, heavy mode).
        ContainerRuntime container = new();
        container.Visible = true;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(container, "Default");

        state.Variables.ShouldContain(v => v.Name == "Visible");
    }

    [Fact]
    public void CreateScreenSave_Shaken_ShouldOmitDefaultValuedQualifiedVariables()
    {
        ContainerRuntime root = new();
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";    // differs from the standard Text default ("Hello") -> kept
        label.Visible = true; // matches the standard default -> pruned
        root.AddChild(label);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        StateSave defaultState = screen.States.First(s => s.Name == "Default");
        defaultState.Variables.ShouldContain(v => v.Name == "Label.Text");
        defaultState.Variables.ShouldNotContain(v => v.Name == "Label.Visible");
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
    public void GetReferencedFiles_ShouldReturnDistinctSourceFilePaths()
    {
        ContainerRuntime root = new();
        SpriteRuntime sprite = new() { Name = "Sprite" };
        Microsoft.Xna.Framework.Graphics.Texture2D texture =
            (Microsoft.Xna.Framework.Graphics.Texture2D)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(Microsoft.Xna.Framework.Graphics.Texture2D));
        texture.Name = "UI/button.png";
        sprite.Texture = texture;
        root.AddChild(sprite);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        serializer.GetReferencedFiles(screen).ShouldBe(new[] { "UI/button.png" });
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
