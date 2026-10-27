using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using RenderingLibrary.Graphics;
using Shouldly;
using ToolsUtilities;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

public class PropertyGridManagerShaderParametersTests : BaseTestClass, IDisposable
{
    private readonly AutoMocker _mocker = new();
    private readonly ComponentSave _component;
    private readonly InstanceSave _holder;

    public PropertyGridManagerShaderParametersTests()
    {
        _component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        StateSave defaultState = new StateSave { ParentContainer = _component };
        _component.States.Add(defaultState);
        _holder = new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = _component };
        _component.Instances.Add(_holder);
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(_component);
        ObjectFinder.Self.GumProjectSave = project;

        // A stand-in for the platform effect the editor compiled; the reader tells what it declares.
        ShaderParameterReader.Reader = _ => new List<ShaderParameterInfo>
        {
            new ShaderParameterInfo("Thickness", ShaderParameterKind.Float, 1, ShaderParameterValue.FromFloat(0f)),
            new ShaderParameterInfo("OutlineColor", ShaderParameterKind.Float, 4, ShaderParameterValue.FromVector4(0f, 0f, 0f, 1f)),
        };
    }

    public new void Dispose()
    {
        ShaderParameterReader.Reader = null;
    }

    private void GivenTheHolderHasAnEffect(bool hasEffect)
    {
        InvisibleRenderable renderable = new InvisibleRenderable { RenderTargetEffect = hasEffect ? new object() : null };
        GraphicalUiElement representation = new GraphicalUiElement(renderable);
        _mocker.GetMock<IWireframeObjectManager>()
            .Setup(manager => manager.GetRepresentation(_holder, It.IsAny<List<ElementWithState>?>()))
            .Returns(representation);
    }

    private StateReferencingInstanceMember CreateTextMember() =>
        new StateReferencingInstanceMember(
            Array.Empty<Attribute>(), null, typeof(string),
            isReadOnly: false, isAssignedByReference: false, isVariable: true,
            _component.DefaultState, null, "Holder.ShaderParameters", _holder, _component,
            _mocker.Get<ISelectedState>(),
            _mocker.Get<IUndoManager>(),
            _mocker.Get<IGuiCommands>(),
            _mocker.Get<IFileCommands>(),
            _mocker.Get<ISetVariableLogic>(),
            _mocker.Get<IWireframeObjectManager>(),
            _mocker.Get<IPluginManager>(),
            _mocker.Get<IHotkeyManager>(),
            _mocker.Get<IDeleteVariableService>(),
            _mocker.Get<IExposeVariableService>(),
            _mocker.Get<IEditVariableService>(),
            _mocker.Get<ITypeManager>(),
            _mocker.Get<IClipboardService>());

    private MemberCategory BuildRenderingCategory()
    {
        MemberCategory category = new MemberCategory("Rendering");
        category.Members.Add(new InstanceMember("Before", _component.DefaultState));
        category.Members.Add(CreateTextMember());
        category.Members.Add(new InstanceMember("After", _component.DefaultState));
        return category;
    }

    [Fact]
    public void AdjustShaderParameters_ReplacesTheTextRowWithOneRowPerParameterInPlace()
    {
        GivenTheHolderHasAnEffect(true);
        MemberCategory category = BuildRenderingCategory();
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();

        sut.AdjustShaderParameters(new List<MemberCategory> { category }, _holder);

        category.Members.Select(member => member.Name).ToArray().ShouldBe(new[]
        {
            "Before", "Thickness", "OutlineColor X", "OutlineColor Y", "OutlineColor Z", "OutlineColor W", "After",
        });
    }

    [Fact]
    public void AdjustShaderParameters_ShowsNoRowsWhenTheContainerHasNoCompiledShader()
    {
        GivenTheHolderHasAnEffect(false);
        MemberCategory category = BuildRenderingCategory();
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();

        sut.AdjustShaderParameters(new List<MemberCategory> { category }, _holder);

        category.Members.Select(member => member.Name).ToArray().ShouldBe(new[] { "Before", "After" });
    }

    [Fact]
    public void EditingARow_StoresTheParameterInTheSavedText()
    {
        GivenTheHolderHasAnEffect(true);
        _mocker.GetMock<ISetVariableLogic>().SetReturnsDefault(GeneralResponse.SuccessfulResponse);
        _mocker.GetMock<ISelectedState>().Setup(state => state.SelectedStateSave).Returns(_component.DefaultState);
        MemberCategory category = BuildRenderingCategory();
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();
        sut.AdjustShaderParameters(new List<MemberCategory> { category }, _holder);

        category.Members.Single(member => member.Name == "Thickness").Value = 3f;

        _component.DefaultState.GetValueRecursive("Holder.ShaderParameters").ShouldBe("Thickness=3.0");
    }
}
