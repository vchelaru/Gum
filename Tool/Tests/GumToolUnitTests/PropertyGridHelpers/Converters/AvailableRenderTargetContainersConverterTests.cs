using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System.Linq;

namespace GumToolUnitTests.PropertyGridHelpers.Converters;

public class AvailableRenderTargetContainersConverterTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedStateMock;
    private AvailableRenderTargetContainersConverter _converter = null!;

    public AvailableRenderTargetContainersConverterTests()
    {
        _selectedStateMock = new Mock<ISelectedState>();
    }

    private void CreateConverter()
    {
        _converter = new AvailableRenderTargetContainersConverter(_selectedStateMock.Object);
    }

    private static InstanceSave AddInstance(ElementSave element, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType };
        element.Instances.Add(instance);
        return instance;
    }

    [Fact]
    public void GetStandardValues_ExcludesNonRenderTargetContainers()
    {
        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.ParentContainer = element;

        AddInstance(element, "RenderTargetContainer", "Container");
        AddInstance(element, "NormalContainer", "Container");
        // Only the first container is flagged as a render target.
        element.DefaultState.SetValue("RenderTargetContainer.IsRenderTarget", true);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(element);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.Cast<string>().ShouldBe(new[] { "<NONE>", "RenderTargetContainer" });
    }

    [Fact]
    public void GetStandardValues_ExcludesSelectedInstance()
    {
        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.ParentContainer = element;

        InstanceSave spriteInstance = AddInstance(element, "SpriteInstance", "Sprite");
        AddInstance(element, "RenderTargetContainer", "Container");
        element.DefaultState.SetValue("RenderTargetContainer.IsRenderTarget", true);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(element);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns(spriteInstance);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.Cast<string>().ShouldBe(new[] { "<NONE>", "RenderTargetContainer" });
    }

    [Fact]
    public void GetStandardValues_ReturnsNoneOnly_WhenNoElementSelected()
    {
        _selectedStateMock.Setup(x => x.SelectedElement).Returns((ElementSave?)null);
        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.Count.ShouldBe(1);
        result[0].ShouldBe("<NONE>");
    }
}
