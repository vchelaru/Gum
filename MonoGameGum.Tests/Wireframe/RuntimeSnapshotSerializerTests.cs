using System.Linq;
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
