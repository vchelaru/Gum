using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// A fragment shader that reproduces one non-default
/// <see cref="global::RenderingLibrary.Graphics.ColorOperation"/> on raylib. Sprites and nine-slices
/// bind it around every draw call for that operation; Modulate (the default) draws unshaded.
///
/// <para>Both shaders emit <b>non-premultiplied</b> color, the same form a normal
/// <c>DrawTexturePro</c> emits (<c>texture.rgba * tint</c>), so they composite under the element's own
/// blend both on screen and inside a render-target bake (the premultiply pass in
/// <see cref="global::RenderingLibrary.Graphics.BatchDrawCallCounter"/>), and the tint alpha fades
/// the result linearly like any other draw.</para>
///
/// <para>The shader is created lazily on first <see cref="Shader"/> access, when a GL context exists.
/// The raylib <see cref="global::RenderingLibrary.Graphics.Renderer"/> owns the instances.</para>
/// </summary>
public sealed class ColorOperationShader
{
    // raylib's default-shader interface: the tint passed to DrawTexturePro arrives as fragColor;
    // colDiffuse stays white unless a material color is set.
    private const string Header = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
";

    /// <summary>
    /// ColorTextureAlpha (issue #3486): the texture supplies only an alpha mask, filled with the tint.
    /// </summary>
    public static ColorOperationShader CreateColorTextureAlpha() => new(Header + @"void main()
{
    float texAlpha = texture(texture0, fragTexCoord).a;
    vec4 tint = fragColor * colDiffuse;
    finalColor = vec4(tint.rgb, tint.a * texAlpha);
}
");

    /// <summary>
    /// Add (issue #7845): the tint's RGB is added to the texture's, and alpha is texture alpha times
    /// tint alpha. One pass, matching Skia's color-matrix Add.
    /// </summary>
    public static ColorOperationShader CreateAdd() => new(Header + @"void main()
{
    vec4 tex = texture(texture0, fragTexCoord);
    vec4 tint = fragColor * colDiffuse;
    finalColor = vec4(min(tex.rgb + tint.rgb, vec3(1.0)), tex.a * tint.a);
}
");

    private readonly string _fragmentSource;
    private Shader _shader;
    private bool _loaded;

    private ColorOperationShader(string fragmentSource)
    {
        _fragmentSource = fragmentSource;
    }

    /// <summary>
    /// The loaded fragment shader, created on first access. Bind it via
    /// <c>BatchDrawCallCounter.BeginShaderMode</c> around the element's draw calls.
    /// </summary>
    public Shader Shader
    {
        get
        {
            if (!_loaded)
            {
                _shader = LoadShaderFromMemory(null, _fragmentSource);
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
