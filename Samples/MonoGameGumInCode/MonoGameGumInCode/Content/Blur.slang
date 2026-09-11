// Fixed-radius 3x3 box blur, authored in Slang (issue #4679). Gum's render-target-effect blit
// sets no shader parameters and runs a single pass, so this bakes its sample offset in as a
// constant sized for this demo's 128x128 render target (one texel is 1/128) instead of exposing a
// host-settable radius -- an adjustable blur would need parameter-setting plus multi-pass support
// Gum doesn't have yet (tracked separately in issue #4679's discussion).
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
    float2 texel = float2(1.0 / 128.0, 1.0 / 128.0);
    float4 total = float4(0, 0, 0, 0);

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 uv = input.TextureCoordinates + float2(x, y) * texel;
            total += SpriteTexture.Sample(SpriteTextureSampler, uv);
        }
    }

    return (total / 9.0) * input.Color;
}
