using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;
using ShadowDusk.Core.Preprocessor;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Turns a container's <c>SourceShaderFile</c> into bytecode with ShadowDusk. The editor canvas
/// (<see cref="RenderTargetShaderResolver"/>) and the Preview window both compile through it; the Preview
/// links this file rather than referencing the tool, so it depends on ShadowDusk only.
/// </summary>
internal static class ShaderSourceCompiler
{
    /// <summary>
    /// Returns ordinary <c>.fx</c> effect text for <paramref name="absolutePath"/>. A <c>.slang</c> file is
    /// converted first via ShadowDusk's Slang frontend (Slang has no technique/pass concept, so it can't go
    /// through <see cref="EffectCompiler.Compile"/> directly); every other extension is assumed to already be
    /// <c>.fx</c> source and passed through unchanged.
    /// </summary>
    public static string ReadFxSource(string absolutePath)
    {
        string sourceText = File.ReadAllText(absolutePath);
        if (!string.Equals(Path.GetExtension(absolutePath), SlangFrontend.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return sourceText;
        }

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            sourceText,
            new SlangConvertOptions { SourceName = absolutePath });

        if (conversion.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not convert the Slang render-target shader '" + absolutePath + "':\n" +
                string.Join("\n", conversion.Error.Select(error => error.Message)));
        }

        return conversion.Value.FxText;
    }

    /// <summary>
    /// Compiles <paramref name="source"/> for <paramref name="target"/> and returns the bytecode, or throws
    /// naming <paramref name="sourcePath"/> and every compiler error. <paramref name="sourcePath"/> is the
    /// shader's own path, which is what a relative <c>#include</c> resolves against. Each file the
    /// shader includes, nested includes too, is passed to <paramref name="onInclude"/> as ShadowDusk looks for it,
    /// a file that does not exist yet included, so creating it later can be noticed.
    /// </summary>
    public static byte[] Compile(string source, PlatformTarget target, string sourcePath, Action<string>? onInclude = null)
    {
        EffectCompiler compiler = new EffectCompiler();
        Result<CompiledShader, ShaderError[]> result = compiler.Compile(
            source,
            new CompilerOptions
            {
                Target = target,
                SourceFileName = sourcePath,
                IncludeResolver = new ReportingIncludeResolver(sourcePath, onInclude),
            });
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not compile the render-target shader '" + sourcePath + "':\n" +
                string.Join("\n", result.Error.Select(error => error.Message)));
        }
        return result.Value.Data;
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
