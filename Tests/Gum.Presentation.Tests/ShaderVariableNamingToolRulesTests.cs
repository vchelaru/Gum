using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// The canonical shader variable names have to be accepted by every grammar the tool puts a variable
/// name through: Gum's own name rules, a C# identifier in generated code, and a Roslyn identifier in a
/// variable reference row (which is parsed as C#).
/// </summary>
public class ShaderVariableNamingToolRulesTests
{
    [Theory]
    [InlineData("Shaders/My Outline.fx", "Thickness", null)]
    [InlineData("a-b.fx", "Tint", 0)]
    [InlineData("A.B.fx", "Tint", 3)]
    [InlineData("3d.fx", "Scale", null)]
    [InlineData("", "_Amount", null)]
    [InlineData("class.fx", "Tint", 2)]
    [InlineData("Wave:Fast#1.slang", "Tint2", 1)]
    [InlineData("アウトライン.fx", "Thickness", null)]
    [InlineData("輪郭ひらがなカタカナ.slang", "Tint", 2)]
    [InlineData("한글.fx", "Scale", null)]
    [InlineData("Ünïcode.fx", "Scale", null)]
    [InlineData("\u0301accent.fx", "Scale", null)]
    [InlineData("\U0001D400math.fx", "Scale", null)]
    public void GetVariableName_AnyInput_IsAcceptedByNameRulesCSharpAndRoslyn(string shaderFile, string parameter, int? component)
    {
        string name = component == null
            ? ShaderVariableNaming.GetVariableName(shaderFile, parameter)
            : ShaderVariableNaming.GetVariableName(shaderFile, parameter, component.Value, componentCount: 4);
        NameRules rules = new NameRules();

        rules.IsNameValidCommon(name, out string commonWhyNotValid, out _).ShouldBeTrue(commonWhyNotValid);
        rules.IsValidCSharpName(name, out string? csharpWhyNotValid, out _).ShouldBeTrue(csharpWhyNotValid);
        SyntaxFacts.IsValidIdentifier(name).ShouldBeTrue(name);
        SyntaxFactory.ParseExpression(name).ShouldBeOfType<IdentifierNameSyntax>();
    }

    [Theory]
    [InlineData("アウトライン.fx", "Thickness", null)]
    [InlineData("輪郭.slang", "Tint", 1)]
    public void TryParseVariableName_JapaneseFileName_RoundTripsThroughTheInverse(string shaderFile, string parameter, int? component)
    {
        string name = component == null
            ? ShaderVariableNaming.GetVariableName(shaderFile, parameter)
            : ShaderVariableNaming.GetVariableName(shaderFile, parameter, component.Value, componentCount: 4);

        ShaderVariableNaming.TryParseVariableName(shaderFile, name, out string parsedParameter, out int? parsedComponent)
            .ShouldBeTrue(name);

        parsedParameter.ShouldBe(parameter);
        parsedComponent.ShouldBe(component);
    }

    [Fact]
    public void GetVariableName_AnyCharacterInTheFileName_IsAcceptedByNameRulesCSharpAndRoslyn()
    {
        // Every BMP character and the first two supplementary planes, in the middle and at the start of
        // the file name (the start also has to be a letter or underscore for C#).
        NameRules rules = new NameRules();
        List<string> failures = new List<string>();

        for (int code = 0; code <= 0xFFFF; code++)
        {
            char character = (char)code;
            if (char.IsSurrogate(character) || character == '.' || character == '/' || character == '\\')
            {
                continue;
            }

            string name = ShaderVariableNaming.GetVariableName("a" + character + "b.fx", "Scale");
            string leading = ShaderVariableNaming.GetVariableName(character + "b.fx", "Scale");
            foreach (string candidate in new[] { name, leading })
            {
                if (!IsAccepted(rules, candidate))
                {
                    failures.Add("U+" + code.ToString("X4") + " -> " + candidate);
                }
            }
        }

        for (int code = 0x10000; code <= 0x2FFFF; code++)
        {
            string character = char.ConvertFromUtf32(code);
            string name = ShaderVariableNaming.GetVariableName("a" + character + "b.fx", "Scale");
            string leading = ShaderVariableNaming.GetVariableName(character + "b.fx", "Scale");
            foreach (string candidate in new[] { name, leading })
            {
                if (!IsAccepted(rules, candidate))
                {
                    failures.Add("U+" + code.ToString("X") + " -> " + candidate);
                }
            }
        }

        failures.Take(10).ShouldBe(new string[0], failures.Count + " rejected names");
    }

    private static bool IsAccepted(NameRules rules, string name) =>
        rules.IsNameValidCommon(name, out _, out _)
        && rules.IsValidCSharpName(name, out _, out _)
        && SyntaxFacts.IsValidIdentifier(name);

    [Fact]
    public void GetVariableName_AsAVariableSaveName_HasNoSourceObject()
    {
        // A dot would split the name into an instance and a property; the underscore joiner must not.
        VariableSave variable = new VariableSave { Name = ShaderVariableNaming.GetVariableName("A.B.fx", "Tint", 1, 4) };

        variable.SourceObject.ShouldBeNull();
        variable.Name.ShouldBe("A_B_Tint__Y");
    }

    [Fact]
    public void GetVariableName_QualifiedWithTheInstance_SplitsOnlyAtTheInstanceDot()
    {
        VariableSave variable = new VariableSave { Name = "Icon." + ShaderVariableNaming.GetVariableName("Outline.fx", "Thickness") };

        variable.SourceObject.ShouldBe("Icon");
        variable.GetRootName().ShouldBe("Outline_Thickness");
    }
}
