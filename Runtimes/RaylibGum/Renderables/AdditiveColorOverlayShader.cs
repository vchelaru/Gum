using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// Owns the fragment shader used for the extra additive pass that reproduces an authored
/// <see cref="global::Gum.Graphics.Animation.AnimationFrameColorOperation.Add"/> frame on raylib
/// (issue #4821 gap 2), mirroring MonoGame/KNI/FNA's <c>Renderer.DrawAdditiveColorOverlay</c>.
///
/// <para><b>Why not reuse <see cref="ColorTextureAlphaShader"/>:</b> that shader emits
/// non-premultiplied <c>(tint.rgb, tint.a * texAlpha)</c>, which is correct for a normal
/// straight-alpha draw (the GL blend factor <c>SrcAlpha</c> multiplies RGB by alpha during
/// blending) but wrong for this pass's additive blend factors (color source/destination both
/// <c>GL_ONE</c>, matching MonoGame's <c>BlendState.AddColorPreserveDestinationAlpha</c>) - those
/// factors don't multiply by alpha at all, so a non-premultiplied RGB would bleed the tint across
/// the sprite's full quad (including fully-transparent texture regions) instead of staying masked
/// to the drawn silhouette. This shader instead premultiplies RGB by the texture's own alpha
/// (<c>tint.rgb * texAlpha</c>), matching MonoGame's own ColorTextureAlpha pixel shader.</para>
/// </summary>
public sealed class AdditiveColorOverlayShader
{
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
    finalColor = vec4(tint.rgb * texAlpha, tint.a * texAlpha);
}
";

    private Shader _shader;
    private bool _loaded;

    /// <summary>
    /// The loaded fragment shader, created on first access. Bind it via
    /// <c>BatchDrawCallCounter.BeginShaderMode</c> around the additive overlay's draw call.
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
