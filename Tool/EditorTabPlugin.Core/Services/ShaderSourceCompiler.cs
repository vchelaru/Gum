using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShadowDusk.Compiler;

using ShadowDusk.Core;
using ShadowDusk.Core.Preprocessor;
using ShadowDusk.SkSL;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Turns a container's <c>SourceShaderFile</c> into bytecode with ShadowDusk. The editor canvas
/// (<see cref="RenderTargetShaderResolver"/>) and the Preview window both compile through it; the Preview
/// links this file rather than referencing the tool, so it depends on ShadowDusk only.
/// </summary>
internal static class ShaderSourceCompiler
{
    /// <summary>
    /// Compiles <paramref name="source"/> for <paramref name="target"/> and returns the bytecode, or throws
    /// naming <paramref name="sourcePath"/> and every compiler error. ShadowDusk tells <c>.fx</c> from Slang by
    /// the text, so the file name plays no part; <paramref name="sourcePath"/> is the shader's own path, which
    /// is what a relative <c>#include</c> resolves against. Input ShadowDusk does not read throws too. Each file the
    /// shader includes, nested includes too, is passed to <paramref name="onInclude"/> as ShadowDusk looks for it,
    /// a file that does not exist yet included, so creating it later can be noticed. A <paramref name="profile"/>
    /// overrides <paramref name="target"/> with a whole (runtime, container) contract, null keeps the default MGFX output.
    /// </summary>
    public static byte[] Compile(string source, PlatformTarget target, string sourcePath, Action<string>? onInclude = null, CapabilityProfile? profile = null)
    {
        if (sourcePath.EndsWith(".sksl", StringComparison.OrdinalIgnoreCase))
        {
            // The text of an SkSL shader does not identify it, so the extension decides (the compile below
            // gets it as a name hint). Registering also covers a trimmed host.
            SkSLFrontend.Register();
        }

        Result<CompiledShader, ShaderError[]> result = ShaderConverter.Compile(
            new EffectCompiler(),
            source,
            new CompilerOptions
            {
                Target = target,
                Profile = profile,
                SourceFileName = sourcePath,
                IncludeResolver = new ReportingIncludeResolver(sourcePath, onInclude),
            },
            sourcePath);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not compile the render-target shader '" + sourcePath + "':\n" +
                FormatErrors(result.Error));
        }
        return result.Value.Data;
    }

    // SD0660 to SD0673 are the codes of ShadowDusk's SkSL input frontend.
    internal const string SkslHint =
        "Gum reads SkSL shaders of the form 'uniform shader SpriteTexture; half4 main(float2 coord)'. " +
        "sk_FragCoord, layout(color), color filters and blenders are not supported.";

    /// <summary>One line per error with its file, line, column and ShadowDusk code, plus what to change for an SkSL error.</summary>
    internal static string FormatErrors(IEnumerable<ShaderError> errors)
    {
        ShaderError[] list = errors.ToArray();
        string text = string.Join("\n", list.Select(
            error => string.IsNullOrEmpty(error.Code)
                ? error.Message
                : error.File + "(" + error.Line + "," + error.Column + "): " + error.Code + ": " + error.Message));
        bool isSksl = list.Any(error =>
            string.CompareOrdinal(error.Code, "SD0660") >= 0 && string.CompareOrdinal(error.Code, "SD0673") <= 0);
        return isSksl ? text + "\n" + SkslHint : text;
    }

    /// <summary>
    /// ShadowDusk's own file-system resolution (the same one it uses with no resolver set), reporting each
    /// file it looks for. ShadowDusk lists no files read in its result, so this is the only way to learn them.
    /// A file that is not found is reported at the place ShadowDusk looks first, next to the file that includes it,
    /// so a shader that stopped compiling because an include went missing reloads when the file comes back.
    /// </summary>
    private sealed class ReportingIncludeResolver : IIncludeResolver
    {
        private readonly FileSystemIncludeResolver _fileSystem = new FileSystemIncludeResolver();
        private readonly string _sourcePath;
        private readonly Action<string>? _onInclude;

        public ReportingIncludeResolver(string sourcePath, Action<string>? onInclude)
        {
            _sourcePath = sourcePath;
            _onInclude = onInclude;
        }

        public Result<IncludeResolvedFile, ShaderError> Resolve(
            string includePath, string? includingFilePath, IReadOnlyList<string> additionalSearchPaths)
        {
            Result<IncludeResolvedFile, ShaderError> resolved =
                _fileSystem.Resolve(includePath, includingFilePath, additionalSearchPaths);
            string? reported = resolved.IsFailure ? NextToIncluder(includePath, includingFilePath) : resolved.Value.FilePath;
            if (reported != null)
            {
                _onInclude?.Invoke(reported);
            }
            return resolved;
        }

        private string? NextToIncluder(string includePath, string? includingFilePath)
        {
            try
            {
                string folder = Path.GetDirectoryName(includingFilePath ?? _sourcePath) ?? "";
                return Path.GetFullPath(Path.Combine(folder, includePath));
            }
            catch (ArgumentException)
            {
                // A spelling that is no path cannot be created later either.
                return null;
            }
        }
    }
}
