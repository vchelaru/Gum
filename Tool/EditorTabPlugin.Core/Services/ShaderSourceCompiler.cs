using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShadowDusk.Compiler;

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
    /// Compiles <paramref name="source"/> for <paramref name="target"/> and returns the bytecode, or throws
    /// naming <paramref name="sourcePath"/> and every compiler error. ShadowDusk tells <c>.fx</c> from Slang by
    /// the text, so the file name plays no part; <paramref name="sourcePath"/> is the shader's own path, which
    /// is what a relative <c>#include</c> resolves against. Input ShadowDusk does not read throws too. Each file the
    /// shader includes, nested includes too, is passed to <paramref name="onInclude"/> as ShadowDusk looks for it,
    /// a file that does not exist yet included, so creating it later can be noticed.
    /// </summary>
    public static byte[] Compile(string source, PlatformTarget target, string sourcePath, Action<string>? onInclude = null)
    {
        Result<CompiledShader, ShaderError[]> result = ShaderConverter.Compile(
            new EffectCompiler(),
            source,
            new CompilerOptions
            {
                Target = target,
                SourceFileName = sourcePath,
                IncludeResolver = new ReportingIncludeResolver(sourcePath, onInclude),
            },
            sourcePath);
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
