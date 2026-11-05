using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Raylib;
using ShadowDusk.Core;

namespace Gum;

/// <summary>
/// Converts a shader ShadowDusk recognizes from its content (an <c>.fx</c> effect or Slang) to a raylib GLSL
/// fragment shader, loads it, and registers the defaults it declares with <see cref="RaylibShaderDefaults"/>.
/// The file name plays no part in telling the formats apart.
/// </summary>
internal sealed class ShadowDuskShaderLoader
{
    private readonly record struct CachedShader(DateTime LastWriteTimeUtc, long Length, Shader Shader);

    // A raylib Shader is a struct, so Gum's own cache (which keeps disposables) cannot hold it: without this,
    // every container that names the same file converts and compiles it again. An entry is reused only while
    // the file is unchanged, so a hot reload of the file loads the new text. A source that is not a loose file
    // (a stream, a bundle) has no stamp to compare and is converted each time.
    private readonly Dictionary<string, CachedShader> _cache = new Dictionary<string, CachedShader>();

    /// <summary>
    /// Loads <paramref name="source"/>, or returns null when ShadowDusk reports the input unsupported (nothing
    /// was attempted, for example raylib's own GLSL). A real conversion error throws.
    /// </summary>
    public Shader? TryLoad(ShaderSource source)
    {
        FileInfo file = new FileInfo(source.Name);
        bool isLooseFile = file.Exists;

        if (isLooseFile
            && _cache.TryGetValue(source.Name, out CachedShader cached)
            && cached.LastWriteTimeUtc == file.LastWriteTimeUtc
            && cached.Length == file.Length)
        {
            return cached.Shader;
        }

        Shader? shader = Convert(source);
        if (shader == null)
        {
            return null;
        }

        // The shader this one replaces is deliberately never unloaded, and its declared defaults stay registered. A
        // container keeps drawing with the Shader it was given until its SourceShaderFile is assigned again, and an
        // unloaded shader draws black with no error. The cost is one GL program per edit of a shader file during
        // hot reload, a development-time leak that ends with the process.
        if (isLooseFile)
        {
            _cache[source.Name] = new CachedShader(file.LastWriteTimeUtc, file.Length, shader.Value);
        }
        return shader;
    }

    private static Shader? Convert(ShaderSource source)
    {
        string text = source.ReadAllText();
        Result<RaylibShader, ShaderError[]> conversion =
            ShaderConverter.ConvertToRaylib(text, new RaylibConvertOptions(), source.Name);

        if (conversion.IsFailure)
        {
            if (ShadowDuskInput.IsUnsupported(conversion.Error))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"ShadowDusk could not convert '{source.Name}' to a raylib shader:\n{ShadowDuskInput.FormatErrors(conversion.Error)}");
        }

        RaylibShader converted = conversion.Value;
        Shader shader = Raylib.LoadShaderFromMemory(null, converted.FragmentShader);

        // raylib falls back to its default shader when the converted text does not compile.
        if (shader.Id == Rlgl.GetShaderIdDefault())
        {
            throw new InvalidOperationException($"raylib could not compile the shader ShadowDusk produced for '{source.Name}'. The compile error is in raylib's log.");
        }

        RegisterDeclaredDefaults(shader, converted);
        return shader;
    }

    // GLSL declares every uniform without a value, so the defaults the .fx or .slang declares are handed to
    // RaylibGum, which writes them for a parameter a container did not set.
    private static void RegisterDeclaredDefaults(Shader shader, RaylibShader converted)
    {
        ShaderUniformDefaults defaults = new ShaderUniformDefaults();
        foreach (RaylibUniform uniform in converted.Uniforms)
        {
            ShaderUniformDefault? declared = ShaderUniformDefault.FromTypeName(
                uniform.Name, uniform.GlslType, uniform.ArrayLength, uniform.DefaultValue);
            if (declared != null)
            {
                defaults.Add(declared);
            }
        }
        if (defaults.Count > 0)
        {
            RaylibShaderDefaults.Set(shader, defaults);
        }
    }
}
