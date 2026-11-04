using System;
using System.Collections.Generic;
using System.IO;
using ShadowDusk.Core;
using ShadowDusk.Core.Preprocessor;

namespace Gum;

/// <summary>
/// ShadowDusk's own file-system <c>#include</c> resolution, noting each file it looks for so runtime hot
/// reload can reload the shader when one changes (<see cref="ShaderIncludeTracker"/>, #7034). ShadowDusk
/// lists no files read in its result, so this is the only way to learn them. This file is linked into the
/// Skia ShadowDusk project too.
/// </summary>
internal sealed class ReportingIncludeResolver : IIncludeResolver
{
    private readonly FileSystemIncludeResolver _fileSystem = new FileSystemIncludeResolver();
    private readonly string _sourcePath;
    private readonly List<string> _files = new List<string>();

    public ReportingIncludeResolver(string sourcePath)
    {
        _sourcePath = sourcePath;
    }

    /// <summary>Every file looked for so far; a missing one is listed where ShadowDusk looks first, so creating it later reloads the shader.</summary>
    public IReadOnlyList<string> Files => _files;

    /// <summary>Hands the files read so far to <see cref="ShaderIncludeTracker"/>, for a failed compile too.</summary>
    public void Record() => ShaderIncludeTracker.Record(_sourcePath, _files);

    public Result<IncludeResolvedFile, ShaderError> Resolve(
        string includePath, string? includingFilePath, IReadOnlyList<string> additionalSearchPaths)
    {
        Result<IncludeResolvedFile, ShaderError> resolved =
            _fileSystem.Resolve(includePath, includingFilePath, additionalSearchPaths);
        string? reported = resolved.IsFailure ? NextToIncluder(includePath, includingFilePath) : resolved.Value.FilePath;
        if (reported != null)
        {
            _files.Add(reported);
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
            return null;
        }
    }
}
