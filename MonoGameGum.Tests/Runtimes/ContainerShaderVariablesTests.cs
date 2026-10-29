using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #7010 slice 3: a saved variable named like <c>Outline_Thickness</c> reaches the container's shader
/// parameter, whatever order the variable, <c>SourceShaderFile</c> and the legacy <c>ShaderParameters</c>
/// text arrive in, and beats the text for the same parameter.
/// </summary>
public class ContainerShaderVariablesTests : BaseTestClass
{
    private const string OutlineFile = "Shaders/Outline.fx";

    public ContainerShaderVariablesTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static ShaderParameterCollection? GetParameters(ContainerRuntime container) =>
        ((IRenderTargetRenderable)container.RenderableComponent).ShaderParameters;

    private static ShaderParameterValue Get(ContainerRuntime container, string name)
    {
        ShaderParameterCollection? parameters = GetParameters(container);
        parameters.ShouldNotBeNull();
        parameters!.TryGetValue(name, out ShaderParameterValue value).ShouldBeTrue("expected parameter " + name);
        return value;
    }

    private static bool Has(ContainerRuntime container, string name) =>
        GetParameters(container) != null && GetParameters(container)!.TryGetValue(name, out _);

    public static IEnumerable<object[]> Orders()
    {
        string[][] orders =
        {
            new[] { "file", "variable", "text" },
            new[] { "file", "text", "variable" },
            new[] { "variable", "file", "text" },
            new[] { "variable", "text", "file" },
            new[] { "text", "file", "variable" },
            new[] { "text", "variable", "file" },
        };
        foreach (string[] order in orders)
        {
            yield return new object[] { string.Join(",", order) };
        }
    }

    private static void Apply(ContainerRuntime sut, string step)
    {
        switch (step)
        {
            case "file":
                sut.SetProperty("SourceShaderFile", OutlineFile);
                break;
            case "variable":
                sut.SetProperty("Outline_Thickness", 7f);
                break;
            case "text":
                sut.SetProperty("ShaderParameters", "Thickness=2.0; Glow=true");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(Orders))]
    public void SetProperty_VariableTextAndFile_AnyOrderGivesTheSameParameters(string order)
    {
        ContainerRuntime sut = new();

        foreach (string step in order.Split(','))
        {
            Apply(sut, step);
        }

        GetParameters(sut)!.Count.ShouldBe(2);
        Get(sut, "Thickness").Kind.ShouldBe(ShaderParameterKind.Float);
        Get(sut, "Thickness").X.ShouldBe(7f, "the variable beats the text for the same parameter");
        Get(sut, "Glow").Kind.ShouldBe(ShaderParameterKind.Bool);
        Get(sut, "Glow").X.ShouldBe(1f, "a parameter only the text names still applies");
    }

    [Fact]
    public void SetProperty_VariableAfterFile_SetsScalarFloat()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Outline_Thickness", 3f);

