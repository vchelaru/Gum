// Example fixed-radius 9x9 box blur, authored in Slang. Gum's render-target-effect blit sets no
// shader parameters and runs a single pass, so the sample spacing below is a compile-time
// constant, not a host-adjustable radius -- an adjustable blur would need parameter-setting plus
// multi-pass support Gum doesn't have yet. The "texel" constant assumes a 128-pixel-square render
// target; if your own container is a different size, change 128.0 to match its Width/Height so
// the blur samples land one (or two) real pixels apart instead of drifting off scale.
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
    // Two texels between taps (not one) so the 9x9 grid covers a visibly larger area instead of
    // just adding softer sampling density over the same small radius.
    float2 texel = float2(2.0 / 128.0, 2.0 / 128.0);
    float4 total = float4(0, 0, 0, 0);

    [unroll]
    for (int y = -4; y <= 4; y++)
    {
        [unroll]
        for (int x = -4; x <= 4; x++)
        {
            float2 uv = input.TextureCoordinates + float2(x, y) * texel;
            total += SpriteTexture.Sample(SpriteTextureSampler, uv);
        }
    }

    return (total / 81.0) * input.Color;
}
