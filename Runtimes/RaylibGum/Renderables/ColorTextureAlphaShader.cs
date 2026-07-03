using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// Owns the single fragment shader that reproduces MonoGame's
/// <see cref="global::RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha"/> on raylib: the
/// texture supplies only an alpha mask, and the sprite is filled with its tint color (issue #3486,
/// umbrella #3432). A <see cref="global::Gum.Renderables.Sprite"/> whose
/// <see cref="Sprite.ColorOperation"/> is <c>ColorTextureAlpha</c> binds this shader around its
/// <c>DrawTexturePro</c> calls; a <c>Modulate</c> sprite (the default) draws unshaded exactly as before.
///
/// <para><b>Output form:</b> the shader emits <b>non-premultiplied</b> <c>(tint.rgb, tint.a * texA)</c>
/// — the same form a normal <c>DrawTexturePro</c> emits (<c>texture.rgba * tint</c>), just with the
/// texture's RGB replaced by the tint's and the alpha masked by the texture. That is what lets a
/// ColorTextureAlpha sprite composite correctly both on screen (raylib's default straight-alpha blend)
/// and inside a render-target bake (the SrcAlpha/OneMinusSrcAlpha premultiply pass in
/// <see cref="global::RenderingLibrary.Graphics.BatchDrawCallCounter"/>), with no extra premultiply math.</para>
///
/// <para><b>Lifecycle:</b> the shader is created lazily on first <see cref="Shader"/> access (a GL
/// context must exist by then — the first Render inside a draw), held for reuse, and released by
/// <see cref="Dispose"/>. Mirrors <see cref="ShadowBlurRenderer"/>'s shader-ownership pattern; the
/// raylib <see cref="global::RenderingLibrary.Graphics.Renderer"/> owns the instance.</para>
/// </summary>
public sealed class ColorTextureAlphaShader
{
    // raylib's default-shader interface (texture0 / colDiffuse / fragColor / fragTexCoord). The tint
    // passed to DrawTexturePro arrives as fragColor; colDiffuse stays white unless a material color is
    // set. rgb is taken from the tint and alpha is the tint's alpha masked by the sampled texture alpha.
    private const string FragmentShader = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
void main()
{
    float texAlpha = texture(texture0, fragTexCoord).a;
    vec4 tint = fragColor * colDiffuse;
    finalColor = vec4(tint.rgb, tint.a * texAlpha);
}
";

    private Shader _shader;
    private bool _loaded;

    /// <summary>
    /// The loaded fragment shader, created on first access. Bind it via
    /// <c>BatchDrawCallCounter.BeginShaderMode</c> around a ColorTextureAlpha sprite's draw calls.
    /// </summary>
    public Shader Shader
    {
        get
        {
            if (!_loaded)
            {
                _shader = LoadShaderFromMemory(null, FragmentShader);
                _loaded = true;
            }
            return _shader;
        }
    }

    /// <summary>Releases the shader if it was loaded. Call on renderer shutdown.</summary>
    public void Dispose()
    {
        if (_loaded)
        {
            UnloadShader(_shader);
            _loaded = false;
        }
    }
}
