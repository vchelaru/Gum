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
}
