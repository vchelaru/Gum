using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

/// <summary>
/// <c>ShaderVariableNaming.IsPossibleVariableName</c> is the shader-free test that a name could be a
/// canonical variable name of some shader. It must never reject a name that
/// <c>TryParseVariableName</c> accepts for a real shader, or the runtime would drop the variable before it
/// knows the shader.
/// </summary>
public class ShaderVariableNamingPossibleNameTests
{
    [Theory]
    [InlineData("Outline_Thickness")]
    [InlineData("Outline_Tint__X")]
    [InlineData("Outline_Foo_")]
    [InlineData("_Glow_Thickness")]
    [InlineData("_1abc_Tint__W")]
    [InlineData("__P")]
    [InlineData("My_Outline_Line_Width")]
    public void IsPossibleVariableName_NamesSomeShaderCouldProduce_ReturnsTrue(string name)
    {
        ShaderVariableNaming.IsPossibleVariableName(name).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Thickness")]
    [InlineData("_Thickness")]
    [InlineData("Outline_")]
    [InlineData("Outline_Out line")]
    [InlineData("Outline.Thickness")]
    public void IsPossibleVariableName_NamesNoShaderCouldProduce_ReturnsFalse(string? name)
    {
        ShaderVariableNaming.IsPossibleVariableName(name).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Outline.fx")]
    [InlineData("_Glow.fx")]
    [InlineData("1abc.fx")]
    [InlineData("My Outline.fx")]
    [InlineData("Shaders/\u30A2.slang")]
    public void IsPossibleVariableName_AcceptsEveryNameTheShaderItselfProduces(string shaderFile)
    {
        foreach (string parameter in new[] { "Thickness", "Foo_", "a", "Line_Width", "_x" })
        {
            ShaderVariableNaming.IsPossibleVariableName(ShaderVariableNaming.GetVariableName(shaderFile, parameter)).ShouldBeTrue();
            for (int index = 0; index < 4; index++)
            {
                ShaderVariableNaming.IsPossibleVariableName(
                    ShaderVariableNaming.GetVariableName(shaderFile, parameter, index, 4)).ShouldBeTrue();
            }
        }
    }
}
