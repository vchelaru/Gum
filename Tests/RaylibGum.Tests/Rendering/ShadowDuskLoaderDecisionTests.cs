using System;
using Gum;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #7104: the raylib loader declines input ShadowDusk does not read (null, so the next resolver runs) and
/// throws for a recognized shader that does not convert. Both happen before any GL call.
/// </summary>
public class ShadowDuskLoaderDecisionTests
{
    private const string BrokenFx = @"
float4 MainPS(float4 color : COLOR0) : COLOR0 { return notAFunction(color); }
technique T { pass P0 { PixelShader = compile ps_3_0 MainPS(); } };
";

    private const string RaylibGlsl = "#version 330\nin vec2 fragTexCoord;\nout vec4 finalColor;\nvoid main() { finalColor = vec4(1.0); }\n";

    private static ShaderSource SourceOf(string name, string text) =>
        new ShaderSource(name, () => new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)));

    [Fact]
    public void TryLoad_ReturnsNull_WhenShadowDuskReportsTheInputUnsupported_EvenUnderAnFxName()
    {
        new ShadowDuskShaderLoader().TryLoad(SourceOf("shader.fx", RaylibGlsl)).ShouldBeNull();
    }

    [Fact]
    public void TryLoad_Throws_WhenARecognizedShaderDoesNotConvert()
    {
        Should.Throw<InvalidOperationException>(
            () => new ShadowDuskShaderLoader().TryLoad(SourceOf("broken", BrokenFx)));
    }
}
