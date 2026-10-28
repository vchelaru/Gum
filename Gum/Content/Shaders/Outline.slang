// Outlines whatever a render-target container draws, for example a pixel art icon. The same file runs
// on MonoGame/KNI/FNA, raylib and Skia (ShadowDusk converts it for each). Five optional parameters,
// set in the tool's Variables tab or from code with ContainerRuntime.SetShaderParameter:
//   OutlineRed, OutlineGreen, OutlineBlue  outline color, each from 0 to 255 (all zero is black)
//   Thickness  outline width in canvas pixels, 0 to 8 (default 1; 0 draws no outline)
//   PixelSize  size of one art pixel in canvas pixels (below 1 counts as 1). A 16x16 icon
//              drawn 48 canvas pixels wide has a PixelSize of 3
// The outline is opaque and drawn outside the content, so the container needs transparent margin of
// at least Thickness * PixelSize canvas pixels around the content or the outline is cut off.
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

// Filled in by Gum with one render target pixel (1 / width, 1 / height) and hidden in the tool.
float2 TexelSize;

float OutlineRed;
float OutlineGreen;
float OutlineBlue;
float Thickness;
float PixelSize;
// Set to 1 by Gum when Thickness was set, so a Thickness of 0 (no outline) differs from an unset one.
float ThicknessIsSet;

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

    // Unset parameters read as zero on some backends, so an unset Thickness counts as 1 and a PixelSize
    // below 1 counts as 1.
    float reach = ThicknessIsSet > 0.5 ? clamp(floor(Thickness + 0.5), 0.0, 8.0) : 1.0;
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
            float2 uv = input.TextureCoordinates + offset * step;
            // A neighbor past the edge of the render target is empty, whatever the sampler's address
            // mode (wrap would otherwise draw the outline of the far edge on this side).
            bool inside = uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0;
            if (inside && reach > 0.5 && abs(offset.x) <= reach && abs(offset.y) <= reach)
            {
                neighborAlpha = max(neighborAlpha, SpriteTexture.Sample(SpriteTextureSampler, uv).a);
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
