// Example grayscale post-process shader, authored in Slang instead of raw HLSL: a shader-stage
// attribute marks the entry point below, and there's no technique/pass block to write since
// ShadowDusk synthesizes one (issue #4677). Point a render-target Container's SourceShaderFile at
// a copy of this file to use it -- see the "Render Target Shaders" docs page for the full setup
// (IsRenderTarget + SourceShaderFile). Single-pass, no host-set parameters: the render-target
// blit sets no shader parameters, so effects like this must be self-contained.
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
