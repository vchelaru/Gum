using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raylib_cs;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler.Raylib;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;

namespace Gum;

/// <summary>
/// Converts an <c>.fx</c> or <c>.slang</c> shader file to a raylib GLSL fragment shader with ShadowDusk, loads
/// it, and registers the defaults it declares with <see cref="RaylibShaderDefaults"/>.
/// </summary>
internal sealed class ShadowDuskShaderLoader
{
    private readonly record struct CachedShader(DateTime LastWriteTimeUtc, long Length, Shader Shader);

    // A raylib Shader is a struct, so Gum's own cache (which keeps disposables) cannot hold it: without this,
    // every container that names the same file converts and compiles it again. An entry is reused only while
    // the file is unchanged, so a hot reload of the file loads the new text.
    private readonly Dictionary<string, CachedShader> _cache = new Dictionary<string, CachedShader>();

    public static bool CanLoad(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".fx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, SlangFrontend.Extension, StringComparison.OrdinalIgnoreCase);
    }

    public Shader Load(string path)
    {
        FileInfo file = new FileInfo(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException($"Shader file not found: {path}", path);
        }

        if (_cache.TryGetValue(path, out CachedShader cached)
            && cached.LastWriteTimeUtc == file.LastWriteTimeUtc
            && cached.Length == file.Length)
        {
            return cached.Shader;
        }

        Shader shader = Convert(path);
        _cache[path] = new CachedShader(file.LastWriteTimeUtc, file.Length, shader);
        return shader;
    }

    private static Shader Convert(string path)
    {
        string source = File.ReadAllText(path);
        RaylibConvertOptions options = new RaylibConvertOptions();

        bool isSlang = string.Equals(Path.GetExtension(path), SlangFrontend.Extension, StringComparison.OrdinalIgnoreCase);
        Result<RaylibShader, ShaderError[]> conversion = isSlang
            ? RaylibConverter.ConvertSlang(source, options)
            : RaylibConverter.Convert(source, options);

        if (conversion.IsFailure)
        {
            string errors = string.Join("\n", conversion.Error.Select(
                error => $"{error.File}({error.Line},{error.Column}): {error.Code}: {error.Message}"));
            throw new InvalidOperationException($"ShadowDusk could not convert '{path}' to a raylib shader:\n{errors}");
        }

        RaylibShader converted = conversion.Value;
        Shader shader = Raylib.LoadShaderFromMemory(null, converted.FragmentShader);

        // raylib falls back to its default shader when the converted text does not compile.
        if (shader.Id == Rlgl.GetShaderIdDefault())
        {
            throw new InvalidOperationException($"raylib could not compile the shader ShadowDusk produced for '{path}'. The compile error is in raylib's log.");
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
