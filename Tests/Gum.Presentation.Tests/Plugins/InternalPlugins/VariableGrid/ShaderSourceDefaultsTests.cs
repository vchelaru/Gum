using Gum.Plugins.InternalPlugins.VariableGrid;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.VariableGrid;

public class ShaderSourceDefaultsTests
{
    private static IReadOnlyDictionary<string, ShaderParameterValue> Parse(string source) =>
        ShaderSourceDefaults.Parse(source);

    [Fact]
    public void Parse_TheShippedOutlineShader_DeclaresAThicknessOfOne()
    {
        string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang"));

        IReadOnlyDictionary<string, ShaderParameterValue> defaults = Parse(source);

        defaults["Thickness"].X.ShouldBe(1f);
        defaults["Thickness"].Kind.ShouldBe(ShaderParameterKind.Float);
    }

    [Fact]
    public void Parse_FloatWithAnInitializer_ReturnsTheValue()
    {
        Parse("float Thickness = 1.0;")["Thickness"].X.ShouldBe(1f);
    }

    [Fact]
    public void Parse_FloatWithNoInitializer_IsLeftOut()
    {
        Parse("float Thickness;").ContainsKey("Thickness").ShouldBeFalse();
    }

    [Theory]
    [InlineData("float A = 2;", 2f)]
    [InlineData("float A = 2.5f;", 2.5f)]
    [InlineData("float A = .5;", 0.5f)]
    [InlineData("float A = -3.25;", -3.25f)]
    [InlineData("float A = 1e-1;", 0.1f)]
    [InlineData("uniform float A = 4.0;", 4f)]
    [InlineData("float A=7.0;", 7f)]
    [InlineData("float\n  A\n  =\n  8.0\n  ;", 8f)]
    public void Parse_FloatLiteralForms_AreAllRead(string source, float expected)
    {
        Parse(source)["A"].X.ShouldBe(expected);
    }

    [Fact]
    public void Parse_Int_ReturnsAnIntValue()
    {
        ShaderParameterValue value = Parse("int Steps = 4;")["Steps"];

        value.Kind.ShouldBe(ShaderParameterKind.Int);
        value.X.ShouldBe(4f);
    }

    [Theory]
    [InlineData("bool Glow = true;", 1f)]
    [InlineData("bool Glow = false;", 0f)]
    [InlineData("bool Glow = 1;", 1f)]
    public void Parse_Bool_ReturnsABoolValue(string source, float expected)
    {
        ShaderParameterValue value = Parse(source)["Glow"];

        value.Kind.ShouldBe(ShaderParameterKind.Bool);
        value.X.ShouldBe(expected);
    }

    [Fact]
    public void Parse_Float4Constructor_ReturnsEveryComponent()
    {
        ShaderParameterValue value = Parse("float4 Tint = float4(1.0, 0.5, 0.25, 1.0);")["Tint"];

        value.Count.ShouldBe(4);
        new[] { value.X, value.Y, value.Z, value.W }.ShouldBe(new[] { 1f, 0.5f, 0.25f, 1f });
    }

    [Fact]
    public void Parse_Float2AndFloat3Constructors_ReturnThatManyComponents()
    {
        ShaderParameterValue two = Parse("float2 Size = float2(3.0, 4.0);")["Size"];
        ShaderParameterValue three = Parse("float3 Color = float3(0.1, 0.2, 0.3);")["Color"];

        two.Count.ShouldBe(2);
        two.Y.ShouldBe(4f);
        three.Count.ShouldBe(3);
        three.Z.ShouldBe(0.3f);
    }

    [Fact]
    public void Parse_VectorConstructorWithOneArgument_FillsEveryComponent()
    {
        ShaderParameterValue value = Parse("float4 Tint = float4(0.5);")["Tint"];

        new[] { value.X, value.Y, value.Z, value.W }.ShouldBe(new[] { 0.5f, 0.5f, 0.5f, 0.5f });
    }

