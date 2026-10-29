using System.Collections.Generic;
using Raylib_cs;

namespace RenderingLibrary.Graphics;

/// <summary>
/// The defaults a raylib shader declares (<c>float Thickness = 1.0;</c>). A GLSL uniform has no value
/// in the shader text, so raylib cannot report them; whatever converted the shader (ShadowDusk reports
/// them as <c>RaylibUniform.DefaultValue</c>) registers them here, and a container that does not set a
/// parameter then draws with the declared default instead of zero.
/// </summary>
/// <remarks>
/// <see cref="Shader"/> is a value struct, so the defaults are kept by shader id. Call
/// <see cref="Remove"/> when the shader is unloaded, since raylib may reuse the id. Accessed only on
/// the render thread.
/// </remarks>
public static class RaylibShaderDefaults
{
    private static readonly Dictionary<uint, ShaderUniformDefaults> _defaultsByShaderId = new();

    /// <summary>Records the declared defaults of <paramref name="shader"/>, replacing any earlier ones.</summary>
    public static void Set(Shader shader, ShaderUniformDefaults defaults)
    {
        _defaultsByShaderId[shader.Id] = defaults;
    }

    /// <summary>Forgets the defaults registered for <paramref name="shader"/>.</summary>
    public static void Remove(Shader shader)
    {
        _defaultsByShaderId.Remove(shader.Id);
    }

    internal static ShaderUniformDefaults? TryGet(Shader shader) =>
        _defaultsByShaderId.TryGetValue(shader.Id, out ShaderUniformDefaults? defaults) ? defaults : null;

    /// <summary>Writes <paramref name="declared"/> to <paramref name="location"/> in <paramref name="shader"/>.</summary>
    internal static void Write(Shader shader, int location, ShaderUniformDefault declared)
    {
        int count = System.Math.Max(1, declared.ArrayLength);
        int components = System.Math.Clamp(declared.ComponentCount, 1, 4);
        float[] values = declared.Values;

        switch (declared.Kind)
        {
            case ShaderParameterKind.Int:
            {
                int[] ints = new int[values.Length];
                for (int i = 0; i < ints.Length; i++) ints[i] = (int)values[i];
                ShaderUniformDataType type = components switch
                {
                    1 => ShaderUniformDataType.Int,
                    2 => ShaderUniformDataType.IVec2,
                    3 => ShaderUniformDataType.IVec3,
                    _ => ShaderUniformDataType.IVec4,
                };
                Raylib.SetShaderValueV(shader, location, ints, type, count);
                break;
            }
            case ShaderParameterKind.Bool:
            {
                uint[] uints = new uint[values.Length];
                for (int i = 0; i < uints.Length; i++) uints[i] = (uint)values[i];
                ShaderUniformDataType type = components switch
                {
                    1 => ShaderUniformDataType.UInt,
                    2 => ShaderUniformDataType.UIVec2,
                    3 => ShaderUniformDataType.UIVec3,
                    _ => ShaderUniformDataType.UIVec4,
                };
                Raylib.SetShaderValueV(shader, location, uints, type, count);
                break;
            }
            default:
            {
                ShaderUniformDataType type = components switch
                {
                    1 => ShaderUniformDataType.Float,
                    2 => ShaderUniformDataType.Vec2,
                    3 => ShaderUniformDataType.Vec3,
                    _ => ShaderUniformDataType.Vec4,
                };
                Raylib.SetShaderValueV(shader, location, values, type, count);
                break;
            }
        }
    }
}
