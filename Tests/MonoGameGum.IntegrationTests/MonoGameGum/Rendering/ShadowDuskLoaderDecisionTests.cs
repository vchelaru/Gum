using System;
using Gum;
using Shouldly;
using ShadowDusk.Core;
using Xunit;

namespace MonoGameGum.IntegrationTests.Rendering;

/// <summary>
/// #7104: the MonoGame/KNI/FNA loader declines input ShadowDusk does not read and throws for a recognized
/// shader that does not compile. Both decisions are made before an Effect exists, so no device is needed.
/// </summary>
public class ShadowDuskLoaderDecisionTests
{
    private const string BrokenFx = @"
float4 MainPS(float4 color : COLOR0) : COLOR0 { return notAFunction(color); }
technique T { pass P0 { PixelShader = compile ps_3_0 MainPS(); } };
";

    private const string Glsl = "#version 330\nin vec2 fragTexCoord;\nout vec4 finalColor;\nvoid main() { finalColor = vec4(1.0); }\n";

    [Fact]
    public void TryLoad_ReturnsNull_WhenShadowDuskReportsTheInputUnsupported_EvenUnderAnFxName()
    {
        ShadowDuskEffectLoader.TryLoad(null!, "shader.fx", Glsl, PlatformTarget.OpenGL).ShouldBeNull();
    }

    [Fact]
    public void TryLoad_Throws_WhenARecognizedShaderDoesNotCompile()
    {
        Should.Throw<InvalidOperationException>(
            () => ShadowDuskEffectLoader.TryLoad(null!, "broken", BrokenFx, PlatformTarget.OpenGL));
    }

    [Fact]
    public void CanLoad_ReadsAnSkslFileByItsName_WhoseTextAloneIsUnrecognized()
    {
        const string sksl = "uniform shader SpriteTexture;\nhalf4 main(float2 c) { return SpriteTexture.eval(c); }";

        ShadowDuskEffectLoader.CanLoad(sksl, "Tint.sksl").ShouldBeTrue();
        ShadowDuskEffectLoader.CanLoad(sksl, "Tint.txt").ShouldBeFalse();
    }

    [Fact]
    public void TryLoad_UnsupportedSkSL_ThrowsWithTheShadowDuskCodeTheLineAndTheSupportedForm()
    {
        const string sksl = "uniform shader SpriteTexture;\nhalf4 main(float2 c) {\n    return half4(sk_FragCoord.x);\n}";

        InvalidOperationException error = Should.Throw<InvalidOperationException>(
            () => ShadowDuskEffectLoader.TryLoad(null!, "Bad.sksl", sksl, PlatformTarget.OpenGL));

        error.Message.ShouldContain("Bad.sksl(3,");
        error.Message.ShouldContain("SD0666");
        error.Message.ShouldContain("sk_FragCoord");
        error.Message.ShouldContain("uniform shader SpriteTexture");
    }
}