    [Theory]
    [InlineData("float4 Tint = float4(1.0, 0.5);")]
    [InlineData("float4 Tint = float2(1.0, 0.5);")]
    [InlineData("float4 Tint = float4(A, 0.5, 0.5, 0.5);")]
    [InlineData("float4 Tint = 1.0;")]
    public void Parse_VectorInitializerThatDoesNotMatchTheType_IsLeftOut(string source)
    {
        Parse(source).ContainsKey("Tint").ShouldBeFalse();
    }

    [Fact]
    public void Parse_CommentsAndWhitespaceAroundTheDeclaration_AreIgnored()
    {
        string source = """
            // float Thickness = 5.0;
            /* float Thickness = 6.0; */
            float /* the width */ Thickness // inline
                = 1.0 /* more */ ; // trailing
            """;

        Parse(source)["Thickness"].X.ShouldBe(1f);
    }

    [Fact]
    public void Parse_ACommentedOutDeclaration_IsNotRead()
    {
        Parse("// float Thickness = 5.0;\n/* float Other = 2.0; */").Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_LocalVariablesInsideAFunction_AreNotParameters()
    {
        string source = """
            float Thickness;
            float4 MainPS() { float Thickness = 2.0; float Local = 3.0; return 0; }
            """;

        IReadOnlyDictionary<string, ShaderParameterValue> defaults = Parse(source);

        defaults.ContainsKey("Thickness").ShouldBeFalse();
        defaults.ContainsKey("Local").ShouldBeFalse();
    }

    [Fact]
    public void Parse_AStaticGlobal_IsNotAParameter()
    {
        Parse("static float Hidden = 2.0;").ContainsKey("Hidden").ShouldBeFalse();
    }

    [Fact]
    public void Parse_SeveralDeclaratorsInOneStatement_AreAllRead()
    {
        IReadOnlyDictionary<string, ShaderParameterValue> defaults = Parse("float A = 1.0, B, C = float(2.0) , D = 3.0;");

        defaults["A"].X.ShouldBe(1f);
        defaults.ContainsKey("B").ShouldBeFalse();
        defaults["D"].X.ShouldBe(3f);
    }

    [Theory]
    [InlineData("float A = B * 2.0;")]
    [InlineData("float A = 1.0 / 3.0;")]
    [InlineData("float A = sin(1.0);")]
    [InlineData("float A = ;")]
    [InlineData("float A = 1.0e;")]
    [InlineData("float A = 1.0.0;")]
    [InlineData("int A = 1.5;")]
    [InlineData("int A = 99999999999;")]
    [InlineData("bool A = maybe;")]
    public void Parse_AnInitializerItCannotRead_IsLeftOutWithoutThrowing(string source)
    {
        Parse(source).ContainsKey("A").ShouldBeFalse();
    }

    [Fact]
    public void Parse_AnUnsupportedInitializer_DoesNotHideTheNextParameter()
    {
        IReadOnlyDictionary<string, ShaderParameterValue> defaults = Parse("float A = B * 2.0;\nfloat C = 5.0;");

        defaults["C"].X.ShouldBe(5f);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("float A = 1.0")]
    [InlineData("/* never closed float A = 1.0;")]
    [InlineData("float4(((((")]
    [InlineData("}}}} float A = 1.0;")]
    [InlineData("{ float A = 1.0;")]
    [InlineData("=;=;=;")]
    public void Parse_UnusualOrBrokenText_DoesNotThrow(string source)
    {
        Should.NotThrow(() => Parse(source));
    }

    [Fact]
    public void Parse_NullSource_ReturnsNothing()
    {
        ShaderSourceDefaults.Parse(null).Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_TexturesAndStructs_AreNotParameters()
    {
        string source = """
            Texture2D SpriteTexture;
            SamplerState SpriteTextureSampler;
            struct VertexShaderOutput { float4 Position : SV_POSITION; };
            """;

        Parse(source).Count.ShouldBe(0);
    }
}
