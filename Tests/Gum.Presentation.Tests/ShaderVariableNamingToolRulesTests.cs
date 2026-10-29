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
