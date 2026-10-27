// Outlines whatever a render-target container draws, for example a pixel art icon. The same file runs
// on MonoGame/KNI/FNA, raylib and Skia (ShadowDusk converts it for each). Set the three parameters
// from code with ContainerRuntime.SetShaderParameter:
//   TexelSize    one pixel of the container's render target as a fraction, e.g. (1/144, 1/144)
//   Thickness    outline width in pixels, from 0 to 8 (0 draws no outline)
//   OutlineColor red, green, blue and alpha from 0 to 1
// The outline is drawn outside the content, so the container needs transparent margin of at least
// Thickness pixels around it or the outline is cut off.
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

float2 TexelSize;
float Thickness;
float4 OutlineColor;

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

    // The strongest alpha found in a ring of eight neighbors, for each whole pixel distance up to
    // Thickness.
    float neighborAlpha = 0.0;

    [unroll]
    for (int ring = 1; ring <= 8; ring++)
    {
        float distance = ring;
        float2 step = TexelSize * distance;
        float diagonal = 0.70710678;

        float ringAlpha = 0.0;
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + float2(step.x, 0)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + float2(-step.x, 0)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + float2(0, step.y)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + float2(0, -step.y)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + step * float2(diagonal, diagonal)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + step * float2(-diagonal, diagonal)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + step * float2(diagonal, -diagonal)).a);
        ringAlpha = max(ringAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + step * float2(-diagonal, -diagonal)).a);

        neighborAlpha = max(neighborAlpha, distance <= Thickness ? ringAlpha : 0.0);
    }

    // The render target holds premultiplied color, so the outline color is premultiplied too and
    // only shows where the sprite does not already cover the pixel.
    float4 outline = float4(OutlineColor.rgb * OutlineColor.a, OutlineColor.a) * neighborAlpha;
    float4 result = sprite + outline * (1.0 - sprite.a);
    return result * input.Color;
}
