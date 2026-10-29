using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

public class ShaderVariableNamingTests
{
    [Theory]
    [InlineData("Shaders/Outline.fx", "Outline")]
    [InlineData("Shaders\\Outline.fx", "Outline")]
    [InlineData("Outline.slang", "Outline")]
    [InlineData("Outline", "Outline")]
    [InlineData("../Content/Shaders/Outline.fs", "Outline")]
    public void GetSanitizedStem_PathOrName_ReturnsFileNameWithoutDirectoryOrExtension(string shaderFile, string expected)
    {
        ShaderVariableNaming.GetSanitizedStem(shaderFile).ShouldBe(expected);
    }

    [Theory]
    [InlineData("My Outline.fx", "My_Outline")]
    [InlineData("a-b.fx", "a_b")]
    [InlineData("A.B.fx", "A_B")]
    [InlineData("3d.fx", "_3d")]
    [InlineData("3d", "_3d")]
    [InlineData("Wave:Fast#1.fx", "Wave_Fast_1")]
    public void GetSanitizedStem_CharactersInvalidInNames_BecomeUnderscores(string shaderFile, string expected)
    {
        ShaderVariableNaming.GetSanitizedStem(shaderFile).ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(".fx")]
    [InlineData("Shaders/")]
    [InlineData("   ")]
    public void GetSanitizedStem_NoUsableFileName_FallsBackToShader(string? shaderFile)
    {
        ShaderVariableNaming.GetSanitizedStem(shaderFile).ShouldBe("Shader");
    }

    [Fact]
    public void GetSanitizedStem_OnlyTheLastExtensionIsRemoved()
    {
        ShaderVariableNaming.GetSanitizedStem("Glow.v2.fx").ShouldBe("Glow_v2");
    }

    [Fact]
    public void GetVariableName_Scalar_JoinsStemAndParameterWithUnderscore()
    {
        ShaderVariableNaming.GetVariableName("Shaders/Outline.fx", "Thickness").ShouldBe("Outline_Thickness");
    }

    [Fact]
    public void GetVariableName_SameShaderInDifferentFileTypes_GivesTheSameName()
    {
        string fx = ShaderVariableNaming.GetVariableName("Outline.fx", "Thickness");
        string slang = ShaderVariableNaming.GetVariableName("Content/Outline.slang", "Thickness");

        slang.ShouldBe(fx);
    }

    [Theory]
    [InlineData(0, "Outline_Tint__X")]
    [InlineData(1, "Outline_Tint__Y")]
    [InlineData(2, "Outline_Tint__Z")]
    [InlineData(3, "Outline_Tint__W")]
    public void GetVariableName_VectorComponent_AppendsDoubleUnderscoreAndAxisLetter(int index, string expected)
    {
        string name = ShaderVariableNaming.GetVariableName("Outline.fx", "Tint", index, componentCount: 4);

        name.ShouldBe(expected);
    }

    [Fact]
    public void GetVariableName_FromMarker_MatchesTheDirectOverloads()
    {
        ShaderParameterMarker scalar = new ShaderParameterMarker("Thickness");
        ShaderParameterMarker component = new ShaderParameterMarker("Tint", componentIndex: 2, componentCount: 3);

        ShaderVariableNaming.GetVariableName("Outline.fx", scalar).ShouldBe("Outline_Thickness");
        ShaderVariableNaming.GetVariableName("Outline.fx", component).ShouldBe("Outline_Tint__Z");
    }

