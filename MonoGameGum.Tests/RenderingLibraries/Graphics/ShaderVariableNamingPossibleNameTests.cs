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
    [InlineData("B__Z")]
    [InlineData("Shader__Z")]
    [InlineData("Z__W")]
    [InlineData("b__W")]
    public void IsPossibleVariableName_NamesSomeShaderCouldProduce_ReturnsTrue(string name)
    {
        ShaderVariableNaming.IsPossibleVariableName(name).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Thickness")]
    [InlineData("_Thickness")]
    [InlineData("Some_")]
    [InlineData("_Name")]
    [InlineData("a_b.c")]
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

    [Fact]
    public void IsPossibleVariableName_IsTrueForEveryNameTryParseVariableNameAccepts_BoundedSweep()
    {
        // Every string over a small alphabet up to length 7, against shader files whose stems include a
        // trailing underscore, a leading underscore and the component letters.
        string[] shaderFiles = { "B_.fx", "B.fx", "_.fx", "Z.fx", "_Z.fx", "Shader.fx", "a_b_.fx" };
        char[] alphabet = { 'a', '_', 'Z', 'X', 'W' };
        int disagreements = 0;
        string? first = null;

        for (int length = 1; length <= 7; length++)
        {
            int total = 1;
            for (int i = 0; i < length; i++) total *= alphabet.Length;
            for (int code = 0; code < total; code++)
            {
                char[] characters = new char[length];
                int remaining = code;
                for (int i = 0; i < length; i++)
                {
                    characters[i] = alphabet[remaining % alphabet.Length];
                    remaining /= alphabet.Length;
                }
                string name = new string(characters);

                foreach (string shaderFile in shaderFiles)
                {
                    if (ShaderVariableNaming.TryParseVariableName(shaderFile, name, out _, out _)
                        && !ShaderVariableNaming.IsPossibleVariableName(name))
                    {
                        disagreements++;
                        first ??= shaderFile + " / " + name;
                    }
                }
            }
        }

        disagreements.ShouldBe(0, "first disagreement: " + first);
    }

    [Theory]
    [InlineData("B_.fx", "Z")]
    [InlineData("B.fx", "_Z")]
    [InlineData("Z.fx", "W")]
    [InlineData("b_.fx", "W")]
    public void IsPossibleVariableName_ScalarWhoseNameLooksLikeAComponent_IsTrue(string shaderFile, string parameter)
    {
        ShaderVariableNaming.IsPossibleVariableName(ShaderVariableNaming.GetVariableName(shaderFile, parameter)).ShouldBeTrue();
    }
}
