// Outlines whatever a render-target container draws, for example a pixel art icon. The same file runs
// on MonoGame/KNI/FNA, raylib and Skia (ShadowDusk converts it for each). Optional parameters, set
// in the tool's Variables tab or from code with ContainerRuntime.SetShaderParameter:
//   Thickness    outline width in whole pixels, 1 to 8 (below 1 counts as 1)
//   OutlineColor red, green, blue and alpha from 0 to 1 (all zero counts as opaque black)
// Set both: an unset parameter is not reliable on every backend (see the Skia note in the PR).
// The outline is drawn outside the content, so the container needs transparent margin of at least
// Thickness pixels around it or the outline is cut off.
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

// Filled in by Gum with one pixel of the render target (1 / width, 1 / height); never set it.
float2 TexelSize;
float Thickness = 1.0;
float4 OutlineColor = float4(0.0, 0.0, 0.0, 1.0);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader("fragment")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 sprite = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates);

    float2 texel = TexelSize;

    // A parameter nobody set reads as zero on raylib and Skia, so zero (or less) stands for the defaults:
    // one pixel, opaque black.
    float reach = max(1.0, floor(Thickness + 0.5));
    float4 color = OutlineColor;
    if (color.r + color.g + color.b + color.a == 0.0)
    {
        color = float4(0.0, 0.0, 0.0, 1.0);
    }

    float neighborAlpha = 0.0;

    [unroll]
    for (int y = -8; y <= 8; y++)
    {
        [unroll]
        for (int x = -8; x <= 8; x++)
        {
            float2 offset = float2(x, y);
            if (abs(offset.x) <= reach && abs(offset.y) <= reach)
            {
                neighborAlpha = max(neighborAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + offset * texel).a);
            }
        }
    }

    float4 outline = float4(color.rgb * color.a, color.a) * neighborAlpha;
    float4 result = sprite + outline * (1.0 - sprite.a);
    return result * input.Color;
}
