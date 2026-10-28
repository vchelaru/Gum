// Outlines whatever a render-target container draws, for example a pixel art icon. The same file runs
// on MonoGame/KNI/FNA, raylib and Skia (ShadowDusk converts it for each). Five optional parameters,
// set in the tool's Variables tab or from code with ContainerRuntime.SetShaderParameter:
//   OutlineRed, OutlineGreen, OutlineBlue  outline color, each from 0 to 255 (all zero is black)
//   Thickness  outline width in art pixels, 1 to 8 (below 1 counts as 1)
//   PixelSize  size of one art pixel in render target pixels (below 1 counts as 1). A 16x16 icon
//              drawn 48 pixels wide has a PixelSize of 3
// The outline is opaque and drawn outside the content, so the container needs transparent margin of
// at least Thickness * PixelSize pixels around the content or the outline is cut off.
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

// Filled in by Gum with one render target pixel (1 / width, 1 / height) and hidden in the tool.
float2 TexelSize;

float OutlineRed;
float OutlineGreen;
float OutlineBlue;
float Thickness;
float PixelSize;

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

    // Unset parameters read as zero, which stands for the defaults: one pixel, one art pixel per pixel.
    float reach = max(1.0, floor(Thickness + 0.5));
    float2 step = TexelSize * max(1.0, floor(PixelSize + 0.5));

    float neighborAlpha = 0.0;

    // Every pixel within reach art pixels in x and y (a square), sampled at the same spot of the
    // neighboring art pixel so the outline edges land on art pixel boundaries.
    [unroll]
    for (int y = -8; y <= 8; y++)
    {
        [unroll]
        for (int x = -8; x <= 8; x++)
        {
            float2 offset = float2(x, y);
            if (abs(offset.x) <= reach && abs(offset.y) <= reach)
            {
                neighborAlpha = max(neighborAlpha, SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates + offset * step).a);
            }
        }
    }

    // The render target holds premultiplied color, so the opaque outline is added only where the
    // sprite does not already cover the pixel.
    float3 color = float3(OutlineRed, OutlineGreen, OutlineBlue) / 255.0;
    float4 outline = float4(color, 1.0) * neighborAlpha;
    float4 result = sprite + outline * (1.0 - sprite.a);
    return result * input.Color;
}