        Get(sut, "Thickness").Kind.ShouldBe(ShaderParameterKind.Float);
        Get(sut, "Thickness").X.ShouldBe(3f);
    }

    [Fact]
    public void SetProperty_IntAndBoolVariables_KeepTheirKind()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Outline_Steps", 4);
        sut.SetProperty("Outline_Glow", true);

        Get(sut, "Steps").Kind.ShouldBe(ShaderParameterKind.Int);
        Get(sut, "Steps").X.ShouldBe(4f);
        Get(sut, "Glow").Kind.ShouldBe(ShaderParameterKind.Bool);
        Get(sut, "Glow").X.ShouldBe(1f);
    }

    [Fact]
    public void SetProperty_TextOnly_StillApplies()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("ShaderParameters", "Thickness=2.0");

        Get(sut, "Thickness").X.ShouldBe(2f);
    }

    [Fact]
    public void SetProperty_VariableOfAnotherShadersStem_IsInert()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Glow_Thickness", 9f);

        Has(sut, "Thickness").ShouldBeFalse();
        Has(sut, "Glow_Thickness").ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_VariableArrivesBeforeAnyShaderFile_AppliesOnceTheFileIsSet()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("Outline_Thickness", 5f);
        Has(sut, "Thickness").ShouldBeFalse();

        sut.SetProperty("SourceShaderFile", OutlineFile);

        Get(sut, "Thickness").X.ShouldBe(5f);
    }

    [Fact]
    public void SetProperty_StemMatchesAcrossShaderFileTypes()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("Outline_Thickness", 5f);

        sut.SetProperty("SourceShaderFile", "Shaders/Outline.slang");

        Get(sut, "Thickness").X.ShouldBe(5f);
    }

    [Fact]
    public void SetProperty_ChangingTheShaderFile_StopsApplyingTheOldShadersVariablesAndStartsTheNewOnes()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Thickness", 5f);
        sut.SetProperty("Glow_Strength", 2f);
        Has(sut, "Thickness").ShouldBeTrue();
        Has(sut, "Strength").ShouldBeFalse();

        sut.SetProperty("SourceShaderFile", "Shaders/Glow.fx");

        Has(sut, "Thickness").ShouldBeFalse("a variable for the previous shader goes inert");
        Get(sut, "Strength").X.ShouldBe(2f);
    }

    [Fact]
    public void SetProperty_ChangingTheShaderFile_BringsBackTheTextValueTheVariableCovered()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("ShaderParameters", "Thickness=2.0");
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Thickness", 5f);
        Get(sut, "Thickness").X.ShouldBe(5f);

        sut.SetProperty("SourceShaderFile", "Shaders/Glow.fx");

        Get(sut, "Thickness").X.ShouldBe(2f);
    }

    [Fact]
    public void SetProperty_NullVariable_FallsBackToTheText()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("ShaderParameters", "Thickness=2.0");
        sut.SetProperty("Outline_Thickness", 5f);

        sut.SetProperty("Outline_Thickness", null);

        Get(sut, "Thickness").X.ShouldBe(2f);
    }

    [Fact]
    public void SetProperty_NewTextWithoutTheParameter_DoesNotWipeTheVariable()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Thickness", 5f);

        sut.SetProperty("ShaderParameters", "Glow=true");

        Get(sut, "Thickness").X.ShouldBe(5f);
        Has(sut, "Glow").ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_EmptyText_KeepsTheVariableAndDropsTheRest()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Thickness", 5f);
        sut.SetProperty("ShaderParameters", "Glow=true");

        sut.SetProperty("ShaderParameters", "");

        Get(sut, "Thickness").X.ShouldBe(5f);
        Has(sut, "Glow").ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_ComponentVariables_AssembleOneVector()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Outline_Tint__X", 0.1f);
        sut.SetProperty("Outline_Tint__Y", 0.2f);
        sut.SetProperty("Outline_Tint__Z", 0.3f);
        sut.SetProperty("Outline_Tint__W", 0.4f);

        ShaderParameterValue tint = Get(sut, "Tint");
        tint.Kind.ShouldBe(ShaderParameterKind.Float);
        tint.Count.ShouldBe(4);
        tint.X.ShouldBe(0.1f);
        tint.Y.ShouldBe(0.2f);
        tint.Z.ShouldBe(0.3f);
        tint.W.ShouldBe(0.4f);
        Has(sut, "Outline_Tint__X").ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_ComponentVariablesBeforeTheFile_AssembleOneVector()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("Outline_Tint__W", 0.4f);
        sut.SetProperty("Outline_Tint__X", 0.1f);
        sut.SetProperty("Outline_Tint__Y", 0.2f);

        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Tint__Z", 0.3f);

        ShaderParameterValue tint = Get(sut, "Tint");
        tint.Count.ShouldBe(4);
        tint.Z.ShouldBe(0.3f);
        tint.W.ShouldBe(0.4f);
    }

    [Fact]
    public void SetProperty_Float2Components_AssembleAVector2()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Outline_Offset__X", 1f);
        sut.SetProperty("Outline_Offset__Y", 2f);

        ShaderParameterValue offset = Get(sut, "Offset");
        offset.Count.ShouldBe(2);
        offset.X.ShouldBe(1f);
        offset.Y.ShouldBe(2f);
    }

    [Fact]
    public void SetProperty_PartialVector_ZeroFillsTheMissingComponents()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Outline_Tint__X", 0.5f);
        sut.SetProperty("Outline_Tint__Z", 0.7f);

        ShaderParameterValue tint = Get(sut, "Tint");
        tint.Count.ShouldBe(3);
        tint.X.ShouldBe(0.5f);
        tint.Y.ShouldBe(0f);
        tint.Z.ShouldBe(0.7f);
    }

    [Fact]
    public void SetProperty_OnlyTheFirstComponent_StillGivesAVector()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Outline_Tint__X", 0.5f);

        ShaderParameterValue tint = Get(sut, "Tint");
        tint.Count.ShouldBe(2);
        tint.X.ShouldBe(0.5f);
        tint.Y.ShouldBe(0f);
    }

    [Fact]
    public void SetProperty_VectorVariablesBeatAVectorInTheText_InAnyOrder()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("Outline_Tint__X", 1f);
        sut.SetProperty("Outline_Tint__Y", 1f);
        sut.SetProperty("Outline_Tint__Z", 1f);
        sut.SetProperty("Outline_Tint__W", 1f);
        sut.SetProperty("ShaderParameters", "Tint=(0.0, 0.0, 0.0, 0.0)");

        sut.SetProperty("SourceShaderFile", OutlineFile);

        ShaderParameterValue tint = Get(sut, "Tint");
        tint.X.ShouldBe(1f);
        tint.W.ShouldBe(1f);
    }

    [Fact]
    public void SetShaderParameter_CodeSetValue_IsLastWriterWins()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);
        sut.SetProperty("Outline_Thickness", 5f);

        sut.SetShaderParameter("Thickness", 8f);
        Get(sut, "Thickness").X.ShouldBe(8f);

        sut.SetShaderParameter("Other", 1f);
        Get(sut, "Other").X.ShouldBe(1f);
        Get(sut, "Thickness").X.ShouldBe(8f);
    }

    [Fact]
    public void SetProperty_VariableWithAnUnrelatedUnderscoreName_IsNotClaimed()
    {
        ContainerRuntime sut = new();
        sut.SetProperty("SourceShaderFile", OutlineFile);

        sut.SetProperty("Some_Custom", 1f);

        (GetParameters(sut)?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public void SetPropertyOnRenderable_LineRectangle_TheEditorsContainer_RoutesTheSameWay()
    {
        LineRectangle lineRectangle = new LineRectangle(null);
        GraphicalUiElement element = new GraphicalUiElement(lineRectangle);

        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(lineRectangle, element, "Outline_Thickness", 4f);
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(lineRectangle, element, "ShaderParameters", "Thickness=1.0; Glow=true");
        CustomSetPropertyOnRenderable.SetPropertyOnRenderable(lineRectangle, element, "SourceShaderFile", OutlineFile);

        lineRectangle.ShaderParameters.ShouldNotBeNull();
        lineRectangle.ShaderParameters!.TryGetValue("Thickness", out ShaderParameterValue thickness).ShouldBeTrue();
        thickness.X.ShouldBe(4f);
        lineRectangle.ShaderParameters.TryGetValue("Glow", out _).ShouldBeTrue();
    }
}
