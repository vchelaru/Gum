// Same grayscale effect as Grayscale.fx, authored in Slang's own idiom instead of raw HLSL: a
// shader-stage attribute marks the entry point below, and there's no technique/pass block to
// write since ShadowDusk synthesizes one (issue #4677). Compare against Grayscale.fx to see how
// much per-shader-model boilerplate Slang input removes.
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader("fragment")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}
