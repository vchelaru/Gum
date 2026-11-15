using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #7010: a shader parameter variable (<c>Outline_Thickness</c>, <c>Outline_Tint__X</c>) can be the left side of
/// a variable reference, and the runtime applies the followed value to the container's shader parameter.
/// </summary>
public class ShaderParameterVariableReferenceRuntimeTests : BaseTestClass
{
    private const string OutlineFile = "Shaders/Outline.fx";

    public ShaderParameterVariableReferenceRuntimeTests()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ContainerRuntime CreateShadedChild(ContainerRuntime parent, string name)
    {
        ContainerRuntime child = new ContainerRuntime();
        child.Name = name;
        child.Tag = new InstanceSave { Name = name };
        child.Parent = parent;
        child.SetProperty("SourceShaderFile", OutlineFile);
        return child;
    }

    private static StateSave BuildState(float pixelSize, params (string owner, string line)[] references)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "PixelSize", Value = pixelSize, Type = "float", SetsValue = true });

        foreach ((string owner, string line) in references)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = owner + ".VariableReferences" };
            list.Value.Add(line);
            state.VariableLists.Add(list);
        }
        return state;
    }

    private static ShaderParameterValue Get(ContainerRuntime container, string name)
    {
        ShaderParameterCollection? parameters = ((IRenderTargetRenderable)container.RenderableComponent!).ShaderParameters;
        parameters.ShouldNotBeNull();
        parameters!.TryGetValue(name, out ShaderParameterValue value).ShouldBeTrue("expected parameter " + name);
        return value;
    }

    [Fact]
    public void ApplyVariableReferences_ScalarShaderParameterOnTheLeft_SetsTheContainersShaderParameter()
    {
        ContainerRuntime parent = new ContainerRuntime();
        ContainerRuntime holder = CreateShadedChild(parent, "Holder");
        StateSave state = BuildState(6f, ("Holder", "Outline_Thickness = PixelSize"));

        parent.ApplyVariableReferences(state);

        Get(holder, "Thickness").X.ShouldBe(6f);
    }

    [Fact]
    public void ApplyVariableReferences_OneVariableDrivesAnOrdinaryVariableAndAShaderParameter()
    {
        // The #7010 example: one component variable drives both an ordinary variable of one instance and the
        // shader parameter of a container.
        ContainerRuntime parent = new ContainerRuntime();
        ContainerRuntime holder = CreateShadedChild(parent, "Holder");
        ContainerRuntime sprite = new ContainerRuntime();
        sprite.Name = "Sprite";
        sprite.Tag = new InstanceSave { Name = "Sprite" };
        sprite.Parent = parent;
        StateSave state = BuildState(
            4f,
            ("Holder", "Outline_Thickness = PixelSize"),
            ("Sprite", "Width = PixelSize"));

        parent.ApplyVariableReferences(state);

        sprite.Width.ShouldBe(4f);
        Get(holder, "Thickness").X.ShouldBe(4f);
    }

    [Fact]
    public void ApplyVariableReferences_VectorComponentsOnTheLeft_AssembleOneVector()
    {
        // The arithmetic on the second line needs the expression service.
        GumExpressionService.Initialize();
        ContainerRuntime parent = new ContainerRuntime();
        ContainerRuntime holder = CreateShadedChild(parent, "Holder");
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Holder.VariableReferences" };
        list.Value.Add("Outline_Tint__X = PixelSize");
        list.Value.Add("Outline_Tint__Y = PixelSize * 2");
        StateSave state = BuildState(0.25f);
        state.VariableLists.Add(list);

        parent.ApplyVariableReferences(state);

        ShaderParameterValue tint = Get(holder, "Tint");
        tint.X.ShouldBe(0.25f);
        tint.Y.ShouldBe(0.5f);
    }

    [Fact]
    public void ApplyVariableReferences_ShaderParameterOnTheLeft_FollowsTheSourceWhenItChangesAndReapplies()
    {
        ContainerRuntime parent = new ContainerRuntime();
        ContainerRuntime holder = CreateShadedChild(parent, "Holder");
        StateSave state = BuildState(2f, ("Holder", "Outline_Thickness = PixelSize"));
        parent.ApplyVariableReferences(state);

        state.SetValue("PixelSize", 9f, null, "float");
        parent.ApplyVariableReferences(state);

        Get(holder, "Thickness").X.ShouldBe(9f);
    }
}