    [Theory]
    [InlineData("Thickness", true)]
    [InlineData("_Thickness", true)]
    [InlineData("Tint_", true)]
    [InlineData("Tint2", true)]
    [InlineData("2Tint", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Out line", false)]
    [InlineData("Out.line", false)]
    [InlineData("Out-line", false)]
    [InlineData("Out#line", false)]
    [InlineData("Tint__X", false)]
    [InlineData("A__B", false)]
    public void IsSupportedParameterName_OnlyIdentifierCharactersWithoutDoubleUnderscore(string? parameter, bool expected)
    {
        ShaderVariableNaming.IsSupportedParameterName(parameter).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Out line")]
    [InlineData("Tint__X")]
    [InlineData("")]
    public void GetVariableName_UnsupportedParameter_Throws(string parameter)
    {
        Should.Throw<ArgumentException>(() => ShaderVariableNaming.GetVariableName("Outline.fx", parameter));
    }

    [Theory]
    [InlineData(-1, 4)]
    [InlineData(4, 4)]
    [InlineData(2, 2)]
    [InlineData(0, 1)]
    [InlineData(0, 5)]
    public void GetVariableName_ComponentOutOfRange_Throws(int index, int count)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => ShaderVariableNaming.GetVariableName("Outline.fx", "Tint", index, count));
    }

    [Theory]
    [InlineData("My Outline.fx", "Thickness", null)]
    [InlineData("a-b.fx", "Tint", 1)]
    [InlineData("A.B.fx", "Tint", 3)]
    [InlineData("3d.fx", "Scale", null)]
    [InlineData("", "Scale", null)]
    [InlineData("Wave:Fast#1.fx", "_Amount", null)]
    public void GetVariableName_AnyInput_IsAValidVariableAndCSharpIdentifier(string shaderFile, string parameter, int? component)
    {
        string name = component == null
            ? ShaderVariableNaming.GetVariableName(shaderFile, parameter)
            : ShaderVariableNaming.GetVariableName(shaderFile, parameter, component.Value, componentCount: 4);

        Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$").ShouldBeTrue(name);
    }

    [Fact]
    public void GetVariableName_EveryParameterAndComponentOfOneShader_IsDistinct()
    {
        // The documented guarantee: for one shader file, distinct (parameter, component) inputs never
        // share a name. Includes the awkward cases: trailing and leading underscores, a parameter that
        // looks like a component suffix without the double underscore.
        string[] parameters = { "Tint", "Tint_", "_Tint", "Tint_X", "_X", "X", "Tint2", "T" };
        HashSet<string> names = new HashSet<string>();
        int count = 0;
        foreach (string parameter in parameters)
        {
            names.Add(ShaderVariableNaming.GetVariableName("Outline.fx", parameter));
            count++;
            for (int index = 0; index < 4; index++)
            {
                names.Add(ShaderVariableNaming.GetVariableName("Outline.fx", parameter, index, componentCount: 4));
                count++;
            }
        }

        names.Count.ShouldBe(count);
    }

    [Theory]
    [InlineData("Outline.fx", "Outline_Thickness", "Thickness", null)]
    [InlineData("Shaders/Outline.slang", "Outline_Thickness", "Thickness", null)]
    [InlineData("Outline.fx", "Outline_Tint__X", "Tint", 0)]
    [InlineData("Outline.fx", "Outline_Tint__W", "Tint", 3)]
    [InlineData("Outline.fx", "Outline_Tint___Y", "Tint_", 1)]
    [InlineData("Outline.fx", "Outline__X", "_X", null)]
    [InlineData("My Outline.fx", "My_Outline_Scale", "Scale", null)]
    public void TryParseVariableName_CanonicalName_RecoversParameterAndComponent(
        string shaderFile, string variableName, string expectedParameter, int? expectedComponent)
    {
        bool parsed = ShaderVariableNaming.TryParseVariableName(
            shaderFile, variableName, out string parameter, out int? componentIndex);

        parsed.ShouldBeTrue();
        parameter.ShouldBe(expectedParameter);
        componentIndex.ShouldBe(expectedComponent);
    }

    [Theory]
    [InlineData("Outline.fx", "Wave_Thickness")]
    [InlineData("Outline.fx", "Thickness")]
    [InlineData("Outline.fx", "Outline_")]
    [InlineData("Outline.fx", "Outline")]
    [InlineData("Outline.fx", "")]
    [InlineData("Outline.fx", "Outline_A__B")]
    [InlineData("Outline.fx", "Outline___X")]
    [InlineData("Outline.fx", "Outline_Out line")]
    [InlineData("Outline.fx", "Outline_Tint__V")]
    public void TryParseVariableName_NotACanonicalNameForThisShader_ReturnsFalse(string shaderFile, string variableName)
    {
        ShaderVariableNaming.TryParseVariableName(shaderFile, variableName, out string parameter, out int? componentIndex)
            .ShouldBeFalse();
        parameter.ShouldBe("");
        componentIndex.ShouldBeNull();
    }

    [Fact]
    public void TryParseVariableName_NullName_ReturnsFalse()
    {
        ShaderVariableNaming.TryParseVariableName("Outline.fx", null, out _, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Outline_Tint__X", true)]
    [InlineData("Outline_Tint__W", true)]
    [InlineData("Tint__Y", true)]
    [InlineData("Outline_Thickness", false)]
    [InlineData("Outline_Tint_X", false)]
    [InlineData("Outline_Tint__V", false)]
    [InlineData("__X", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsComponentName_NameEndingInDoubleUnderscoreAndAxis_IsAComponent(string? variableName, bool expected)
    {
        ShaderVariableNaming.IsComponentName(variableName).ShouldBe(expected);
    }

    [Fact]
    public void RoundTrip_EveryNameThisShaderCanProduce_ParsesBackToItsInputs()
    {
        string[] parameters = { "Thickness", "Tint", "Tint_", "_Tint", "_X", "X", "A1" };
        foreach (string shaderFile in new[] { "Outline.fx", "My Outline.slang", "3d.fx", "A.B.fx" })
        {
            foreach (string parameter in parameters)
            {
                string scalarName = ShaderVariableNaming.GetVariableName(shaderFile, parameter);
                ShaderVariableNaming.TryParseVariableName(shaderFile, scalarName, out string scalarParameter, out int? scalarComponent)
                    .ShouldBeTrue(scalarName);
                scalarParameter.ShouldBe(parameter);
                scalarComponent.ShouldBeNull();

                for (int index = 0; index < 4; index++)
                {
                    string componentName = ShaderVariableNaming.GetVariableName(shaderFile, parameter, index, componentCount: 4);
                    ShaderVariableNaming.TryParseVariableName(shaderFile, componentName, out string componentParameter, out int? componentIndex)
                        .ShouldBeTrue(componentName);
                    componentParameter.ShouldBe(parameter);
                    componentIndex.ShouldBe(index);
                }
            }
        }
    }
}
