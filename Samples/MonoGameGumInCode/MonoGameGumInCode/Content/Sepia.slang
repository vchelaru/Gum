// Classic sepia-tone post-process, authored in Slang (issue #4679). Single-pass, no host-set
// parameters, matching Gum's render-target-effect contract -- see Grayscale.slang for the
// simplest possible example of that shape; this one just replaces the grayscale weights with the
// standard three-channel sepia matrix.
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
    float3 sepia;
    sepia.r = dot(color.rgb, float3(0.393, 0.769, 0.189));
    sepia.g = dot(color.rgb, float3(0.349, 0.686, 0.168));
    sepia.b = dot(color.rgb, float3(0.272, 0.534, 0.131));
    return float4(saturate(sepia), color.a);
}
